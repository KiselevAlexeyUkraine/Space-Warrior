using UnityEngine;
using Components.Weapon;
using Components.Player.Helpers;
using Components.Player; 
using Zenject;
using Lean.Pool;

namespace Components.Enemy
{
    [RequireComponent(typeof(BoundsCheck))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Параметры врага")]
        [SerializeField] protected float speed = 10f; 
        [SerializeField] protected float health = 10f;
        [SerializeField] private int score = 100; 
        
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
        private AudioService _audioService; 
        private PlayerScore _playerScore;
        private bool _isInitialized = false;

        protected BoundsCheck bndCheck;
        
        protected Main main;

        public Vector3 pos
        {
            get => transform.position;
            set => transform.position = value;
        }
        
        [Inject]
        private void Construct(Utils utils, AudioService audioService, PlayerScore playerScore, Main main)
        {
            _utils = utils;
            _audioService = audioService;
            _playerScore = playerScore;
            this.main = main;
        }

        protected virtual void Awake()
        {
            bndCheck = GetComponent<BoundsCheck>();
        }

        protected virtual void Start()
        {
            // Инициализируем материалы один раз; допускаем падение Zenject для объектов из пула
            EnsureMaterialsInitialized();
        }

        protected virtual void OnEnable()
        {
            notifiedOfDestruction = false;
            showingDamage = false;

            // Если враг достается из пула повторно, сбрасываем цвета к исходным
            // Попробуем инициализировать материалы, если это не было сделано в Start (например при пуллинге)
            EnsureMaterialsInitialized();

            if (_isInitialized && materials != null)
            {
                UnShowDamage();
            }
        }

        // Инициализация материалов и запоминание исходных цветов.
        // Делаем локальный fallback на new Utils(), т.к. объекты, создаваемые через пуллинги,
        // могут не получить Zenject-инъекции.
        private void EnsureMaterialsInitialized()
        {
            if (_isInitialized) return;

            if (_utils == null)
            {
                // Utils не зависит от Unity/Zenject, можно создать локально как fallback
                _utils = new Utils();
            }

            materials = _utils.GetAllMaterials(gameObject) ?? new Material[0];
            originalColors = new Color[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                originalColors[i] = materials[i] != null ? materials[i].color : Color.white;
            }

            _isInitialized = true;
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
                if (LeanPool.Links.ContainsKey(gameObject))
                {
                    LeanPool.Despawn(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        public abstract void Move();

        protected virtual void OnCollisionEnter(Collision collision)
        {
            GameObject otherGo = collision.gameObject;
            
            if (!otherGo.CompareTag("ProjectileHero")) return;

            Projectile p = otherGo.GetComponent<Projectile>();

            if (bndCheck != null && !bndCheck.isOnScreen)
            {
                SafeDespawn(otherGo);
                return;
            }

            ProcessDamage(p.type);
            SafeDespawn(otherGo);
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

                    _playerScore?.AddScore(score);
                    notifiedOfDestruction = true;
                    
                    main.ShipDestroyed(this);
                }
            }
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

        // Найди эти методы в самом низу EnemyBase.cs и замени private на protected:

        protected void ShowDamage()
        {
            if (materials == null) return;
            foreach (Material m in materials)
            {
                if (m != null) m.color = Color.red;
            }
            showingDamage = true;
            damageDoneTime = Time.time + showDamageDuration;
        }

        protected void UnShowDamage()
        {
            if (materials == null) return;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && i < originalColors.Length)
                    materials[i].color = originalColors[i];
            }
            showingDamage = false;
        }
    }
}