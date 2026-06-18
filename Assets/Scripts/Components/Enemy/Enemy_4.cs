using UnityEngine;

namespace Components.Enemy
{
    [System.Serializable]
    public class Part
    {
        public string name; 
        public float health; 
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

        private void Start()
        {
            p0 = p1 = pos;
            
            // ОПТИМИЗАЦИЯ: Кешируем компоненты деталей ОДИН раз при старте, а не каждый кадр в Update/Move!
            foreach (Part prt in parts)
            {
                Transform t = transform.Find(prt.name);
                if (t != null)
                {
                    prt.go = t.gameObject;
                    prt.mat = prt.go.GetComponent<Renderer>().material;
                }
            }
            
            InitMovement();
        }

        private void InitMovement()
        {
            p0 = p1; 
            float widMinRad = bndCheck.camWidth - bndCheck.radius;
            float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
            
            p1.x = Random.Range(-widMinRad, widMinRad);
            p1.y = Random.Range(-hgtMinRad, hgtMinRad);

            timeStart = Time.time;
        }

        public override void Move()
        {
            float u = (Time.time - timeStart) / duration;

            if (u >= 1)
            {
                InitMovement();
                u = 0;
            }

            u = 1 - Mathf.Pow(1 - u, 2); // Плавное замедление (Ease Out)
            pos = (1 - u) * p0 + u * p1; 
        }

        protected override void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.gameObject;
            if (!other.CompareTag("ProjectileHero")) return;

            if (bndCheck != null && !bndCheck.isOnScreen)
            {
                Destroy(other);
                return;
            }

            // Определяем, в какую конкретно деталь попали
            GameObject goHit = collision.contacts[0].thisCollider.gameObject;
            Part prtHit = FindPart(goHit);
            
            if (prtHit == null)
            {
                goHit = collision.contacts[0].otherCollider.gameObject;
                prtHit = FindPart(goHit);
            }

            if (prtHit == null)
            {
                Destroy(other);
                return;
            }

            // Проверяем защиту детали
            if (prtHit.protectedBy != null)
            {
                foreach (string s in prtHit.protectedBy)
                {
                    if (!Destroyed(s))
                    {
                        Destroy(other); 
                        return; 
                    }
                }
            }

            // Наносим урон конкретной детали
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

            // Проверяем полную деструкцию корабля
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
                Main.S.ShipDestroyed(this);
                Destroy(gameObject);
            }
            
            Destroy(other); 
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
            m.color = Color.red;
            damageDoneTime = Time.time + showDamageDuration;
            showingDamage = true;
        }
    }
}