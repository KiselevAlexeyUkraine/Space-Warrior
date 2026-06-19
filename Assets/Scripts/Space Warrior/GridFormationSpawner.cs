using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;
using Servise;
using System.Collections.Generic;
using Components.Ui.Pages;

public class GridFormationSpawner : MonoBehaviour
{
    [System.Serializable]
    public class RowSettings
    {
        [Tooltip("Префаб врага (должен наследоваться от EnemyBase и иметь галочку Use Grid Formation).")]
        public GameObject enemyPrefab;

        [Header("Фильтры размещения")]
        [Tooltip("С какого индекса ряда начинать спавнить этого врага (включительно).")]
        public int startRow = 0;

        [Tooltip("По какой индекс ряда спавнить этого врага (включительно).")]
        public int endRow = 3;

        [Tooltip("Шаг по рядам. 1 — каждый ряд, 2 — через один и т.д.")]
        [Min(1)] public int rowStep = 1;

        [Tooltip("Шаг по колонкам. 1 — подряд, 2 — в шахматном порядке.")]
        [Min(1)] public int columnStep = 1;

        [HideInInspector] public int prefabLayer;
    }

    [System.Serializable]
    public class WaveConfiguration
    {
        [Tooltip("Название волны для логов.")]
        public string waveName = "Волна";

        [Tooltip("Настройки для первого типа врагов в этой волне.")]
        public RowSettings firstEnemySettings;

        [Tooltip("Настройки для второго типа врагов в этой волне.")]
        public RowSettings secondEnemySettings;
    }

    [Header("Grid Formation Settings")]
    [SerializeField] private Transform formationRoot;
    [SerializeField, Min(1)] private int maxColumns = 5;
    [SerializeField, Min(1)] private int maxRows = 4;
    [SerializeField] private float spacingX = 1.0f;
    [SerializeField] private float spacingY = 1.0f;

    [Header("Wave Settings")]
    [Tooltip("Список волн, которые будут идти по очереди.")]
    [SerializeField] private List<WaveConfiguration> waves = new List<WaveConfiguration>();

    [Tooltip("Скорость генерации сетки (врагов в секунду).")]
    [SerializeField] private float spawnPerSecond = 2f;

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

        if (formationRoot == null)
        {
            formationRoot = this.transform;
            Debug.LogWarning("Formation Root не задан! Используется спавнер.", this);
        }

        CacheWaveLayers();
    }

    private void Start()
    {
        StartNextWave();
    }

    private void StartNextWave()
    {
        // ИСПРАВЛЕНО: Если индекс дошел до конца списка волн, значит, последняя волна была успешно зачищена
        if (_currentWaveIndex >= waves.Count)
        {
            Debug.Log("🎉 ВСЕ ВОЛНЫ ПРОЙДЕНЫ! ПОБЕДА!");
            
            // Открываем экран победы
            _pageSwitcher?.Open(PageName.Complete).Forget();
            return;
        }

        Debug.Log($"⚔️ Старт волны [{_currentWaveIndex + 1}/{waves.Count}]: {waves[_currentWaveIndex].waveName}");
        
        _currentColumn = 0;
        _currentRow = 0;
        _totalSpawnedInWave = 0;
        _activeEnemies.Clear();
        _spawningWaveActive = true;

        // Запускаем движение через сервис анимации
        if (_animatorService != null)
        {
            _animatorService.StartAnimation(formationRoot);
        }

        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }

    private void SpawnNextGridEnemy()
    {
        if (!_spawningWaveActive) return;

        WaveConfiguration currentWave = waves[_currentWaveIndex];
        RowSettings activeSettings = GetActiveSettingsForCurrentCell(currentWave);

        if (activeSettings == null || activeSettings.enemyPrefab == null)
        {
            AdvanceGridPointers();
            CheckWaveSpawnCompletion();
            return;
        }

        GameObject go = LeanPool.Spawn(activeSettings.enemyPrefab);
        _container.InjectGameObject(go);

        float spawnY = _bndCheck.camHeight + 2f;
        float spawnX = Random.Range(-_bndCheck.camWidth + 1f, _bndCheck.camWidth - 1f);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);
        go.transform.SetParent(formationRoot, true); 

        SetLayerRecursively(go, activeSettings.prefabLayer);

        Vector3 localTargetPos = new Vector3(_currentColumn * spacingX, _currentRow * spacingY, 0f);

        EnemyBase enemyScript = go.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
            enemyScript.SetTargetLocalPosition(localTargetPos);
        }

        // Регистрируем врага, чтобы знать, когда волна опустеет
        _activeEnemies.Add(go);
        _totalSpawnedInWave++;

        AdvanceGridPointers();
        CheckWaveSpawnCompletion();
    }

    private void AdvanceGridPointers()
    {
        _currentColumn++;
        if (_currentColumn >= maxColumns)
        {
            _currentColumn = 0;
            _currentRow++;
        }
    }

    private void CheckWaveSpawnCompletion()
    {
        if (_currentRow >= maxRows)
        {
            _spawningWaveActive = false;
            Debug.Log($"Спавн волны {_currentWaveIndex + 1} завершен. Ожидаем зачистки. Всего заспавнено: {_totalSpawnedInWave}");
            
            // Если вдруг условия шагов отсеяли вообще всех врагов и никто не заспавнился:
            if (_activeEnemies.Count == 0)
            {
                OnWaveCleared();
            }
            return;
        }

        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }

    private void Update()
    {
        // Очищаем уничтоженные/деспавненные объекты из нашего списка живых врагов
        if (!_spawningWaveActive && _activeEnemies.Count > 0)
        {
            _activeEnemies.RemoveWhere(item => item == null || !item.activeInHierarchy);

            if (_activeEnemies.Count == 0)
            {
                OnWaveCleared();
            }
        }
    }

    private void OnWaveCleared()
    {
        CancelInvoke(nameof(SpawnNextGridEnemy));
        Debug.Log($"👍 Волна {_currentWaveIndex + 1} полностью уничтожена!");
        
        _currentWaveIndex++;

        if (_animatorService != null)
        {
            // Сбрасываем корень анимации в дефолтную позицию
            _animatorService.ResetFormationAndCall(() =>
            {
                // По завершении движения возврата — запускаем следующую волну
                StartNextWave();
            });
        }
        else
        {
            StartNextWave();
        }
    }

    private RowSettings GetActiveSettingsForCurrentCell(WaveConfiguration wave)
    {
        if (IsCellValidForConfig(_currentRow, _currentColumn, wave.secondEnemySettings)) return wave.secondEnemySettings;
        if (IsCellValidForConfig(_currentRow, _currentColumn, wave.firstEnemySettings)) return wave.firstEnemySettings;
        return null;
    }

    private bool IsCellValidForConfig(int row, int col, RowSettings settings)
    {
        if (settings == null || settings.enemyPrefab == null) return false;
        if (row < settings.startRow || row > settings.endRow) return false;
        if ((row - settings.startRow) % settings.rowStep != 0) return false;
        if (col % settings.columnStep != 0) return false;
        return true;
    }

    private void CacheWaveLayers()
    {
        foreach (var wave in waves)
        {
            if (wave.firstEnemySettings?.enemyPrefab != null)
                wave.firstEnemySettings.prefabLayer = wave.firstEnemySettings.enemyPrefab.layer;
            if (wave.secondEnemySettings?.enemyPrefab != null)
                wave.secondEnemySettings.prefabLayer = wave.secondEnemySettings.enemyPrefab.layer;
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