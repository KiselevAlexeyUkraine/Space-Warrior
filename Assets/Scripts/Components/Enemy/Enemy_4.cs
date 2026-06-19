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
            // Кешируем компоненты деталей один раз при самом первом старте
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

            // Восстанавливаем все части корабля обратно для нового спавна из пула
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

        // ИСПРАВЛЕНО: Переопределяем метод под триггеры
        protected override void OnTriggerEnter(Collider other)
        {
            GameObject otherGo = other.gameObject;

            // 1. Проброс тарана игрока в базовый класс EnemyBase
            if (otherGo.CompareTag("Hero") || otherGo.CompareTag("Player"))
            {
                base.OnTriggerEnter(other);
                return;
            }

            // 2. Обработка попадания пули
            if (!otherGo.CompareTag("ProjectileHero")) return;

            // На всякий случай проверяем видимость, если bndCheck включен
            if (bndCheck != null && bndCheck.enabled && !bndCheck.isOnScreen)
            {
                SafeDespawn(otherGo);
                return;
            }
            
            Part prtHit = null;

            // Проверяем, какой из коллайдеров частей пересекается с пулей
            foreach (Part prt in parts)
            {
                if (prt.go != null)
                {
                    Collider partCollider = prt.go.GetComponent<Collider>();
                    if (partCollider != null && partCollider.BoundsIntersect(other)) // Вспомогательная проверка
                    {
                        prtHit = prt;
                        break;
                    }
                }
            }

            // Если быстрый поиск по границам не сработал, берем дефолтную первую живую деталь как fallback
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

            // Проверяем защиту детали другой деталью
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

            // Наносим урон детали
            Projectile p = otherGo.GetComponent<Projectile>();
            if (p != null)
            {
                var weaponDef = Main.GetWeaponDefinition(p.type);
                if (weaponDef != null)
                {
                    prtHit.health -= weaponDef.damageOnHit;
                }
            }
            
            ShowLocalizedDamage(prtHit.mat);

            // Если деталь уничтожена, отключаем её
            if (prtHit.health <= 0)
            {
                prtHit.go.SetActive(false);
            }

            // Проверяем, уничтожен ли весь корабль полностью
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
                // ИСПРАВЛЕНО: Обнуляем здоровье для базового класса, чтобы сработал правильный деспавн, звуки и очки в EnemyBase
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

    // Небольшой метод-расширение, помогающий триггерам определять пересечение деталей
    public static class ColliderExtensions
    {
        public static bool BoundsIntersect(this Collider co, Collider other)
        {
            return co.bounds.Intersects(other.bounds);
        }
    }
}