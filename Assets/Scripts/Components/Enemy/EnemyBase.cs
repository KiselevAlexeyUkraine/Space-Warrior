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
    public enum MovementPattern
    {
        [InspectorName("Прямо вниз")]
        StraightDown,

        [InspectorName("Синусоида (влево-вправо + вниз)")]
        SineWave,

        [InspectorName("Диагональный пролёт (вход/выход по краям экрана)")]
        DiagonalSweep,

        [InspectorName("Дугой вниз (заход сбоку, разворот)")]
        SwoopCurve,

        [InspectorName("Сбоку до края (появляется слева/справа, летит горизонтально)")]
        HorizontalEdge
    }

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

        [Header("Паттерн движения")]
        [Tooltip("Траектория движения врага. Действует ТОЛЬКО если 'Use Grid Formation' выключен — в сетке позиция управляется GridFormationSpawner.")]
        [SerializeField] protected MovementPattern movementPattern = MovementPattern.StraightDown;

        [Header("Паттерн: Sine Wave (синусоида)")]
        [Tooltip("Период колебания по X, секунд.")]
        [SerializeField] protected float waveFrequency = 2f;
        [Tooltip("Амплитуда колебания по X.")]
        [SerializeField] protected float waveWidth = 4f;
        [Tooltip("Максимальный угол поворота корабля по Y при колебании.")]
        [SerializeField] protected float waveRotY = 45f;

        [Header("Паттерн: Diagonal Sweep (диагональный пролёт)")]
        [Tooltip("Время полного пролёта от одного края экрана до другого, секунд.")]
        [SerializeField] protected float diagonalLifeTime = 10f;
        [Tooltip("Степень 'выгибания' траектории синусоидой (0 = прямая линия).")]
        [SerializeField] protected float diagonalEccentricity = 0.6f;

        [Header("Паттерн: Swoop Curve (дуга вниз)")]
        [Tooltip("Время прохождения всей дуги, секунд.")]
        [SerializeField] protected float swoopLifeTime = 5f;

        [Header("Паттерн: Horizontal Edge (сбоку до края)")]
        [Tooltip("Если включено — сторона появления (слева/справа) выбирается случайно каждый раз. Если выключено — используется 'Fixed Start Side'.")]
        [SerializeField] protected bool horizontalRandomSide = true;
        [Tooltip("Фиксированная сторона появления, если 'Horizontal Random Side' выключен.")]
        [SerializeField] protected bool horizontalStartFromLeft = true;

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

        // --- Состояние паттернов движения ---
        private float _patternX0;
        private float _patternBirthTime;
        private Vector3 _patternP0;
        private Vector3 _patternP1;
        private Vector3[] _patternSwoopPoints;
        private int _patternHorizontalDirection; // 1 = слева направо, -1 = справа налево

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

            if (!useGridFormation)
            {
                InitMovementPattern();
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

        // --- Инициализация состояния под выбранный паттерн (вызывается при каждом OnEnable, т.к. объекты из пула) ---
        private void InitMovementPattern()
        {
            _patternBirthTime = Time.time;

            switch (movementPattern)
            {
                case MovementPattern.StraightDown:
                    // Параметры не требуются.
                    break;

                case MovementPattern.SineWave:
                    _patternX0 = pos.x;
                    break;

                case MovementPattern.DiagonalSweep:
                    InitDiagonalSweep();
                    break;

                case MovementPattern.SwoopCurve:
                    InitSwoopCurve();
                    break;

                case MovementPattern.HorizontalEdge:
                    InitHorizontalEdge();
                    break;
            }
        }

        private void InitDiagonalSweep()
        {
            _patternP0 = Vector3.zero;
            _patternP0.x = -bndCheck.camWidth - bndCheck.radius;
            _patternP0.y = Random.Range(-bndCheck.camHeight, bndCheck.camHeight);

            _patternP1 = Vector3.zero;
            _patternP1.x = bndCheck.camWidth + bndCheck.radius;
            _patternP1.y = Random.Range(-bndCheck.camHeight, bndCheck.camHeight);

            if (Random.value > 0.5f)
            {
                _patternP0.x *= -1;
                _patternP1.x *= -1;
            }

            pos = _patternP0;
        }

        private void InitSwoopCurve()
        {
            _patternSwoopPoints = new Vector3[3];
            _patternSwoopPoints[0] = pos;

            float xMin = -bndCheck.camWidth + bndCheck.radius;
            float xMax = bndCheck.camWidth - bndCheck.radius;

            Vector3 v = Vector3.zero;
            v.x = Random.Range(xMin, xMax);
            v.y = -bndCheck.camHeight * Random.Range(2f, 2.75f);
            _patternSwoopPoints[1] = v;

            v = Vector3.zero;
            v.y = pos.y;
            v.x = Random.Range(xMin, xMax);
            _patternSwoopPoints[2] = v;
        }

        private void InitHorizontalEdge()
        {
            bool startFromLeft = horizontalRandomSide ? Random.value > 0.5f : horizontalStartFromLeft;

            float edgeX = bndCheck.camWidth + bndCheck.radius;

            Vector3 tempPos = pos;
            tempPos.x = startFromLeft ? -edgeX : edgeX;
            pos = tempPos;

            _patternHorizontalDirection = startFromLeft ? 1 : -1;
        }

        // --- Движение по выбранному паттерну. Не абстрактный - можно переопределить в наследнике (см. Enemy_4). ---
        public virtual void Move()
        {
            switch (movementPattern)
            {
                case MovementPattern.StraightDown:
                    MoveStraightDown();
                    break;

                case MovementPattern.SineWave:
                    MoveSineWave();
                    break;

                case MovementPattern.DiagonalSweep:
                    MoveDiagonalSweep();
                    break;

                case MovementPattern.SwoopCurve:
                    MoveSwoopCurve();
                    break;

                case MovementPattern.HorizontalEdge:
                    MoveHorizontalEdge();
                    break;
            }
        }

        private void MoveStraightDown()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        private void MoveSineWave()
        {
            Vector3 tempPos = pos;
            float age = Time.time - _patternBirthTime;
            float theta = Mathf.PI * 2 * age / waveFrequency;
            float sin = Mathf.Sin(theta);

            tempPos.x = _patternX0 + waveWidth * sin;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;

            Vector3 rot = new Vector3(0, sin * waveRotY, 0);
            transform.rotation = Quaternion.Euler(rot);
        }

        private void MoveDiagonalSweep()
        {
            float u = (Time.time - _patternBirthTime) / diagonalLifeTime;

            if (u > 1)
            {
                SafeDespawn(gameObject);
                return;
            }

            u = u + diagonalEccentricity * Mathf.Sin(u * Mathf.PI * 2);
            pos = (1 - u) * _patternP0 + u * _patternP1;
        }

        private void MoveSwoopCurve()
        {
            float u = (Time.time - _patternBirthTime) / swoopLifeTime;

            if (u > 1)
            {
                SafeDespawn(gameObject);
                return;
            }

            u = u - 0.2f * Mathf.Sin(u * Mathf.PI * 2);

            Vector3 p01 = (1 - u) * _patternSwoopPoints[0] + u * _patternSwoopPoints[1];
            Vector3 p12 = (1 - u) * _patternSwoopPoints[1] + u * _patternSwoopPoints[2];
            pos = (1 - u) * p01 + u * p12;
        }

        private void MoveHorizontalEdge()
        {
            Vector3 tempPos = pos;
            tempPos.x += _patternHorizontalDirection * speed * Time.deltaTime;
            pos = tempPos;

            float edgeX = bndCheck.camWidth + bndCheck.radius;

            // Долетел до противоположного края - и всё, исчезает.
            if (_patternHorizontalDirection > 0 && tempPos.x >= edgeX)
            {
                SafeDespawn(gameObject);
            }
            else if (_patternHorizontalDirection < 0 && tempPos.x <= -edgeX)
            {
                SafeDespawn(gameObject);
            }
        }

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