using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;
using Servise;

public class GridFormationSpawner : MonoBehaviour
{
    [Header("Grid Formation Settings")]
    [Tooltip("Центральная/стартовая точка, от которой начнется построение сетки.")]
    [SerializeField] private Transform formationRoot;

    [Tooltip("Количество врагов в одном ряду (по горизонтали X).")]
    [SerializeField, Min(1)] private int maxColumns = 5;

    [Tooltip("Количество рядов (по вертикали Y).")]
    [SerializeField, Min(1)] private int maxRows = 4;

    [Tooltip("Расстояние между врагами по горизонтали.")]
    [SerializeField] private float spacingX = 1.0f;

    [Tooltip("Расстояние между врагами по вертикали.")]
    [SerializeField] private float spacingY = 1.0f;

    [Header("Spawn Settings")]
    [Tooltip("Префаб врага (должен наследоваться от EnemyBase и иметь галочку Use Grid Formation).")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Скорость спавна (врагов в секунду).")]
    [SerializeField] private float spawnPerSecond = 2f;

    private BoundsCheck _bndCheck;
    private DiContainer _container;
    private AnimatorService _animatorService; 
    
    private int _currentColumn = 0;
    private int _currentRow = 0;
    private bool _formationComplete = false;
    private int _prefabLayer; // Кэшируем слой префаба

    [Inject]
    private void Construct(DiContainer container, AnimatorService animatorService)
    {
        _container = container;
        _animatorService = animatorService;
    }

    private void Awake()
    {
        _bndCheck = GetComponent<BoundsCheck>();
        if (_bndCheck == null)
        {
            _bndCheck = gameObject.AddComponent<BoundsCheck>();
        }

        if (formationRoot == null)
        {
            formationRoot = this.transform;
            Debug.LogWarning("Formation Root не задан! Используется позиция самого спавнера.", this);
        }

        // Запоминаем оригинальный слой префаба, чтобы не читать его каждый раз
        if (enemyPrefab != null)
        {
            _prefabLayer = enemyPrefab.layer;
        }
    }

    private void Start()
    {
        if (_animatorService != null)
        {
            _animatorService.StartAnimation(formationRoot);
        }
        else
        {
            Debug.LogError("AnimatorService не был внедрен в GridFormationSpawner!");
        }

        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }

    private void SpawnNextGridEnemy()
    {
        if (_formationComplete) return;

        GameObject go = LeanPool.Spawn(enemyPrefab);
        _container.InjectGameObject(go);

        float spawnY = _bndCheck.camHeight + 2f;
        float spawnX = Random.Range(-_bndCheck.camWidth + 1f, _bndCheck.camWidth - 1f);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        // Назначаем родителя
        go.transform.SetParent(formationRoot, true); 

        // ИСПРАВЛЕНО: Принудительно возвращаем объекту и всем его дочерним элементам (если есть) слой префаба
        SetLayerRecursively(go, _prefabLayer);

        Vector3 localTargetPos = new Vector3(
            _currentColumn * spacingX,
            _currentRow * spacingY,
            0f 
        );

        EnemyBase enemyScript = go.GetComponent<EnemyBase>();
        if (enemyScript != null)
        {
            enemyScript.SetTargetLocalPosition(localTargetPos);

            if (!enemyScript.UseGridFormation)
            {
                Debug.LogWarning($"[Внимание] Вы заспавнили врага {go.name} через GridSpawner, но на его префабе не стоит галочка 'Use Grid Formation'!", go);
            }
        }
        else
        {
            Debug.LogError($"На префабе {go.name} отсутствует скрипт, наследуемый от EnemyBase!", go);
        }

        _currentColumn++;
        if (_currentColumn >= maxColumns)
        {
            _currentColumn = 0;
            _currentRow++;
            
            if (_currentRow >= maxRows)
            {
                _formationComplete = true;
                Debug.Log("Сетка врагов полностью заполнена!");
                return;
            }
        }

        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }

    // Вспомогательный метод для изменения слоя объекта и всех его подобъектов (моделей, крыльев, деталей)
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        
        obj.layer = newLayer;
        
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}