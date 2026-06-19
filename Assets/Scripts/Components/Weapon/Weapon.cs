using UnityEngine;
using Lean.Pool;
using Level;

namespace Components.Weapon
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private Transform projectileAnchor;

        [Header("Weapon Settings")]
        [SerializeField] private WeaponType _type = WeaponType.none;
        [SerializeField] private WeaponDefinition def;
        
        [Tooltip("Время жизни пули в секундах до возврата в пул")]
        [SerializeField] private float projectileLifeTime = 3f; 

        [Header("References")]
        [SerializeField] private GameObject collar;

        private Renderer collarRenderer;
        private float lastShotTime;

        public WeaponType Type => _type;
        public WeaponDefinition Definition => def;

        private void Awake()
        {
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

            if (projectileAnchor == null)
            {
                GameObject anchor = new GameObject("_ProjectileAnchor");
                projectileAnchor = anchor.transform;
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
            Projectile projCenter = MakeProjectile();
            if (projCenter != null && projCenter.rigid != null) 
                projCenter.rigid.linearVelocity = baseVelocity;

            Projectile projLeft = MakeProjectile();
            if (projLeft != null && projLeft.rigid != null)
            {
                projLeft.transform.rotation = Quaternion.AngleAxis(10f, Vector3.back);
                projLeft.rigid.linearVelocity = projLeft.transform.rotation * baseVelocity;
            }

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

            // Спавним пулю через LeanPool
            GameObject projGO = LeanPool.Spawn(
                def.projectilePrefab, 
                collar.transform.position, 
                Quaternion.identity, 
                projectileAnchor
            );

            if (transform.root.CompareTag("Hero"))
            {
                projGO.tag = "ProjectileHero";
                projGO.layer = LayerMask.NameToLayer("ProjectileHero");
            }

            Projectile projectile = projGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.type = _type;
            }

            // МАГИЯ ОДНОЙ СТРОКИ: Говорим пулу автоматически вернуть этот объект 
            // обратно в пул через указанное количество секунд (projectileLifeTime)
            LeanPool.Despawn(projGO, projectileLifeTime);

            return projectile;
        }
    }
}