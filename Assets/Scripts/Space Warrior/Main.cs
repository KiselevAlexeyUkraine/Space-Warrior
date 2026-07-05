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
        private Dictionary<WeaponType, WeaponConfig> _weaponConfigs;

        [Header("Enemy Spawn")]
        [Tooltip("Если выключено, враги не будут спавниться.")]
        [SerializeField] private bool spawnEnemies = true;

        [Tooltip("Префабы врагов.")]
        [SerializeField] private GameObject[] prefabEnemies;

        [Tooltip("Количество врагов в секунду.")]
        [SerializeField] private float enemySpawnPerSecond = 0.5f;

        [Tooltip("Отступ врага от границы экрана.")]
        [SerializeField] private float enemyDefaultPadding = 1.5f;

        [Header("PowerUps")]
        [Tooltip("Разрешить выпадение PowerUp с врагов.")]
        [SerializeField] private bool enablePowerUpDrops = true;

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

        [Inject]
        private void Construct(DiContainer container, List<WeaponConfig> injectedConfigs)
        {
            _container = container;
            BuildWeaponDictionary(injectedConfigs);
        }

        private void Awake()
        {
            _boundsCheck = GetComponent<BoundsCheck>();
        }

        private void Start()
        {
            if (spawnEnemies)
            {
                Invoke(nameof(SpawnEnemy), 1f / enemySpawnPerSecond);
            }
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

            if (enablePowerUpDrops &&
                Random.value <= enemy.powerUpDropChance)
            {
                int index = Random.Range(0, powerUpFrequency.Length);
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
            }

            SafeDespawn(enemy.gameObject);
        }

        public void SpawnEnemy()
        {
            if (!spawnEnemies)
                return;

            int index = Random.Range(0, prefabEnemies.Length);

            GameObject enemyObject = LeanPool.Spawn(prefabEnemies[index]);

            _container?.InjectGameObject(enemyObject);

            float enemyPadding = enemyDefaultPadding;

            BoundsCheck enemyBounds = enemyObject.GetComponent<BoundsCheck>();

            if (enemyBounds != null)
            {
                enemyPadding = Mathf.Abs(enemyBounds.radius);
            }

            Vector3 position = Vector3.zero;

            float xMin = -_boundsCheck.camWidth - enemyPadding + 2f;
            float xMax = _boundsCheck.camWidth - enemyPadding - 2f;

            position.x = Random.Range(xMin, xMax);
            position.y = _boundsCheck.camHeight + enemyPadding;

            enemyObject.transform.position = position;

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