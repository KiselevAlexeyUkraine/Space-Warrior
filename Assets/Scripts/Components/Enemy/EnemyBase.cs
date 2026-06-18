using UnityEngine;
using Components.Weapon;
using Components.Player.Helpers;
using Components.Player; // Подключаем пространство имен аудио-сервиса
using Zenject;

namespace Components.Enemy
{
    [RequireComponent(typeof(BoundsCheck))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Параметры врага")]
        [SerializeField] protected float speed = 10f; 
        [SerializeField] protected float health = 10f;
        [SerializeField] private int score = 100; // Очки за уничтожение (для будущего использования)
        
        public float showDamageDuration = 0.1f; 
        public float powerUpDropChance = 1f; 

        [Header("Audio Settings")]
        [Tooltip("Звук взрыва (смерти) этого конкретного врага")]
        [SerializeField] private AudioClip _explosionClip; 

        [Header("Set Dynamically: Enemy")]
        [SerializeField] private Color[] originalColors;
        [SerializeField] private Material[] materials; 
        
        [HideInInspector] public bool showingDamage = false;
        [HideInInspector] public float damageDoneTime; 
        
        private bool notifiedOfDestruction = false; 
        private Utils _utils;
        private AudioService _audioService; // Поле для хранения ссылки на аудио-сервис
        private PlayerScore _playerScore;

        protected BoundsCheck bndCheck;

        public Vector3 pos
        {
            get => transform.position;
            set => transform.position = value;
        }
        
        [Inject]
        private void Construct(Utils utils, AudioService audioService, PlayerScore playerScore)
        {
            _utils = utils;
            _audioService = audioService;
            _playerScore = playerScore;
        }

        protected virtual void Awake()
        {
            bndCheck = GetComponent<BoundsCheck>();
            
            materials = _utils.GetAllMaterials(gameObject); 
            originalColors = new Color[materials.Length];
            
            for (int i = 0; i < materials.Length; i++)
            {
                originalColors[i] = materials[i].color;
            }
        }

        protected virtual void Update()
        {
            Move();

            if (showingDamage && Time.time > damageDoneTime)
            {
                UnShowDamage();
            }
            
            if (bndCheck != null && bndCheck.offDown)
            {
                Destroy(gameObject);
            }
        }

        // Абстрактный метод: каждый тип врага ОБЯЗАН реализовать свой алгоритм перемещения
        public abstract void Move();

        protected virtual void OnCollisionEnter(Collision collision)
        {
            GameObject otherGo = collision.gameObject;
            
            if (!otherGo.CompareTag("ProjectileHero")) return;

            Projectile p = otherGo.GetComponent<Projectile>();

            // Если враг еще не на экране, игнорируем урон
            if (bndCheck != null && !bndCheck.isOnScreen)
            {
                Destroy(otherGo);
                return;
            }

            ProcessDamage(p.type);
            Destroy(otherGo);
        }

        protected void ProcessDamage(WeaponType weaponType)
        {
            ShowDamage();
    
            var weaponDef = Main.GetWeaponDefinition(weaponType);
            if (weaponDef != null)
            {
                health -= weaponDef.damageOnHit;
            }

            if (health <= 0)
            {
                if (!notifiedOfDestruction)
                {
                    if (_audioService != null && _explosionClip != null)
                    {
                        _audioService.PlayOneShot(_explosionClip);
                    }

                    // НАЧИСЛЯЕМ ОЧКИ ПЕРЕД СМЕРТЬЮ
                    _playerScore?.AddScore(score);

                    Main.S.ShipDestroyed(this);
                    notifiedOfDestruction = true;
                }
                Destroy(gameObject);
            }
        }

        private void ShowDamage()
        {
            foreach (Material m in materials)
            {
                m.color = Color.red;
            }
            showingDamage = true;
            damageDoneTime = Time.time + showDamageDuration;
        }

        private void UnShowDamage()
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    materials[i].color = originalColors[i];
            }
            showingDamage = false;
        }
    }
}