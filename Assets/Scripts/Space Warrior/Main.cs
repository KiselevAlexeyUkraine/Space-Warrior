using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    static public Main S;
    static Dictionary<WeaponType, WeaponDefinition> WEAP_DICT;

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

    public void ShipDestroyed(Enemy e)
    {
        if (Random.value <= e.powerUpDropChance)
        {
            int ndx = Random.Range(0, powerUpFrequency.Length);
            WeaponType puType = powerUpFrequency[ndx];

            GameObject go = Instantiate(prefabPowerUp);
            PowerUp pu = go.GetComponent<PowerUp>();

            pu.SetType(puType);
            pu.transform.position = e.transform.position;
        }
    }

    private void Awake()
    {
        S = this;

        bndCheck = GetComponent<BoundsCheck>();

        Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);

        WEAP_DICT = new Dictionary<WeaponType, WeaponDefinition>();

        foreach (WeaponDefinition def in weaponDefinitions)
        {
            WEAP_DICT[def.type] = def;
        }
    }

    public void SpawnEnemy()
    {
        int ndx = Random.Range(0, prefabEnemies.Length);

        GameObject go = Instantiate(prefabEnemies[ndx]);

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
        if (WEAP_DICT.ContainsKey(wt))
        {
            return WEAP_DICT[wt];
        }

        return new WeaponDefinition();
    }
}