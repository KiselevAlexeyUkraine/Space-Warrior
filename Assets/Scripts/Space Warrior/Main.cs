using System;
using System.Collections.Generic;
using Components.Items;
using Components.Weapon;
using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;

namespace Level
{
    public sealed class Main : MonoBehaviour
    {
        [Serializable]
        public class WaveSpawnSettings
        {
            [Tooltip("Индекс волны из GridFormationSpawner (0 = первая волна), для которой действуют эти враги.")]
            public int waveIndex;

            [Tooltip("Враги, которые будут спавняться на этой волне (случайно выбираются из списка).")]
            public GameObject[] enemies;

            [Tooltip("Если включено — враги спавнятся бесконечно, пока не начнётся следующая волна.")]
            public bool isInfinite = true;

            [Tooltip("Максимальное количество врагов для спавна на этой волне. Учитывается только если 'Is Infinite' выключен.")]
            [Min(0)] public int maxEnemyCount = 10;

            [Tooltip("PowerUp'ы, которые могут заспавниться после уничтожения ПОСЛЕДНЕГО врага именно этой волны (случайно выбирается один из списка). Если пусто — после этой волны PowerUp не спавнится.")]
            public GameObject[] powerUpPrefabs;

            [HideInInspector] public int spawnedCount;

            // ------------------------------------------------------------------
            // LINE SPAWN — дополнительный параллельный спавн врагов друг за другом
            // ------------------------------------------------------------------

            [Header("Line Spawn (доп. спавн врагов друг за другом)")]
            [Tooltip("Если включено — ДОПОЛНИТЕЛЬНО к обычному спавну этой волны будет запущен ещё один " +
                     "поток спавна врагов, которые появляются друг за другом с фиксированной периодичностью " +
                     "(Line Spawn Interval). Позиция спавна такая же, как и у обычных врагов (случайный X, верх экрана).")]
            public bool useLineSpawn = false;

            [Tooltip("Враги для дополнительного Line Spawn (случайно выбираются из списка). " +
                     "Если пусто — используется основной список 'Enemies' выше.")]
            public GameObject[] lineSpawnEnemies;

            [Tooltip("Периодичность появления врагов в режиме Line Spawn, в секундах.")]
            [Min(0.01f)] public float lineSpawnInterval = 1f;

            [Tooltip("Если включено — Line Spawn спавнится бесконечно, пока не начнётся следующая волна.")]
            public bool lineSpawnIsInfinite = true;

            [Tooltip("Максимальное количество врагов для Line Spawn на этой волне. Учитывается только если 'Line Spawn Is Infinite' выключен.")]
            [Min(0)] public int lineSpawnMaxEnemyCount = 5;

            [HideInInspector] public int lineSpawnedCount;
        }

        private Dictionary<WeaponType, WeaponConfig> _weaponConfigs;

        [Header("Enemy Spawn")]
        [Tooltip("Если выключено, враги не будут спавниться вообще.")]
        [SerializeField] private bool spawnEnemies = true;

        [Tooltip("Количество врагов в секунду.")]
        [SerializeField] private float enemySpawnPerSecond = 0.5f;

        [Tooltip("Отступ врага от границы экрана.")]
        [SerializeField] private float enemyDefaultPadding = 1.5f;

        [Header("Wave-Based Enemy Spawn")]
        [Tooltip("Ссылка на GridFormationSpawner, чьи волны определяют, какие враги спавнятся сейчас. ОБЯЗАТЕЛЬНО назначить в инспекторе.")]
        [SerializeField] private GridFormationSpawner gridFormationSpawner;

        [Tooltip("Если включено — для завершения волны и дропа PowerUp нужно убить ВСЕХ врагов (и сетку, и тех, кого спавнит Main). Если выключено — учитываются только враги из сетки.")]
        [SerializeField] private bool requireAllEnemiesToClearWave = false;

        [Tooltip("Список: какая волна (по индексу) — каких врагов спавнить. Если для волны нет записи здесь, обычные враги на ней спавниться не будут (только сетка).")]
        [SerializeField] private List<WaveSpawnSettings> wavePhases = new List<WaveSpawnSettings>();


        private BoundsCheck _boundsCheck;
        private DiContainer _container;

        private WaveSpawnSettings _currentPhase;
        private bool _spawnLoopActive;
        private bool _lineSpawnLoopActive;

        [Inject]
        private void Construct(DiContainer container, List<WeaponConfig> injectedConfigs)
        {
            _container = container;
            BuildWeaponDictionary(injectedConfigs);
        }

        private void Awake()
        {
            _boundsCheck = GetComponent<BoundsCheck>();

            if (gridFormationSpawner == null)
            {
                Debug.LogWarning("[Main] Grid Formation Spawner не назначен в инспекторе — враги по волнам спавниться не будут.");
                return;
            }

            gridFormationSpawner.OnWaveStarted += HandleWaveStarted;
            gridFormationSpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
        }

        private void OnDestroy()
        {
            if (gridFormationSpawner != null)
            {
                gridFormationSpawner.OnWaveStarted -= HandleWaveStarted;
                gridFormationSpawner.OnAllWavesCompleted -= HandleAllWavesCompleted;
            }
        }

        private void HandleWaveStarted(int waveIndex)
        {
            CancelInvoke(nameof(SpawnEnemy));
            CancelInvoke(nameof(SpawnEnemyLine));
            _spawnLoopActive = false;
            _lineSpawnLoopActive = false;

            if (!spawnEnemies)
                return;

            _currentPhase = wavePhases.Find(p => p.waveIndex == waveIndex);

            if (_currentPhase == null || _currentPhase.enemies == null || _currentPhase.enemies.Length == 0)
            {
                Debug.Log($"[Main] Для волны {waveIndex} враги не назначены — спавн от Main пропущен.");
                return;
            }

            _currentPhase.spawnedCount = 0;
            _currentPhase.lineSpawnedCount = 0;
            _spawnLoopActive = true;

            Debug.Log($"[Main] Волна {waveIndex}: старт спавна врагов ({_currentPhase.enemies.Length} типов, infinite={_currentPhase.isInfinite}).");
            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);

            // Line Spawn — ДОПОЛНИТЕЛЬНЫЙ, независимый поток спавна, работает параллельно с обычным
            if (_currentPhase.useLineSpawn)
            {
                _lineSpawnLoopActive = true;

                Debug.Log($"[Main] Волна {waveIndex}: старт ДОПОЛНИТЕЛЬНОГО line-спавна врагов " +
                          $"(interval={_currentPhase.lineSpawnInterval}, infinite={_currentPhase.lineSpawnIsInfinite}).");

                Invoke(nameof(SpawnEnemyLine), _currentPhase.lineSpawnInterval);
            }
        }

        private void HandleAllWavesCompleted()
        {
            CancelInvoke(nameof(SpawnEnemy));
            CancelInvoke(nameof(SpawnEnemyLine));
            _spawnLoopActive = false;
            _lineSpawnLoopActive = false;
            _currentPhase = null;
        }

        private void BuildWeaponDictionary(List<WeaponConfig> configs)
        {
            _weaponConfigs = new Dictionary<WeaponType, WeaponConfig>();

            if (configs == null)
                return;

            foreach (WeaponConfig config in configs)
            {
                if (config == null)
                    continue;

                _weaponConfigs[config.weaponType] = config;
            }
        }

        public WeaponConfig GetWeaponConfig(WeaponType weaponType)
        {
            if (_weaponConfigs == null)
                return null;

            return _weaponConfigs.TryGetValue(weaponType, out WeaponConfig config)
                ? config
                : null;
        }

        public void ShipDestroyed(EnemyBase enemy)
        {
            if (enemy == null)
                return;

            bool isLastEnemyInWave = false;
            int completedWaveIndex = 0;

            if (gridFormationSpawner != null)
            {
                completedWaveIndex = gridFormationSpawner.CurrentWaveIndex;

                // Проверяем, был ли этот враг последним в зарегистрированном списке спавнера
                isLastEnemyInWave = gridFormationSpawner.CheckAndUnregisterLastEnemy(enemy.gameObject);
            }

            if (isLastEnemyInWave)
            {
                // Останавливаем дальнейший спавн летучих врагов для этой волны (оба потока)
                CancelInvoke(nameof(SpawnEnemy));
                CancelInvoke(nameof(SpawnEnemyLine));
                _spawnLoopActive = false;
                _lineSpawnLoopActive = false;

                WaveSpawnSettings completedPhase = wavePhases.Find(p => p.waveIndex == completedWaveIndex);

                if (completedPhase != null && completedPhase.powerUpPrefabs != null && completedPhase.powerUpPrefabs.Length > 0)
                {
                    int prefabIndex = UnityEngine.Random.Range(0, completedPhase.powerUpPrefabs.Length);
                    GameObject targetPrefab = completedPhase.powerUpPrefabs[prefabIndex];

                    if (targetPrefab != null)
                    {
                        GameObject powerUpObject = LeanPool.Spawn(
                            targetPrefab,
                            enemy.transform.position,
                            Quaternion.identity);

                        string modeInfo = requireAllEnemiesToClearWave ? "Все враги (включая спавн Main)" : "Сетка";
                        Debug.Log($"<color=yellow>⭐ {modeInfo} волны {completedWaveIndex} зачищена! Заспавнен PowerUp: {targetPrefab.name} ⭐</color>");
                    }
                }
                else
                {
                    Debug.Log($"[Main] Для волны {completedWaveIndex} PowerUp'ы не назначены в WaveSpawnSettings — спавн пропущен.");
                }
            }

            SafeDespawn(enemy.gameObject);
        }

        private Vector3 GetRandomTopSpawnPosition(GameObject enemyObject, out float enemyPadding)
        {
            enemyPadding = enemyDefaultPadding;

            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();

            if (enemyBounds != null)
            {
                enemyPadding = Mathf.Abs(enemyBounds.radius);
            }

            Vector3 position = Vector3.zero;

            float xMin = -_boundsCheck.camWidth - enemyPadding + 2f;
            float xMax = _boundsCheck.camWidth - enemyPadding - 2f;

            position.x = UnityEngine.Random.Range(xMin, xMax);
            position.y = _boundsCheck.camHeight + enemyPadding;

            return position;
        }

        public void SpawnEnemy()
        {
            if (!spawnEnemies || !_spawnLoopActive || _currentPhase == null)
                return;

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _spawnLoopActive = false;
                return;
            }

            GameObject[] pool = _currentPhase.enemies;

            if (pool == null || pool.Length == 0)
                return;

            int index = UnityEngine.Random.Range(0, pool.Length);

            GameObject enemyObject = LeanPool.Spawn(pool[index]);

            _container?.InjectGameObject(enemyObject);

            // Динамическая регистрация в зависимости от состояния галочки в инспекторе
            if (requireAllEnemiesToClearWave && gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            Vector3 position = GetRandomTopSpawnPosition(enemyObject, out _);
            enemyObject.transform.position = position;

            _currentPhase.spawnedCount++;

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _spawnLoopActive = false;
                return;
            }

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
        }

        /// <summary>
        /// ДОПОЛНИТЕЛЬНЫЙ поток спавна: работает параллельно с обычным SpawnEnemy.
        /// Враги появляются друг за другом с периодичностью lineSpawnInterval,
        /// позиция такая же, как и у обычных врагов — случайный X сверху экрана.
        /// </summary>
        public void SpawnEnemyLine()
        {
            if (!spawnEnemies || !_lineSpawnLoopActive || _currentPhase == null || !_currentPhase.useLineSpawn)
                return;

            if (!_currentPhase.lineSpawnIsInfinite && _currentPhase.lineSpawnedCount >= _currentPhase.lineSpawnMaxEnemyCount)
            {
                _lineSpawnLoopActive = false;
                return;
            }

            GameObject[] pool = (_currentPhase.lineSpawnEnemies != null && _currentPhase.lineSpawnEnemies.Length > 0)
                ? _currentPhase.lineSpawnEnemies
                : _currentPhase.enemies;

            if (pool == null || pool.Length == 0)
                return;

            int index = UnityEngine.Random.Range(0, pool.Length);

            GameObject enemyObject = LeanPool.Spawn(pool[index]);

            _container?.InjectGameObject(enemyObject);

            if (requireAllEnemiesToClearWave && gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            Vector3 position = GetRandomTopSpawnPosition(enemyObject, out _);
            enemyObject.transform.position = position;

            _currentPhase.lineSpawnedCount++;

            if (!_currentPhase.lineSpawnIsInfinite && _currentPhase.lineSpawnedCount >= _currentPhase.lineSpawnMaxEnemyCount)
            {
                _lineSpawnLoopActive = false;
                return;
            }

            Invoke(nameof(SpawnEnemyLine), _currentPhase.lineSpawnInterval);
        }

        private void SafeDespawn(GameObject go)
        {
            if (go == null)
                return;

            if (LeanPool.Links.ContainsKey(go))
            {
                LeanPool.Despawn(go);
            }
            else
            {
                Destroy(go);
            }
        }
    }
}