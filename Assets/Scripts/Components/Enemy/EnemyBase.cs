using UnityEngine;
using Components.Weapon;
using Components.Player.Helpers;
using Components.Player; 
using Zenject;
using Lean.Pool;
using Level;
using System.Collections.Generic;

namespace Components.Enemy
{
    [RequireComponent(typeof(BoundsCheck))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Параметры врага (fallback, если BalanceConfig не внедрён)")]
        [Tooltip("Реальное значение возьмётся из BalanceConfig.GetEnemySpeed(enemyLevel) в Start(). Это поле используется только как запасной вариант.")]
        [SerializeField] protected float speed = 10f;

        [Tooltip("Реальное значение возьмётся из BalanceConfig.GetEnemyHealth(enemyLevel) в Start(). Это поле используется только как запасной вариант.")]
        [SerializeField] protected float health = 10f;
        [SerializeField] private int score = 100;

        [Header("Баланс (из таблицы GDD)")]
        [SerializeField] protected EnemyLevel enemyLevel = EnemyLevel.Level_1;
        [SerializeField] protected  bool debugBalanceValues = true;


        [HideInInspector] public float collisionDamage = 1f; 
        
        public float showDamageDuration = 0.1f; 
        public float powerUpDropChance = 1f; 

        [Header("Grid Formation (Новое)")]
        [SerializeField] private bool useGridFormation = false;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _explosionClip; 

        [Header("Visual Effects")]
        [Tooltip("Префаб частиц при уничтожении всего корабля")]
        [SerializeField] private ParticleSystem _explosionParticlePrefab;

        [Header("Настройка рендереров (Вручную)")]
        [Tooltip("Перетащи сюда объекты с MeshRenderer, которые должны краснеть при уроне")]
        [SerializeField] private Renderer[] renderersToFlash; 

        [Header("Set Dynamically: Enemy")]
        [SerializeField] private Material[] materials; 
        [SerializeField] private Color[] originalColors;
        
        [HideInInspector] public bool showingDamage = false;
        [HideInInspector] public float damageDoneTime; 
        
        private bool notifiedOfDestruction = false; 
        private Utils _utils;
        private AudioService _audioService; 
        private PlayerScore _playerScore;
        protected BalanceConfig balanceConfig;
        private bool _isInitialized = false;

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
        private void Construct(Utils utils, AudioService audioService, PlayerScore playerScore, Main main, BalanceConfig balanceConfig)
        {
            _utils = utils;
            _audioService = audioService;
            _playerScore = playerScore;
            this.main = main;
            this.balanceConfig = balanceConfig;
        }

        protected virtual void Awake()
        {
            bndCheck = GetComponent<BoundsCheck>();
            _maxHealthCached = health;
        }

        protected virtual void Start()
        {
            EnsureMaterialsInitialized();
            ApplyBalanceConfig();
        }

        protected virtual void ApplyBalanceConfig()
        {
            if (balanceConfig == null)
            {
                if (debugBalanceValues)
                {
                    Debug.LogWarning($"[{gameObject.name}] BalanceConfig не внедрён через Zenject — используются значения из инспектора: speed={speed}, health={health}, collisionDamage={collisionDamage}");
                }
                return;
            }

            speed = balanceConfig.GetEnemySpeed(enemyLevel);
            health = balanceConfig.GetEnemyHealth(enemyLevel);
            collisionDamage = balanceConfig.GetEnemyCollisionDamage(enemyLevel);
            _maxHealthCached = health;

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({GetType().Name}, уровень {enemyLevel}) применены значения баланса: " +
                          $"Скорость = {speed}, Здоровье = {health}, Урон от столкновения = {collisionDamage}");
            }
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

            if (bndCheck != null)
            {
                bndCheck.enabled = !useGridFormation;
            }
        }

        private void EnsureMaterialsInitialized()
        {
            if (_isInitialized) return;

            if (renderersToFlash == null || renderersToFlash.Length == 0)
            {
                materials = new Material[0];
                originalColors = new Color[0];
                _isInitialized = true;
                return;
            }

            List<Material> extractedMaterials = new List<Material>();
            foreach (Renderer rend in renderersToFlash)
            {
                if (rend != null && rend.materials != null)
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat != null)
                        {
                            extractedMaterials.Add(mat);
                        }
                    }
                }
            }

            materials = extractedMaterials.ToArray();
            originalColors = new Color[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_Color"))
                {
                    originalColors[i] = materials[i].color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }
            }

            _isInitialized = true;
        }

        protected virtual void Update()
        {
            if (notifiedOfDestruction) return;

            if (useGridFormation) HandleGridMovement();
            else Move();

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

        public void SetTargetLocalPosition(Vector3 localTargetPos)
        {
            _targetLocalPosition = new Vector3(localTargetPos.x, localTargetPos.y, 0f);
            _hasGridTarget = true;
            _reachedGridTarget = false;
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (notifiedOfDestruction) return;

            GameObject otherGo = other.gameObject;
            
            if (otherGo.CompareTag("ProjectileHero"))
            {
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
            if (notifiedOfDestruction) return;

            ShowDamage();

            WeaponConfig config = main.GetWeaponConfig(weaponType);

            if (config != null)
            {
                health -= config.damageOnHit;
            }

            if (health <= 0)
            {
                if (!notifiedOfDestruction)
                {
                    notifiedOfDestruction = true;

                    if (_explosionParticlePrefab != null)
                    {
                        LeanPool.Spawn(_explosionParticlePrefab, transform.position, Quaternion.identity);
                    }

                    if (_audioService != null && _explosionClip != null)
                    {
                        _audioService.PlayOneShot(_explosionClip);
                    }

                    _playerScore?.AddScore(score);
            
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
            if (materials == null || notifiedOfDestruction) return;
            foreach (Material m in materials)
            {
                if (m != null && m.HasProperty("_Color")) 
                {
                    m.color = Color.red;
                }
            }
            showingDamage = true;
            damageDoneTime = Time.time + showDamageDuration;
        }

        protected void UnShowDamage()
        {
            if (materials == null || notifiedOfDestruction) return;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && i < originalColors.Length && materials[i].HasProperty("_Color"))
                {
                    materials[i].color = originalColors[i];
                }
            }
            showingDamage = false;
        }
    }
}