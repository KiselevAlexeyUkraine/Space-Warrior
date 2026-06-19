using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;

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
    [Tooltip("Префаб врага Enemy_DontMove.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Скорость спавна (врагов в секунду).")]
    [SerializeField] private float spawnPerSecond = 2f;

    private BoundsCheck _bndCheck;
    private DiContainer _container;
    
    private int _currentColumn = 0;
    private int _currentRow = 0;
    private bool _formationComplete = false;

    [Inject]
    private void Construct(DiContainer container)
    {
        _container = container;
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
            Debug.LogWarning("Formation Root не задан! Испольуется позиция самого спавнера.", this);
        }

        // Запуск спавна по интервалу
        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }

    private void SpawnNextGridEnemy()
    {
        if (_formationComplete) return;

        // 1. Спавним врага через LeanPool
        GameObject go = LeanPool.Spawn(enemyPrefab);

        // 2. Внедряем Zenject зависимости (Utils, Main, etc.)
        _container.InjectGameObject(go);

        // 3. Задаем начальную позицию появления (сверху за границей экрана)
        float spawnY = _bndCheck.camHeight + 2f;
        float spawnX = Random.Range(-_bndCheck.camWidth + 1f, _bndCheck.camWidth - 1f);
        go.transform.position = new Vector3(spawnX, spawnY, 0);

        // 4. Рассчитываем целевую позицию внутри сетки относительно formationRoot
        Vector3 localTargetPos = new Vector3(
            _currentColumn * spacingX,
            _currentRow * spacingY, // Если нужно, чтобы ряды строились вниз, замени на: -_currentRow * spacingY
            0
        );
        Vector3 globalTargetGridPos = formationRoot.position + localTargetPos;

        // 5. Передаем точку компоненту врага
        Enemy_DontMove enemyScript = go.GetComponent<Enemy_DontMove>();
        if (enemyScript != null)
        {
            enemyScript.SetTargetPosition(globalTargetGridPos);
        }
        else
        {
            Debug.LogError("На префабе отсутствует скрипт Enemy_DontMove!", go);
        }

        // 6. Считаем индексы для следующего врага
        _currentColumn++;
        if (_currentColumn >= maxColumns)
        {
            _currentColumn = 0;
            _currentRow++;
            
            if (_currentRow >= maxRows)
            {
                _formationComplete = true;
                Debug.Log("Сетка врагов полностью заполнена!");
                return; // Прекращаем вызовы Invoke
            }
        }

        // Повторяем вызов для следующего элемента
        Invoke(nameof(SpawnNextGridEnemy), 1f / spawnPerSecond);
    }
}