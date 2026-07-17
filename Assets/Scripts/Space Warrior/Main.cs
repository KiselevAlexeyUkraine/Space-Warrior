using System;
using System.Collections.Generic;
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

            [Tooltip("Максимальное количество врагов для спавна на этой волне (суммарно, включая линейный спавн, если он включён). Учитывается только если 'Is Infinite' выключен.")]
            [Min(0)] public int maxEnemyCount = 10;

            [Tooltip("PowerUp'ы, которые могут заспавниться после уничтожения ПОСЛЕДНЕГО врага именно этой волны (случайно выбирается один из списка). Если пусто — после этой волны PowerUp не спавнится.")]
            public GameObject[] powerUpPrefabs;

            [HideInInspector] public int spawnedCount;

            // ------------------------------------------------------------------
            // LINE SPAWN — ДОПОЛНИТЕЛЬНЫЙ спавн врагов друг за другом со смещением по X
            // ------------------------------------------------------------------

            public enum LineSpawnDirection
            {
                Right,
                Left
            }

            [Header("Line Spawn (доп. спавн волны со смещением по X)")]
            [Tooltip("Если включено — ДОПОЛНИТЕЛЬНО к обычному случайному спавну этой волны будут " +
                     "спавниться враги друг за другом.")]
            public bool useLineSpawn = false;

            [Tooltip("Враги для доп. Line Spawn (случайно выбираются из списка). " +
                     "Если пусто — используется основной список 'Enemies' выше.")]
            public GameObject[] lineSpawnEnemies;

            [Tooltip("Периодичность появления врагов в режиме Line Spawn, в секундах.")]
            [Min(0.01f)] public float lineSpawnInterval = 1f;

            [Tooltip("Направление смещения по X: Right — каждый следующий враг правее предыдущего, Left — левее.")]
            public LineSpawnDirection lineSpawnDirection = LineSpawnDirection.Right;

            [Tooltip("Стартовая позиция по X для первого врага линии.")]
            public float lineSpawnStartX = 0f;

            [Tooltip("Шаг смещения по X между соседними врагами линии.")]
            [Min(0f)] public float lineSpawnStepX = 1.5f;

            [Tooltip("Сколько врагов спавнится в одной линии, прежде чем позиция X сбросится на старт " +
                     "(линия начнётся заново, пока волна активна).")]
            [Min(1)] public int lineSpawnCount = 5;

            [HideInInspector] public int lineSpawnedInCurrentLine;
            [HideInInspector] public float currentLineX;
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
            _currentPhase.lineSpawnedInCurrentLine = 0;

            _spawnLoopActive = true;

            Debug.Log($"[Main] Волна {waveIndex}: старт обычного спавна врагов ({_currentPhase.enemies.Length} типов, infinite={_currentPhase.isInfinite}).");

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);

            if (_currentPhase.useLineSpawn)
            {
                _currentPhase.currentLineX = _currentPhase.lineSpawnStartX;
                _lineSpawnLoopActive = true;

                Debug.Log($"[Main] Волна {waveIndex}: дополнительно старт LINE-спавна врагов " +
                          $"(interval={_currentPhase.lineSpawnInterval}, direction={_currentPhase.lineSpawnDirection}).");

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
                isLastEnemyInWave = gridFormationSpawner.CheckAndUnregisterLastEnemy(enemy.gameObject);
            }

            if (isLastEnemyInWave)
            {
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
                        LeanPool.Spawn(targetPrefab, enemy.transform.position, Quaternion.identity);
                        string modeInfo = requireAllEnemiesToClearWave ? "Все враги (включая спавн Main)" : "Сетка";
                        Debug.Log($"<color=yellow>⭐ {modeInfo} волны {completedWaveIndex} зачищена! Заспавнен PowerUp: {targetPrefab.name} ⭐</color>");
                    }
                }
            }

            SafeDespawn(enemy.gameObject);
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

            float enemyPadding = enemyDefaultPadding;
            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();
            if (enemyBounds != null)
            {
                enemyPadding = Mathf.Abs(enemyBounds.radius);
            }

            if (requireAllEnemiesToClearWave && gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            // --- ИСПРАВЛЕНИЕ СПАВНА ПО ОСИ Y ---
            Vector3 position = Vector3.zero;
            float xMin = -_boundsCheck.camWidth - enemyPadding + 2f;
            float xMax = _boundsCheck.camWidth - enemyPadding - 2f;
            position.x = UnityEngine.Random.Range(xMin, xMax);

            // Вычисляем случайную высоту внутри видимой зоны экрана, а не жестко сверху
            float paddingY = _boundsCheck.radius > 0 ? _boundsCheck.radius : 1f;
            position.y = UnityEngine.Random.Range(-_boundsCheck.camHeight + paddingY, _boundsCheck.camHeight - paddingY);

            enemyObject.transform.position = position;
            // ----------------------------------

            // Перезапускаем паттерн движения EnemyBase вручную, так как мы изменили позицию ПОСЛЕ OnEnable
            EnemyBase enemyScript = enemyObject.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                // Вызываем приватный метод инициализации через SendMessage, чтобы паттерн подхватил новую точку Y
                enemyScript.SendMessage("InitMovementPattern", SendMessageOptions.DontRequireReceiver);
            }

            _currentPhase.spawnedCount++;

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _spawnLoopActive = false;
                return;
            }

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
        }

        public void SpawnEnemyLine()
        {
            if (!spawnEnemies || !_lineSpawnLoopActive || _currentPhase == null || !_currentPhase.useLineSpawn)
                return;

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
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

            float enemyPadding = enemyDefaultPadding;
            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();
            if (enemyBounds != null)
            {
                enemyPadding = Mathf.Abs(enemyBounds.radius);
            }

            float xMin = -_boundsCheck.camWidth - enemyPadding + 2f;
            float xMax = _boundsCheck.camWidth - enemyPadding - 2f;
            float clampedX = Mathf.Clamp(_currentPhase.currentLineX, xMin, xMax);

            // --- ИСПРАВЛЕНИЕ СПАВНА ПО ОСИ Y ДЛЯ ЛИНИИ ---
            // Линия спавнится на одной случайной высоте на экране
            float paddingY = _boundsCheck.radius > 0 ? _boundsCheck.radius : 1f;
            float randomLineY = UnityEngine.Random.Range(-_boundsCheck.camHeight + paddingY, _boundsCheck.camHeight - paddingY);

            Vector3 position = new Vector3(clampedX, randomLineY, 0f);
            enemyObject.transform.position = position;
            // ---------------------------------------------

            EnemyBase enemyScript = enemyObject.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.SendMessage("InitMovementPattern", SendMessageOptions.DontRequireReceiver);
            }

            _currentPhase.spawnedCount++;
            _currentPhase.lineSpawnedInCurrentLine++;

            float xOffset = _currentPhase.lineSpawnDirection == WaveSpawnSettings.LineSpawnDirection.Right
                ? _currentPhase.lineSpawnStepX
                : -_currentPhase.lineSpawnStepX;

            _currentPhase.currentLineX += xOffset;

            if (_currentPhase.lineSpawnedInCurrentLine >= _currentPhase.lineSpawnCount)
            {
                _currentPhase.lineSpawnedInCurrentLine = 0;
                _currentPhase.currentLineX = _currentPhase.lineSpawnStartX;
            }

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
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