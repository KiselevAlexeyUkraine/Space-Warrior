using UnityEngine;
using Lean.Pool;
using UnityEngine.Serialization;
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
        
        [Header("===== References =====")]
        [Tooltip("Дуло или визуальная часть оружия. Отсюда будут вылетать снаряды и этот объект меняет цвет под конфигурацию оружия.")]
        [SerializeField]
        private GameObject firePoint;

        private Renderer _collarRenderer;
        private float _lastShotTime;

        public WeaponType Type => _type;
        public WeaponConfig Config => _config;

        private void Awake()
        {
            if (firePoint == null)
            {
                Transform collarTransform = transform.Find("Collar");

                if (collarTransform != null)
                {
                    firePoint = collarTransform.gameObject;
                }
            }

            if (firePoint != null)
            {
                _collarRenderer = firePoint.GetComponent<Renderer>();
            }
        }

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

            if (_collarRenderer != null)
            {
                _collarRenderer.material.color =
                    _config.weaponColor;
            }

            _lastShotTime = 0f;
        }

        private void Fire()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (_config == null)
                return;

            if (Time.time - _lastShotTime < _config.fireRate)
                return;

            Vector3 velocity =
                Vector3.up * _config.projectileSpeed;

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

            if (projectile != null &&
                projectile.rigid != null)
            {
                projectile.rigid.linearVelocity =
                    baseVelocity;
            }
        }

        private void FireSpread(Vector3 baseVelocity)
        {
            int count =
                Mathf.Max(1, _config.projectileCount);

            float spread =
                _config.spreadAngle;

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

                Projectile projectile =
                    MakeProjectile();

                if (projectile == null ||
                    projectile.rigid == null)
                    continue;

                Quaternion rotation =
                    Quaternion.AngleAxis(
                        angle,
                        Vector3.back);

                projectile.transform.rotation =
                    rotation;

                projectile.rigid.linearVelocity =
                    rotation * baseVelocity;
            }
        }

        private Projectile MakeProjectile()
        {
            if (_config == null)
                return null;

            if (_config.projectilePrefab == null)
                return null;

            // ИСПРАВЛЕНО: Убран projectileAnchor, теперь спавн идет без указания родителя (в корень сцены)
            GameObject projectileGo =
                LeanPool.Spawn(
                    _config.projectilePrefab,
                    firePoint.transform.position,
                    Quaternion.identity);

            if (transform.root.CompareTag("Hero"))
            {
                projectileGo.tag = "ProjectileHero";
                projectileGo.layer =
                    LayerMask.NameToLayer("ProjectileHero");
            }

            Projectile projectile =
                projectileGo.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.type = _type;
            }

            LeanPool.Despawn(
                projectileGo,
                _config.projectileLifeTime);

            return projectile;
        }
    }
}