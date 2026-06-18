using UnityEngine;

namespace Components.Weapon
{
    public class Weapon : MonoBehaviour
    {
        public static Transform PROJECTILE_ANCHOR;

        [Header("Weapon Settings")]
        [SerializeField] private WeaponType _type = WeaponType.none;
        [SerializeField] private WeaponDefinition def;

        [Header("References")]
        [SerializeField] private GameObject collar;

        private Renderer collarRenderer;
        private float lastShotTime;

        public WeaponType Type => _type;
        public WeaponDefinition Definition => def;

        private void Awake()
        {
            // Автоматический поиск Collar, если не задан вручную
            if (collar == null)
            {
                Transform collarTransform = transform.Find("Collar");
                if (collarTransform != null)
                {
                    collar = collarTransform.gameObject;
                }
            }

            if (collar != null)
            {
                collarRenderer = collar.GetComponent<Renderer>();
            }

            // Создание якоря для снарядов на сцене
            if (PROJECTILE_ANCHOR == null)
            {
                GameObject anchor = new GameObject("_ProjectileAnchor");
                PROJECTILE_ANCHOR = anchor.transform;
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
            def = Main.GetWeaponDefinition(_type);

            if (collarRenderer != null && def != null)
            {
                collarRenderer.material.color = def.color;
            }

            lastShotTime = 0f;
        }

        private void Fire()
        {
            if (!gameObject.activeInHierarchy || def == null) 
                return;

            if (Time.time - lastShotTime < def.delayBetweenShots) 
                return;

            Vector3 velocity = Vector3.up * def.velocity;

            // Инверсия направления для врагов (если пушка смотрит вниз)
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
            }

            lastShotTime = Time.time;
        }

        private void FireBlaster(Vector3 baseVelocity)
        {
            Projectile proj = MakeProjectile();
            if (proj != null && proj.rigid != null) 
                proj.rigid.linearVelocity = baseVelocity;
        }

        private void FireSpread(Vector3 baseVelocity)
        {
            // Центральный снаряд
            Projectile projCenter = MakeProjectile();
            if (projCenter != null && projCenter.rigid != null) 
                projCenter.rigid.linearVelocity = baseVelocity;

            // Левый снаряд (+10 градусов)
            Projectile projLeft = MakeProjectile();
            if (projLeft != null && projLeft.rigid != null)
            {
                projLeft.transform.rotation = Quaternion.AngleAxis(10f, Vector3.back);
                projLeft.rigid.linearVelocity = projLeft.transform.rotation * baseVelocity;
            }

            // Правый снаряд (-10 градусов)
            Projectile projRight = MakeProjectile();
            if (projRight != null && projRight.rigid != null)
            {
                projRight.transform.rotation = Quaternion.AngleAxis(-10f, Vector3.back);
                projRight.rigid.linearVelocity = projRight.transform.rotation * baseVelocity;
            }
        }

        private Projectile MakeProjectile()
        {
            if (def == null || def.projectilePrefab == null) 
                return null;

            GameObject projGO = Instantiate(def.projectilePrefab);

            if (transform.root.CompareTag("Hero"))
            {
                projGO.tag = "ProjectileHero";
                projGO.layer = LayerMask.NameToLayer("ProjectileHero");
            }

            projGO.transform.position = collar.transform.position;
            projGO.transform.SetParent(PROJECTILE_ANCHOR, true);

            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.type = _type;
            }

            return projectile;
        }
    }
}