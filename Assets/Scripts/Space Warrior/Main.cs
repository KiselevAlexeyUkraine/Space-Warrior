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

            [Tooltip("Враги, которые будут спавниться на этой волне (случайно выбираются из списка).")]
            public GameObject[] enemies;

            [Tooltip("Если включено — враги спавнятся бесконечно, пока не начнётся следующая волна.")]
            public bool isInfinite = true;

            [Tooltip("Максимальное количество врагов для спавна на этой волне. Учитывается только если 'Is Infinite' выключен.")]
            [Min(0)] public int maxEnemyCount = 10;

            [HideInInspector] public int spawnedCount;
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

        [Tooltip("Список: какая волна (по индексу) — каких врагов спавнить. Если для волны нет записи здесь, обычные враги на ней спавниться не будут (только сетка).")]
        [SerializeField] private List<WaveSpawnSettings> wavePhases = new List<WaveSpawnSettings>();

        [Header("PowerUps")]
        [Tooltip("Префаб PowerUp, который спавнится при уничтожении последнего врага волны.")]
        [SerializeField] private GameObject prefabPowerUp;

        [SerializeField] private WeaponType[] powerUpFrequency =
        {
            WeaponType.blaster,
            WeaponType.blaster,
            WeaponType.spread,
            WeaponType.shield
        };

        private BoundsCheck _boundsCheck;
        private DiContainer _container;

        private WaveSpawnSettings _currentPhase;
        private bool _spawnLoopActive;

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
            _spawnLoopActive = false;

            if (!spawnEnemies)
                return;

            _currentPhase = wavePhases.Find(p => p.waveIndex == waveIndex);

            if (_currentPhase == null || _currentPhase.enemies == null || _currentPhase.enemies.Length == 0)
            {
                Debug.Log($"[Main] Для волны {waveIndex} враги не назначены — спавн от Main пропущен.");
                return;
            }

            _currentPhase.spawnedCount = 0;
            _spawnLoopActive = true;

            Debug.Log($"[Main] Волна {waveIndex}: старт спавна врагов ({_currentPhase.enemies.Length} типов, infinite={_currentPhase.isInfinite}).");

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
        }

        private void HandleAllWavesCompleted()
        {
            CancelInvoke(nameof(SpawnEnemy));
            _spawnLoopActive = false;
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

            // Проверяем, был ли этот враг последним в текущей волне спавнера.
            // CheckAndUnregisterLastEnemy сама запускает переход на следующую волну, если это так.
            bool isLastEnemyInWave = false;
            if (gridFormationSpawner != null)
            {
                isLastEnemyInWave = gridFormationSpawner.CheckAndUnregisterLastEnemy(enemy.gameObject);
            }

            // PowerUp спавнится ТОЛЬКО при уничтожении последнего врага волны.
            if (isLastEnemyInWave && prefabPowerUp != null)
            {
                int index = UnityEngine.Random.Range(0, powerUpFrequency.Length);
                WeaponType powerUpType = powerUpFrequency[index];

                GameObject powerUpObject = LeanPool.Spawn(
                    prefabPowerUp,
                    enemy.transform.position,
                    Quaternion.identity);

                PowerUp powerUp = powerUpObject.GetComponent<PowerUp>();

                if (powerUp != null)
                {
                    powerUp.SetType(powerUpType);
                }

                Debug.Log("<color=yellow>⭐ Последний враг волны убит! PowerUp заспавнен! ⭐</color>");
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

            // Регистрируем врага, который спавнится случайно из Main, в общем спавнере волн, 
            // чтобы он тоже учитывался при подсчете "последнего врага волны"
            if (gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            Vector3 position = Vector3.zero;

            float xMin = -_boundsCheck.camWidth - enemyPadding + 2f;
            float xMax = _boundsCheck.camWidth - enemyPadding - 2f;

            position.x = UnityEngine.Random.Range(xMin, xMax);
            position.y = _boundsCheck.camHeight + enemyPadding;

            enemyObject.transform.position = position;

            _currentPhase.spawnedCount++;

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _spawnLoopActive = false; // Лимит достигнут — ждём следующую волну.
                return;
            }

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
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