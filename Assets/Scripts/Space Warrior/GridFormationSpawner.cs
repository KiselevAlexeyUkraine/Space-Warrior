using System;
using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;
using Servise;
using System.Collections.Generic;
using Components.Ui.Pages;

namespace Level
{
    public sealed class GridFormationSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class WaveConfiguration
        {
            public string waveName = "Волна";

            [Header("===== SINGLE ENEMY SETTINGS =====")]
            [Tooltip("Если включено, эта волна спавнит только ОДНОГО врага по центру вместо сетки.")]
            public bool isSingleEnemyWave = false;
            
            [Tooltip("Префаб одиночного врага (например, EnemyOfficer).")]
            public GameObject singleEnemyPrefab;

            [Header("===== GRID SETTINGS (Игнорируется, если включен сингл) =====")]
            [Tooltip("Количество столбцов в этой волне (если 0, возьмутся настройки по умолчанию).")]
            [Min(0)] public int maxColumns = 5;

            [Tooltip("Количество рядов в этой волне. Определяет, сколько рядов будет создано.")]
            [Min(0)] public int maxRows = 4;

            [Header("Prefabs Per Row")]
            [Tooltip("Список префабов для каждого ряда. Индекс элемента = номер ряда (Ряд 0, Ряд 1 и т.д.). Если рядов больше, чем элементов в списке, будет использоваться последний префаб.")]
            public List<GameObject> rowPrefabs = new List<GameObject>();
            
            [HideInInspector] public List<int> cachedLayers = new List<int>();
        }

        [Header("Grid Formation (Default Settings)")]
        [SerializeField] private Transform formationRoot;
        [SerializeField, Min(1)] private int defaultColumns = 5;
        [SerializeField, Min(1)] private int defaultRows = 4;
        [SerializeField] private float spacingX = 1.0f;
        [SerializeField] private float spacingY = 1.0f;

        [Header("Wave Settings")]
        [SerializeField] private List<WaveConfiguration> waves = new List<WaveConfiguration>();

        [Tooltip("Скорость генерации сетки (врагов в секунду).")]
        [SerializeField] private float spawnPerSecond = 2f;

        [Header("Bypass Settings")]
        [SerializeField] private float criticalYPosition = -70f;

        private BoundsCheck _bndCheck;
        private DiContainer _container;
        private PageSwitcher _pageSwitcher; 
        private AnimatorService _animatorService; 
        
        private int _currentWaveIndex = 0;
        private int _currentColumn = 0;
        private int _currentRow = 0;
        private bool _spawningWaveActive = false;
        private int _totalSpawnedInWave = 0;
        private HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();
        private bool _isTransitioningBetweenWaves = false; 

        private int _currentWaveMaxColumns;
        private int _currentWaveMaxRows;

        /// <summary>Индекс текущей активной волны (0-based). Используется извне (например, в Main) для привязки спавна врагов к волне.</summary>
        public int CurrentWaveIndex => _currentWaveIndex;

        /// <summary>Вызывается каждый раз при старте новой волны. Параметр — индекс волны (0-based).</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>Вызывается один раз, когда пройдены все волны.</summary>
        public event Action OnAllWavesCompleted;

        [Inject]
        private void Construct(DiContainer container, AnimatorService animatorService, PageSwitcher pageSwitcher)
        {
            _container = container;
            _animatorService = animatorService;
            _pageSwitcher = pageSwitcher;
        }

        private void Awake()
        {
            _bndCheck = GetComponent<BoundsCheck>();
            if (_bndCheck == null) _bndCheck = gameObject.AddComponent<BoundsCheck>();

            if (formationRoot == null) formationRoot = this.transform;

            CacheWaveLayers();
        }

        private void Start()
        {
            StartNextWave();
        }

        private void StartNextWave()
        {
            if (_currentWaveIndex >= waves.Count)
            {
                Debug.Log("🎉 ВСЕ ВОЛНЫ ПРОЙДЕНЫ! ПОБЕДА!");
                OnAllWavesCompleted?.Invoke();
                _pageSwitcher?.Open(PageName.Complete).Forget();
                return;
            }

            WaveConfiguration currentWave = waves[_currentWaveIndex];
            Debug.Log($"⚔️ Старт волны [{_currentWaveIndex + 1}/{waves.Count}]: {currentWave.waveName}");

            OnWaveStarted?.Invoke(_currentWaveIndex);
            
            _totalSpawnedInWave = 0;
            _activeEnemies.Clear();
            _isTransitioningBetweenWaves = false; 

            if (currentWave.isSingleEnemyWave)
            {
                _spawningWaveActive = false;
                SpawnSingleEnemy(currentWave.singleEnemyPrefab);
                return;
            }

            _currentWaveMaxColumns = currentWave.maxColumns > 0 ? currentWave.maxColumns : defaultColumns;
            _currentWaveMaxRows = currentWave.maxRows > 0 ? currentWave.maxRows : defaultRows;

            _currentColumn = 0;
            _currentRow = 0;
            _spawningWaveActive = true;

            if (_animatorService != null)
            {
                _animatorService.StartAnimation(formationRoot);
            }

            Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
        }

        private void SpawnSingleEnemy(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"[Spawner] В волне {_currentWaveIndex + 1} включен режим одиночки, но префаб не назначен!");
                OnWaveCleared();
                return;
            }

            GameObject go = LeanPool.Spawn(prefab);
            _container.InjectGameObject(go);

            float spawnY = _bndCheck.camHeight + 2f;
            go.transform.position = new Vector3(0f, spawnY, 0f);
            go.transform.SetParent(formationRoot, true);

            EnemyBase enemyScript = go.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.SetTargetLocalPosition(Vector3.zero);
            }

            _activeEnemies.Add(go);
            _totalSpawnedInWave = 1;

            if (_animatorService != null)
            {
                _animatorService.StartAnimation(formationRoot);
            }
        }

        private void SpawnNextGridEnemy()
        {
            if (!_spawningWaveActive) return;

            WaveConfiguration currentWave = waves[_currentWaveIndex];
            
            // Получаем префаб и слой для текущего ряда
            GameObject prefab = GetPrefabForCurrentRow(currentWave, _currentRow);
            int layer = GetLayerForCurrentRow(currentWave, _currentRow);

            if (prefab == null)
            {
                // Если префаб для этого ряда забыли поставить, просто идем дальше
                AdvanceGridPointers();
                CheckWaveSpawnCompletion();
                return;
            }

            GameObject go = LeanPool.Spawn(prefab);
            _container.InjectGameObject(go);

            float spawnY = _bndCheck.camHeight + 2f;
            float spawnX = UnityEngine.Random.Range(-_bndCheck.camWidth + 1f, _bndCheck.camWidth - 1f);
            go.transform.position = new Vector3(spawnX, spawnY, 0f);
            go.transform.SetParent(formationRoot, true); 

            SetLayerRecursively(go, layer);

            Vector3 localTargetPos = new Vector3(_currentColumn * spacingX, _currentRow * spacingY, 0f);

            EnemyBase enemyScript = go.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.SetTargetLocalPosition(localTargetPos);
            }

            _activeEnemies.Add(go);
            _totalSpawnedInWave++;

            AdvanceGridPointers();
            CheckWaveSpawnCompletion();
        }

        private void AdvanceGridPointers()
        {
            _currentColumn++;
            if (_currentColumn >= _currentWaveMaxColumns)
            {
                _currentColumn = 0;
                _currentRow++;
            }
        }

        private void CheckWaveSpawnCompletion()
        {
            if (_currentRow >= _currentWaveMaxRows)
            {
                _spawningWaveActive = false;
                if (_activeEnemies.Count == 0 && !_isTransitioningBetweenWaves)
                {
                    OnWaveCleared();
                }
                return;
            }

            Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
        }

        private void Update()
        {
            if (_isTransitioningBetweenWaves) return;

            if (formationRoot != null && formationRoot.position.y <= criticalYPosition)
            {
                ForceClearRemainingEnemies();
                OnWaveCleared();
                return;
            }

            // Резервная проверка: ловит врагов, которые исчезли не через Main.ShipDestroyed
            // (например, улетели за экран и деспавнились сами внутри EnemyBase).
            if (!_spawningWaveActive && _activeEnemies.Count > 0)
            {
                _activeEnemies.RemoveWhere(item => item == null || !item.activeInHierarchy);

                if (_activeEnemies.Count == 0)
                {
                    OnWaveCleared();
                }
            }
        }

        /// <summary>
        /// Позволяет классу Main регистрировать врагов, созданных динамически, 
        /// чтобы они учитывались в общем списке активных юнитов волны.
        /// </summary>
        public void RegisterAdditionalEnemy(GameObject enemy)
        {
            if (enemy != null && !_isTransitioningBetweenWaves)
            {
                _activeEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// Вызывается при подтверждённом убийстве врага (через Main.ShipDestroyed).
        /// Немедленно убирает врага из списка активных и, если это был последний враг волны,
        /// сразу же запускает переход к следующей волне (не дожидаясь Update()).
        /// Возвращает true, если враг был последним в волне.
        /// </summary>
        public bool CheckAndUnregisterLastEnemy(GameObject enemy)
        {
            if (enemy == null || _isTransitioningBetweenWaves) return false;

            _activeEnemies.Remove(enemy);
            _activeEnemies.RemoveWhere(item => item == null || !item.activeInHierarchy);

            bool isLast = !_spawningWaveActive && _activeEnemies.Count == 0;

            if (isLast)
            {
                OnWaveCleared();
            }

            return isLast;
        }

        private void ForceClearRemainingEnemies()
        {
            _spawningWaveActive = false;
            CancelInvoke(nameof(SpawnNextGridEnemy));

            foreach (GameObject enemy in _activeEnemies)
            {
                if (enemy != null && enemy.activeInHierarchy)
                {
                    if (LeanPool.Links.ContainsKey(enemy)) LeanPool.Despawn(enemy);
                    else Destroy(enemy);
                }
            }
            _activeEnemies.Clear();
        }

        private void OnWaveCleared()
        {
            _isTransitioningBetweenWaves = true;
            CancelInvoke(nameof(SpawnNextGridEnemy));
            
            _currentWaveIndex++;

            if (_animatorService != null)
            {
                _animatorService.ResetFormationAndCall(() => StartNextWave());
            }
            else
            {
                StartNextWave();
            }
        }

        private GameObject GetPrefabForCurrentRow(WaveConfiguration wave, int rowIndex)
        {
            if (wave.rowPrefabs == null || wave.rowPrefabs.Count == 0) return null;
            
            int index = Mathf.Min(rowIndex, wave.rowPrefabs.Count - 1);
            return wave.rowPrefabs[index];
        }

        private int GetLayerForCurrentRow(WaveConfiguration wave, int rowIndex)
        {
            if (wave.cachedLayers == null || wave.cachedLayers.Count == 0) return 0;
            int index = Mathf.Min(rowIndex, wave.cachedLayers.Count - 1);
            return wave.cachedLayers[index];
        }

        private void CacheWaveLayers()
        {
            foreach (var wave in waves)
            {
                wave.cachedLayers = new List<int>();
                if (wave.rowPrefabs == null) continue;

                foreach (var prefab in wave.rowPrefabs)
                {
                    if (prefab != null) wave.cachedLayers.Add(prefab.layer);
                    else wave.cachedLayers.Add(0);
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null) SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}