using UnityEngine;
using Components.Weapon;

namespace Components.Enemy
{
    public class EnemyWithShield : EnemyBase
    {
        [Header("Настройки Щита Врага")]
        [Tooltip("Объект щита (с Renderer), который висит на враге")]
        [SerializeField] private GameObject shieldObject;
        
        [Tooltip("Текущий и максимальный уровень щита (целое число для текстуры)")]
        [SerializeField] private int maxShieldLevel = 4;
        [SerializeField] private float rotationsPerSecond = 0.1f;

        [Header("Настройки Регенерации Щита")]
        [Tooltip("Сколько единиц щита восстанавливается в секунду")]
        [SerializeField] private float regenRate = 1f;

        [Tooltip("Задержка перед началом восстановления после получения урона (в секундах)")]
        [SerializeField] private float regenDelay = 3f;

        private float currentShieldLevel;
        private Material shieldMat;
        private int lastShownLevel = -1;
        private float nextRegenTime; // Время, начиная с которого щит может регенерировать

        protected override void Awake()
        {
            base.Awake();
            
            if (shieldObject != null)
            {
                Renderer shieldRend = shieldObject.GetComponent<Renderer>();
                if (shieldRend != null)
                {
                    // Используем .material (создает копию для этого врага, чтобы не ломать общий материал)
                    shieldMat = shieldRend.material;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Сбрасываем уровень щита при активации из пула
            currentShieldLevel = maxShieldLevel;
            lastShownLevel = -1;
            nextRegenTime = 0f;
            
            if (shieldObject != null)
            {
                shieldObject.SetActive(true);
            }
            
            UpdateShieldVisual(forceUpdate: true);
        }

        public override void Move()
        {
            // Пример простого движения вниз, если не используется Grid Formation
            pos -= new Vector3(0, speed * Time.deltaTime, 0);
        }

        protected override void Update()
        {
            base.Update();

            // Если щит еще активен, вращаем его точно так же, как у игрока
            if (shieldObject != null && shieldObject.activeSelf)
            {
                float rZ = -(rotationsPerSecond * Time.time * 360f) % 360f;
                shieldObject.transform.rotation = Quaternion.Euler(0, 0, rZ);
            }

            // Логика регенерации щита
            HandleShieldRegeneration();
        }

        private void HandleShieldRegeneration()
        {
            // ИСПРАВЛЕНО: Убрана проверка !shieldObject.activeSelf, чтобы регенерация запускалась даже с нуля
            if (shieldObject == null || currentShieldLevel >= maxShieldLevel) 
                return;

            // Ждем пока пройдет время задержки после последнего попадания
            if (Time.time >= nextRegenTime)
            {
                // ИСПРАВЛЕНО: Если щит был выключен, включаем его обратно, так как пошло восстановление
                if (!shieldObject.activeSelf)
                {
                    shieldObject.SetActive(true);
                }

                // Постепенно увеличиваем щит, но не выше максимального значения
                currentShieldLevel += regenRate * Time.deltaTime;
                currentShieldLevel = Mathf.Min(currentShieldLevel, maxShieldLevel);

                UpdateShieldVisual();
            }
        }

        // Переопределяем получение урона, чтобы сначала страдал щит
        protected override void OnTriggerEnter(Collider other)
        {
            if (shieldObject != null && shieldObject.activeSelf && other.CompareTag("ProjectileHero"))
            {
                Projectile p = other.GetComponent<Projectile>();
                if (p != null)
                {
                    ProcessShieldDamage(p.type);
                }
                
                SafeDespawn(other.gameObject);
                return; // Выходим, чтобы базовый урон по корпусу не наносился
            }

            // Если щита нет — вызываем стандартную логику из EnemyBase
            base.OnTriggerEnter(other);
        }

        private void ProcessShieldDamage(WeaponType weaponType)
        {
            // Мигание красным цветом самого корабля при попадании в щит (по желанию можно убрать)
            ShowDamage(); 

            // Сдвигаем время начала регенерации в будущее (текущее время + задержка)
            nextRegenTime = Time.time + regenDelay;

            WeaponConfig config = main.GetWeaponConfig(weaponType);
            if (config != null)
            {
                currentShieldLevel -= config.damageOnHit;
            }

            if (currentShieldLevel <= 0)
            {
                currentShieldLevel = 0;
                if (shieldObject != null)
                {
                    shieldObject.SetActive(false); // Выключаем щит, теперь урон пойдет по броне
                }
            }

            UpdateShieldVisual();
        }

        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (shieldMat == null) return;

            // Округляем до ближайшего целого вниз, как в скрипте игрока
            int currentVisualLevel = Mathf.FloorToInt(currentShieldLevel);

            if (lastShownLevel != currentVisualLevel || forceUpdate)
            {
                lastShownLevel = currentVisualLevel;
                // Сдвиг текстуры по оси X (каждый уровень сдвигает на 0.2f)
                shieldMat.mainTextureOffset = new Vector2(0.2f * lastShownLevel, 0);
            }
        }
    }
}