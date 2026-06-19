using UnityEngine;
using Components.Weapon;
using Lean.Pool;

namespace Components.Enemy
{
    [System.Serializable]
    public class Part
    {
        public string name; 
        public float health; 
        [HideInInspector] public float startHealth; // Запоминаем стартовое ХП для пула
        public string[] protectedBy; 

        [HideInInspector] public GameObject go; 
        [HideInInspector] public Material mat; 
    }

    public sealed class Enemy_4 : EnemyBase
    {
        [Header("Set in Inspector: Enemy_4")]
        [SerializeField] private Part[] parts; 
        
        private Vector3 p0, p1;  
        private float timeStart; 
        private float duration = 4f; 
        private bool _movementInitialized = false; // Флаг, чтобы отследить, заданы ли правильные координаты

        private void Start()
        {
            // Кешируем компоненты деталей один раз при самом первом старте
            foreach (Part prt in parts)
            {
                Transform t = transform.Find(prt.name);
                if (t != null)
                {
                    prt.go = t.gameObject;
                    prt.mat = prt.go.GetComponent<Renderer>().material;
                    prt.startHealth = prt.health; // Кешируем базовое здоровье части
                }
            }
        }

        // Вызывается каждый раз, когда LeanPool достает Enemy_4 обратно на сцену
        protected override void OnEnable()
        {
            base.OnEnable();

            if (parts == null) return;

            // Восстанавливаем все части корабля обратно для нового спавна из пула
            foreach (Part prt in parts)
            {
                if (prt.go != null)
                {
                    prt.go.SetActive(true);
                    prt.health = prt.startHealth;
                }
            }

            // Сбрасываем флаг движения. Мы инициализируем его только тогда, 
            // когда объект займет новые координаты, установленные скриптом Main
            _movementInitialized = false; 
        }

        private void InitMovement()
        {
            p0 = p1 = pos; // Теперь pos будет равен новой позиции сверху экрана, заданной из Main!
            
            float widMinRad = bndCheck.camWidth - bndCheck.radius;
            float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
            
            p1.x = Random.Range(-widMinRad, widMinRad);
            p1.y = Random.Range(-hgtMinRad, hgtMinRad);

            timeStart = Time.time;
            _movementInitialized = true;
        }

        public override void Move()
        {
            // Если мы только появились на экране и Main уже установил нам позицию,
            // инициализируем движение от этой новой позиции за экраном
            if (!_movementInitialized)
            {
                InitMovement();
            }

            float u = (Time.time - timeStart) / duration;

            if (u >= 1)
            {
                InitMovement();
                u = 0;
            }

            u = 1 - Mathf.Pow(1 - u, 2); 
            pos = (1 - u) * p0 + u * p1; 
        }

        protected override void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.gameObject;
            if (!other.CompareTag("ProjectileHero")) return;

            if (bndCheck != null && !bndCheck.isOnScreen)
            {
                SafeDespawn(other);
                return;
            }

            GameObject goHit = collision.contacts[0].thisCollider.gameObject;
            Part prtHit = FindPart(goHit);
            
            if (prtHit == null)
            {
                goHit = collision.contacts[0].otherCollider.gameObject;
                prtHit = FindPart(goHit);
            }

            if (prtHit == null)
            {
                SafeDespawn(other);
                return;
            }

            if (prtHit.protectedBy != null)
            {
                foreach (string s in prtHit.protectedBy)
                {
                    if (!Destroyed(s))
                    {
                        SafeDespawn(other); 
                        return; 
                    }
                }
            }

            Projectile p = other.GetComponent<Projectile>();
            var weaponDef = Main.GetWeaponDefinition(p.type);
            
            if (weaponDef != null)
            {
                prtHit.health -= weaponDef.damageOnHit;
            }
            
            ShowLocalizedDamage(prtHit.mat);

            if (prtHit.health <= 0)
            {
                prtHit.go.SetActive(false);
            }

            bool allDestroyed = true;
            foreach (Part prt in parts)
            {
                if (!Destroyed(prt))
                {
                    allDestroyed = false;
                    break; 
                }
            }

            if (allDestroyed)
            {
                if (main != null)
                {
                    main.ShipDestroyed(this);
                }
                SafeDespawn(other);
                return; 
            }
            
            SafeDespawn(other); 
        }

        private void SafeDespawn(GameObject go)
        {
            if (go == null) return;

            if (LeanPool.Links.ContainsKey(go))
            {
                LeanPool.Despawn(go);
            }
            else
            {
                Destroy(go);
            }
        }

        private Part FindPart(string n)
        {
            foreach (Part prt in parts)
            {
                if (prt.name == n) return prt;
            }
            return null;
        }

        private Part FindPart(GameObject targetGo)
        {
            foreach (Part prt in parts)
            {
                if (prt.go == targetGo) return prt;
            }
            return null;
        }

        private bool Destroyed(string n) => Destroyed(FindPart(n));
        private bool Destroyed(Part prt) => prt == null || prt.health <= 0;

        private void ShowLocalizedDamage(Material m)
        {
            if (m != null) m.color = Color.red;
            damageDoneTime = Time.time + showDamageDuration;
            showingDamage = true;
        }
    }
}