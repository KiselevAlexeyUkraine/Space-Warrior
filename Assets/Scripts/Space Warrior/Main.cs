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
            public int waveIndex;
            public GameObject[] enemies;
            public bool isInfinite = true;
            [Min(0)] public int maxEnemyCount = 10;
            public GameObject[] powerUpPrefabs;
            [HideInInspector] public int spawnedCount;

            public enum LineSpawnDirection { Right, Left }

            [Header("Line Spawn (доп. спавн волны со смещением по X)")]
            public bool useLineSpawn = false;
            public GameObject[] lineSpawnEnemies;
            [Min(0.01f)] public float lineSpawnInterval = 1f;
            public LineSpawnDirection lineSpawnDirection = LineSpawnDirection.Right;
            public float lineSpawnStartX = 0f;
            [Min(0f)] public float lineSpawnStepX = 1.5f;
            [Min(1)] public int lineSpawnCount = 5;
            [HideInInspector] public int lineSpawnedInCurrentLine;
            [HideInInspector] public float currentLineX;
        }

        private Dictionary<WeaponType, WeaponConfig> _weaponConfigs;

        [Header("Enemy Spawn")]
        [SerializeField] private bool spawnEnemies = true;
        [SerializeField] private float enemySpawnPerSecond = 0.5f;
        [SerializeField] private float enemyDefaultPadding = 1.5f;

        [Header("Wave-Based Enemy Spawn")]
        [SerializeField] private GridFormationSpawner gridFormationSpawner;
        [SerializeField] private bool requireAllEnemiesToClearWave = false;
        [SerializeField] private List<WaveSpawnSettings> wavePhases = new List<WaveSpawnSettings>();

        private BoundsCheck _boundsCheck;
        private DiContainer _container;
        private BalanceConfig _balanceConfig;

        private WaveSpawnSettings _currentPhase;
        private bool _spawnLoopActive;
        private bool _lineSpawnLoopActive;

        [Inject]
        private void Construct(DiContainer container, List<WeaponConfig> injectedConfigs, BalanceConfig balanceConfig)
        {
            _container = container;
            _balanceConfig = balanceConfig;
            BuildWeaponDictionary(injectedConfigs);
        }

        private void Awake()
        {
            _boundsCheck = GetComponent<BoundsCheck>();
            if (gridFormationSpawner != null)
            {
                gridFormationSpawner.OnWaveStarted += HandleWaveStarted;
                gridFormationSpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
            }
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

            if (!spawnEnemies) return;

            _currentPhase = wavePhases.Find(p => p.waveIndex == waveIndex);
            if (_currentPhase == null || _currentPhase.enemies == null || _currentPhase.enemies.Length == 0) return;

            _currentPhase.spawnedCount = 0;
            _currentPhase.lineSpawnedInCurrentLine = 0;
            _spawnLoopActive = true;

            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);

            if (_currentPhase.useLineSpawn)
            {
                _currentPhase.currentLineX = _currentPhase.lineSpawnStartX;
                _lineSpawnLoopActive = true;
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
            if (configs == null) return;
            foreach (WeaponConfig config in configs)
            {
                if (config != null) _weaponConfigs[config.weaponType] = config;
            }
        }

        public WeaponConfig GetWeaponConfig(WeaponType weaponType)
        {
            if (_weaponConfigs == null) return null;
            return _weaponConfigs.TryGetValue(weaponType, out WeaponConfig config) ? config : null;
        }

        public void ShipDestroyed(EnemyBase enemy)
        {
            if (enemy == null) return;
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
                    if (targetPrefab != null) LeanPool.Spawn(targetPrefab, enemy.transform.position, Quaternion.identity);
                }
            }
            SafeDespawn(enemy.gameObject);
        }

        public void SpawnEnemy()
        {
            if (!spawnEnemies || !_spawnLoopActive || _currentPhase == null) return;

            // Страховка: Ждем инициализации таблиц
            if (_balanceConfig != null && !_balanceConfig.IsLoaded)
            {
                Invoke(nameof(SpawnEnemy), 0.1f);
                return;
            }

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _spawnLoopActive = false;
                return;
            }

            GameObject[] pool = _currentPhase.enemies;
            if (pool == null || pool.Length == 0) return;

            int index = UnityEngine.Random.Range(0, pool.Length);
            GameObject enemyObject = LeanPool.Spawn(pool[index]);
            _container?.InjectGameObject(enemyObject);

            float enemyPadding = enemyDefaultPadding;
            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();
            if (enemyBounds != null) enemyPadding = Mathf.Abs(enemyBounds.radius);

            if (requireAllEnemiesToClearWave && gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            EnemyBase enemyScript = enemyObject.GetComponent<EnemyBase>();
            Vector3 position = Vector3.zero;

            float xMin = -_boundsCheck.camWidth + enemyPadding;
            float xMax = _boundsCheck.camWidth - enemyPadding;
            position.x = UnityEngine.Random.Range(xMin, xMax);

            if (enemyScript != null && enemyScript.movementPattern == MovementPattern.HorizontalEdge)
            {
                float paddingY = _boundsCheck.radius > 0 ? _boundsCheck.radius : 1f;
                position.y = UnityEngine.Random.Range(-_boundsCheck.camHeight + paddingY, _boundsCheck.camHeight - paddingY);
            }
            else
            {
                position.y = _boundsCheck.camHeight + enemyPadding;
            }

            enemyObject.transform.position = position;

            if (enemyScript != null)
            {
                enemyScript.SendMessage("InitMovementPattern", SendMessageOptions.DontRequireReceiver);
            }

            _currentPhase.spawnedCount++;
            Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
        }

        public void SpawnEnemyLine()
        {
            if (!spawnEnemies || !_lineSpawnLoopActive || _currentPhase == null || !_currentPhase.useLineSpawn) return;

            // Страховка: Ждем инициализации таблиц
            if (_balanceConfig != null && !_balanceConfig.IsLoaded)
            {
                Invoke(nameof(SpawnEnemyLine), 0.1f);
                return;
            }

            if (!_currentPhase.isInfinite && _currentPhase.spawnedCount >= _currentPhase.maxEnemyCount)
            {
                _lineSpawnLoopActive = false;
                return;
            }

            GameObject[] pool = (_currentPhase.lineSpawnEnemies != null && _currentPhase.lineSpawnEnemies.Length > 0)
                ? _currentPhase.lineSpawnEnemies
                : _currentPhase.enemies;

            if (pool == null || pool.Length == 0) return;

            int index = UnityEngine.Random.Range(0, pool.Length);
            GameObject enemyObject = LeanPool.Spawn(pool[index]);
            _container?.InjectGameObject(enemyObject);

            if (requireAllEnemiesToClearWave && gridFormationSpawner != null)
            {
                gridFormationSpawner.RegisterAdditionalEnemy(enemyObject);
            }

            float enemyPadding = enemyDefaultPadding;
            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();
            if (enemyBounds != null) enemyPadding = Mathf.Abs(enemyBounds.radius);

            float xMin = -_boundsCheck.camWidth + enemyPadding;
            float xMax = _boundsCheck.camWidth - enemyPadding;
            float clampedX = Mathf.Clamp(_currentPhase.currentLineX, xMin, xMax);

            EnemyBase enemyScript = enemyObject.GetComponent<EnemyBase>();
            float targetY = 0f;

            if (enemyScript != null && enemyScript.movementPattern == MovementPattern.HorizontalEdge)
            {
                float paddingY = _boundsCheck.radius > 0 ? _boundsCheck.radius : 1f;
                targetY = UnityEngine.Random.Range(-_boundsCheck.camHeight + paddingY, _boundsCheck.camHeight - paddingY);
            }
            else
            {
                targetY = _boundsCheck.camHeight + enemyPadding;
            }

            Vector3 position = new Vector3(clampedX, targetY, 0f);
            enemyObject.transform.position = position;

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

            Invoke(nameof(SpawnEnemyLine), _currentPhase.lineSpawnInterval);
        }

        private void SafeDespawn(GameObject go)
        {
            if (go == null) return;
            if (LeanPool.Links.ContainsKey(go)) LeanPool.Despawn(go);
            else Destroy(go);
        }
    }
}