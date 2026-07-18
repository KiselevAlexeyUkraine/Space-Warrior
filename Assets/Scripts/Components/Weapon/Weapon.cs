using UnityEngine;
using Lean.Pool;
using Zenject;

namespace Components.Weapon
{
    public sealed class Weapon : MonoBehaviour
    {
        [Header("===== Weapon Settings =====")]
        [Tooltip("Тип оружия (например: blaster, spread). Определяет логику стрельбы и паттерн снарядов.")]
        [SerializeField]
        private WeaponType _type = WeaponType.none;

        [Inject]
        private WeaponConfig _config; 

        [Inject]
        private BalanceConfig _balanceConfig; 

        [Inject]
        private DiContainer _container; 

        [Header("===== References =====")]
        [Tooltip("Точка (Трансформ), из которой будут физически спавниться снаряды при выстреле.")]
        [SerializeField]
        private Transform firePoint;

        private float _lastShotTime;

        public WeaponType Type => _type;
        public WeaponConfig Config => _config;

        private float DynamicFireRate => _balanceConfig.PlayerFireRate;
        private float DynamicProjectileSpeed => _balanceConfig.PlayerBulletSpeed;

        private void Start()
        {
            SetType(_type);
        }

        private void Update()
        {
            Fire();
        }

        public void SetType(WeaponType weaponType)
        {
            _type = weaponType;

            if (_type == WeaponType.none)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_config == null)
            {
                Debug.LogError($"{name}: WeaponConfig не назначен.");
                return;
            }

            if (_balanceConfig == null)
            {
                Debug.LogError($"{name}: BalanceConfig не внедрен через Zenject.");
                return;
            }
            
            _lastShotTime = 0f;
        }

        private void Fire()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (_config == null || _balanceConfig == null)
                return;

            // ИСПРАВЛЕНИЕ: Ждем, пока таблица полностью загрузится из сети
            if (!_balanceConfig.IsLoaded)
                return;

            // Проверка кулдауна по значению из BalanceConfig
            if (Time.time - _lastShotTime < DynamicFireRate)
                return;

            // Скорость полета берется из BalanceConfig
            Vector3 velocity = Vector3.up * DynamicProjectileSpeed;

            if (transform.up.y < 0)
            {
                velocity.y *= -1f;
            }

            switch (_type)
            {
                case WeaponType.blaster:
                    FireBlaster(velocity);
                    break;

                case WeaponType.spread:
                    FireSpread(velocity);
                    break;

                default:
                    FireBlaster(velocity);
                    break;
            }

            _lastShotTime = Time.time;
        }

        private void FireBlaster(Vector3 baseVelocity)
        {
            Projectile projectile = MakeProjectile();

            if (projectile != null && projectile.Rigid != null)
            {
                projectile.Rigid.linearVelocity = baseVelocity;
            }
        }

        private void FireSpread(Vector3 baseVelocity)
        {
            int count = Mathf.Max(1, _config.projectileCount);
            float spread = _config.spreadAngle;

            if (count == 1)
            {
                FireBlaster(baseVelocity);
                return;
            }

            float startAngle = -(spread * 0.5f);
            float step = spread / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;

                Projectile projectile = MakeProjectile();

                if (projectile == null || projectile.Rigid == null)
                    continue;

                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.back);
                projectile.transform.rotation = rotation;
                projectile.Rigid.linearVelocity = rotation * baseVelocity;
            }
        }

        private Projectile MakeProjectile()
        {
            if (_config == null || _config.projectilePrefab == null)
                return null;

            GameObject projectileGo = LeanPool.Spawn(
                _config.projectilePrefab,
                firePoint.position,
                Quaternion.identity);

            _container.InjectGameObject(projectileGo);

            Projectile projectile = projectileGo.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.type = _type;
                
                // ИСПРАВЛЕНИЕ: Полностью сбрасываем старую скорость Rigidbody из пула перед тем, как задать новую
                if (projectile.Rigid != null)
                {
                    projectile.Rigid.linearVelocity = Vector3.zero;
                    projectile.Rigid.angularVelocity = Vector3.zero;
                }
            }

            LeanPool.Despawn(projectileGo, _config.projectileLifeTime);

            return projectile;
        }
    }
}