using System.Collections.Generic;
using Components.Items;
using Components.Weapon;
using Components.Enemy;
using UnityEngine;
using Zenject;
using Lean.Pool;

public class Main : MonoBehaviour
{
    private static Dictionary<WeaponType, WeaponDefinition> _weapDict;

    [Header("Set in Inspector")]
    [SerializeField] private GameObject[] prefabEnemies;
    [SerializeField] private float enemySpawnPerSecond = 0.5f;
    [SerializeField] private float enemyDefaultPadding = 1.5f;
    [SerializeField] private WeaponDefinition[] weaponDefinitions;
    [SerializeField] private GameObject prefabPowerUp;

    [SerializeField] private WeaponType[] powerUpFrequency =
    {
        WeaponType.blaster,
        WeaponType.blaster,
        WeaponType.spread,
        WeaponType.shield
    };

    private BoundsCheck bndCheck;
    private DiContainer _container;

    [Inject]
    private void Construct(DiContainer container)
    {
        _container = container;
    }

    public void ShipDestroyed(EnemyBase e)
    {
        if (Random.value <= e.powerUpDropChance)
        {
            int ndx = Random.Range(0, powerUpFrequency.Length);
            WeaponType puType = powerUpFrequency[ndx];

            // На будущее: бонусы (PowerUp) тоже можно перевести на LeanPool.Spawn аналогично врагам
            GameObject go = Instantiate(prefabPowerUp);
            PowerUp pu = go.GetComponent<PowerUp>();

            pu.SetType(puType);
            pu.transform.position = e.transform.position;
        }

        // ЗАМЕНЕНО: вместо Destroy(gameObject) внутри EnemyBase/Enemy_4, 
        // теперь деспавним уничтоженного врага через пул
        if (LeanPool.Links.ContainsKey(e.gameObject))
        {
            LeanPool.Despawn(e.gameObject);
        }
        else
        {
            Destroy(e.gameObject);
        }
    }

    private void Awake()
    {
        bndCheck = GetComponent<BoundsCheck>();

        Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);

        _weapDict = new Dictionary<WeaponType, WeaponDefinition>();

        foreach (WeaponDefinition def in weaponDefinitions)
        {
            _weapDict[def.type] = def;
        }
    }

    public void SpawnEnemy()
    {
        int ndx = Random.Range(0, prefabEnemies.Length);

        // ЗАМЕНЕНО: Спавним врага через LeanPool вместо Container.InstantiatePrefab
        GameObject go = LeanPool.Spawn(prefabEnemies[ndx]);

        // ВАЖНО: Накатываем инжекцию Zenject на заспавненный из пула объект
        _container.InjectGameObject(go);

        float enemyPadding = enemyDefaultPadding;
        BoundsCheck enemyBounds = go.GetComponent<BoundsCheck>();

        if (enemyBounds != null)
        {
            enemyPadding = Mathf.Abs(enemyBounds.radius);
        }

        Vector3 pos = Vector3.zero;

        float xMin = -bndCheck.camWidth - enemyPadding + 2f;
        float xMax = bndCheck.camWidth - enemyPadding - 2f;

        pos.x = Random.Range(xMin, xMax);
        pos.y = bndCheck.camHeight + enemyPadding;

        go.transform.position = pos;

        Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
    }

    public static WeaponDefinition GetWeaponDefinition(WeaponType wt)
    {
        if (_weapDict.ContainsKey(wt))
        {
            return _weapDict[wt];
        }

        return new WeaponDefinition();
    }
}