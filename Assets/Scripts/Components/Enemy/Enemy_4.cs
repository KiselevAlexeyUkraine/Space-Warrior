using UnityEngine;
using Components.Weapon;
using Level;

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
        private bool _movementInitialized = false; 

        private void Start()
        {
            foreach (Part prt in parts)
            {
                Transform t = transform.Find(prt.name);
                if (t != null)
                {
                    prt.go = t.gameObject;
                    prt.mat = prt.go.GetComponent<Renderer>().material;
                    prt.startHealth = prt.health; 
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (parts == null) return;

            foreach (Part prt in parts)
            {
                if (prt.go != null)
                {
                    prt.go.SetActive(true);
                    prt.health = prt.startHealth;
                }
            }

            _movementInitialized = false; 
        }

        private void InitMovement()
        {
            p0 = p1 = pos; 
            
            float widMinRad = bndCheck.camWidth - bndCheck.radius;
            float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
            
            p1.x = Random.Range(-widMinRad, widMinRad);
            p1.y = Random.Range(-hgtMinRad, hgtMinRad);

            timeStart = Time.time;
            _movementInitialized = true;
        }

        public override void Move()
        {
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

        protected override void OnTriggerEnter(Collider other)
        {
            GameObject otherGo = other.gameObject;

            if (otherGo.CompareTag("Hero") || otherGo.CompareTag("Player"))
            {
                base.OnTriggerEnter(other);
                return;
            }

            if (!otherGo.CompareTag("ProjectileHero")) return;

            if (bndCheck != null && bndCheck.enabled && !bndCheck.isOnScreen)
            {
                SafeDespawn(otherGo);
                return;
            }
            
            Part prtHit = null;

            foreach (Part prt in parts)
            {
                if (prt.go != null)
                {
                    Collider partCollider = prt.go.GetComponent<Collider>();
                    
                    // Теперь компилятор успешно найдет этот метод расширения из класса ниже
                    if (partCollider != null && partCollider.BoundsIntersect(other)) 
                    {
                        prtHit = prt;
                        break;
                    }
                }
            }

            if (prtHit == null)
            {
                foreach (Part prt in parts)
                {
                    if (!Destroyed(prt))
                    {
                        prtHit = prt;
                        break;
                    }
                }
            }

            if (prtHit == null)
            {
                SafeDespawn(otherGo);
                return;
            }

            if (prtHit.protectedBy != null)
            {
                foreach (string s in prtHit.protectedBy)
                {
                    if (!Destroyed(s))
                    {
                        SafeDespawn(otherGo); 
                        return; 
                    }
                }
            }

            Projectile p = otherGo.GetComponent<Projectile>();
            if (p != null)
            {
                WeaponConfig config = main.GetWeaponConfig(p.type);

                if (config != null)
                {
                    prtHit.health -= config.damageOnHit;
                }
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
                health = 0;
                ProcessDamage(p != null ? p.type : WeaponType.blaster);
            }
            
            SafeDespawn(otherGo); 
        }

        private Part FindPart(string n)
        {
            foreach (Part prt in parts)
            {
                if (prt.name == n) return prt;
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

    // ВЕРНУЛИ НА МЕСТО: Метод-расширение для проверки пересечения коллайдеров деталей
    public static class ColliderExtensions
    {
        public static bool BoundsIntersect(this Collider co, Collider other)
        {
            return co.bounds.Intersects(other.bounds);
        }
    }
}