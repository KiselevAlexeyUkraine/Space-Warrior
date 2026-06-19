using UnityEngine;
using Components.Weapon;
using Components.Player.Helpers;
using Components.Player; 
using Zenject;
using Lean.Pool;
using Level;

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

        [Header("Grid Formation (Новое)")]
        [Tooltip("Если галочка стоит, враг летит на локальную позицию сетки formationRoot и отключает BoundsCheck.")]
        [SerializeField] private bool useGridFormation = false;

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

        // Переменные для логики движения по сетке
        private Vector3 _targetLocalPosition;
        private bool _hasGridTarget = false;
        private bool _reachedGridTarget = false;
        private float _maxHealthCached;

        protected BoundsCheck bndCheck;
        protected Main main;

        public Vector3 pos
        {
            get => transform.position;
            set => transform.position = value;
        }

        public bool UseGridFormation => useGridFormation;
        
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
            _maxHealthCached = health;
        }

        protected virtual void Start()
        {
            EnsureMaterialsInitialized();
        }

        protected virtual void OnEnable()
        {
            notifiedOfDestruction = false;
            showingDamage = false;
            health = _maxHealthCached;

            _hasGridTarget = false;
            _reachedGridTarget = false;

            EnsureMaterialsInitialized();

            if (_isInitialized && materials != null)
            {
                UnShowDamage();
            }

            // Автоматически настраиваем BoundsCheck в зависимости от типа движения
            if (bndCheck != null)
            {
                bndCheck.enabled = !useGridFormation;
            }
        }

        private void EnsureMaterialsInitialized()
        {
            if (_isInitialized) return;

            if (_utils == null)
            {
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
            // Главное разделение логики перемещения
            if (useGridFormation)
            {
                HandleGridMovement();
            }
            else
            {
                Move();
            }

            if (showingDamage && Time.time > damageDoneTime)
            {
                UnShowDamage();
            }
            
            if (bndCheck != null && bndCheck.enabled && bndCheck.offDown)
            {
                SafeDespawn(gameObject);
            }
        }

        public abstract void Move();

        // Логика полета в точку построения сетки
        private void HandleGridMovement()
        {
            if (!_hasGridTarget || _reachedGridTarget) return;

            if (Vector3.Distance(transform.localPosition, _targetLocalPosition) > 0.05f)
            {
                Vector3 nextLocalPos = Vector3.MoveTowards(transform.localPosition, _targetLocalPosition, speed * Time.deltaTime);
                nextLocalPos.z = 0f; 
                transform.localPosition = nextLocalPos;
            }
            else
            {
                transform.localPosition = _targetLocalPosition;
                _reachedGridTarget = true;
            }
        }

        // Публичный метод, вызываемый из спавнера вместо удаления старого скрипта
        public void SetTargetLocalPosition(Vector3 localTargetPos)
        {
            _targetLocalPosition = new Vector3(localTargetPos.x, localTargetPos.y, 0f);
            _hasGridTarget = true;
            _reachedGridTarget = false;
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            GameObject otherGo = other.gameObject;

            Debug.Log($"[ОБЩИЙ ТРИГГЕР] Враг {gameObject.name} пересёкся с объектом: {otherGo.name} (Тег: {otherGo.tag})");
            
            if (otherGo.CompareTag("ProjectileHero"))
            {
                Debug.Log($"[ПОПАДАНИЕ СРАБОТАЛО] В {gameObject.name} прилетела триггерная пуля {otherGo.name}");
                
                Projectile p = otherGo.GetComponent<Projectile>();
                if (p != null)
                {
                    ProcessDamage(p.type);
                }
                
                SafeDespawn(otherGo);
            }
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

        protected void SafeDespawn(GameObject go)
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