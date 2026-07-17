using UnityEngine;
using Components.Weapon;
using Components.Player.Helpers;
using Components.Player; 
using Zenject;
using Lean.Pool;
using Level;
using Servise;
using System.Collections.Generic;

namespace Components.Enemy
{
    public enum MovementPattern
    {
        [InspectorName("Прямо вниз")] StraightDown,
        [InspectorName("Синусоида (влево-вправо + вниз)")] SineWave,
        [InspectorName("Диагональный пролёт (вход/выход по краям экрана)")] DiagonalSweep,
        [InspectorName("Дугой вниз (заход сбоку, разворот)")] SwoopCurve,
        [InspectorName("Сбоку до края (появляется слева/справа, летит горизонтально)")] HorizontalEdge
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
        [SerializeField] protected bool debugBalanceValues = true;

        [HideInInspector] public float collisionDamage = 1f; 
        public float showDamageDuration = 0.1f; 
        public float powerUpDropChance = 1f; 

        [Header("Grid Formation (Новое)")]
        [SerializeField] [Tooltip("Использовать ли логику сетки для этого врага.")] private bool useGridFormation = false;

        [Header("Паттерн движения")]
        [Tooltip("Траектория движения врага. Действует ТОЛЬКО если 'Use Grid Formation' выключен — в сетке позиция управляется GridFormationSpawner.")]
        [SerializeField] protected MovementPattern movementPattern = MovementPattern.StraightDown;

        [Header("Паттерн: Sine Wave (синусоида)")]
        [Tooltip("Период колебания по X, секунд.")] [SerializeField] protected float waveFrequency = 2f;
        [Tooltip("Амплитуда колебания по X.")] [SerializeField] protected float waveWidth = 4f;
        [Tooltip("Максимальный угол поворота корабля по Y при колебании.")] [SerializeField] protected float waveRotY = 45f;

        [Header("Паттерн: Diagonal Sweep (диагональный пролёт)")]
        [Tooltip("Время полного пролёта от одного края экрана до другого, секунд.")] [SerializeField] protected float diagonalLifeTime = 10f;
        [Tooltip("Степень 'выгибания' траектории синусоидой (0 = прямая линия).")] [SerializeField] protected float diagonalEccentricity = 0.6f;

        [Header("Паттерн: Swoop Curve (дуга вниз)")]
        [Tooltip("Время прохождения всей дуги, секунд.")] [SerializeField] protected float swoopLifeTime = 5f;

        [Header("Паттерн: Horizontal Edge (сбоку до края)")]
        [Tooltip("Если включено — сторона появления (слева/справа) выбирается случайно каждый раз. Если выключено — используется 'Fixed Start Side'.")] [SerializeField] protected bool horizontalRandomSide = true;
        [Tooltip("Фиксированная сторона появления, если 'Horizontal Random Side' выключен.")] [SerializeField] protected bool horizontalStartFromLeft = true;

        [Header("Настройки покачивания (Bobbing Effect)")]
        [SerializeField] [Tooltip("Включить ли комплексное покачивание (Зависание/Bounce эффекты) для визуальной модели корабля.")] private bool enableBobbing = true;
        [SerializeField] [Tooltip("Трансформ-объект модели врага (визуальная часть), которую будет покачивать. ВНИМАНИЕ: Не кидайте сюда корневой объект! Создайте дочерний контейнер.")] private Transform visualBobTarget;
        [SerializeField] [Tooltip("Амплитуда смещения вниз по локальной оси Y.")] private float bobPositionOffset = 0.3f;
        [SerializeField] [Tooltip("Амплитуда покачивания вращения по осям Y и Z (в градусах).")] private float bobRotationOffset = 15f;
        [SerializeField] [Tooltip("Время одного цикла покачивания (туда-обратно), в секундах.")] private float bobDuration = 1.5f;
        [SerializeField] [Tooltip("Тип сглаживания PrimeTween анимации покачивания.")] private PrimeTween.Ease bobEase = PrimeTween.Ease.InOutSine;
        

        [Header("Visual Effects")]
        [Tooltip("Префаб частиц при уничтожении всего корабля")] [SerializeField] private ParticleSystem _explosionParticlePrefab;

        [Header("Новая механика отстрела деталей")]
        [Tooltip("Объекты на враге, которые будут СЛУЧАЙНО выключаться при попадании.")]
        [SerializeField] private GameObject[] objectsToDisable;
        [Tooltip("Объект (например, обломок или взрыв), который временно спавнитcя на месте отстреленной детали.")]
        [SerializeField] protected GameObject hitEffectPrefab;
        [Tooltip("Время жизни спавнящегося временного объекта (в секундах).")]
        [SerializeField] protected float effectDestroyDelay = 2f;

        // Список для отслеживания еще оставшихся активных деталей
        protected List<GameObject> _activeObjectsList = new List<GameObject>();

        [HideInInspector] public bool showingDamage = false;
        [HideInInspector] public float damageDoneTime; 
        
        private bool notifiedOfDestruction = false; 
        protected AudioService _audioService; 
        private PlayerScore _playerScore;
        protected AnimatorService _animatorService; 
        protected BalanceConfig balanceConfig;

        private Vector3 _targetLocalPosition;
        private bool _hasGridTarget = false;
        private bool _reachedGridTarget = false;
        private float _maxHealthCached;

        private float _patternX0;
        private float _patternBirthTime;
        private Vector3 _patternP0;
        private Vector3 _patternP1;
        private Vector3[] _patternSwoopPoints;
        private int _patternHorizontalDirection; 

        private Vector3 _initialVisualLocalPos;
        private Quaternion _initialVisualLocalRot;
        private bool _isAwaken = false;

        protected BoundsCheck bndCheck;
        protected Main main;

        public Vector3 pos
        {
            get => transform.position;
            set => transform.position = value;
        }
        
        [Inject]
        private void Construct(AudioService audioService, PlayerScore playerScore, Main main, BalanceConfig balanceConfig, AnimatorService animatorService)
        {
            _audioService = audioService;
            _playerScore = playerScore;
            this.main = main;
            this.balanceConfig = balanceConfig;
            _animatorService = animatorService; 
        }

        protected virtual void Awake()
        {
            bndCheck = GetComponent<BoundsCheck>();
            _maxHealthCached = health;

            if (visualBobTarget != null)
            {
                _initialVisualLocalPos = visualBobTarget.localPosition;
                _initialVisualLocalRot = visualBobTarget.localRotation;
            }

            _isAwaken = true;
        }

        protected virtual void Start()
        {
            InitializeActiveObjectsList();
            ApplyBalanceConfig();
            TriggerBobbingAnimation();
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
            TriggerBobbingAnimation();
        }

        protected virtual void OnEnable()
        {
            notifiedOfDestruction = false;
            showingDamage = false;
            health = _maxHealthCached;
            _hasGridTarget = false;
            _reachedGridTarget = false;

            InitializeActiveObjectsList();
            ApplyBalanceConfig();

            if (bndCheck != null)
            {
                bndCheck.enabled = !useGridFormation;
            }

            if (!useGridFormation)
            {
                InitMovementPattern();
            }

            if (_isAwaken)
            {
                TriggerBobbingAnimation();
            }
        }

        protected virtual void OnDisable()
        {
            if (_animatorService != null && visualBobTarget != null)
            {
                _animatorService.StopComplexBob(visualBobTarget);
                visualBobTarget.localPosition = _initialVisualLocalPos;
                visualBobTarget.localRotation = _initialVisualLocalRot;
            }
        }

        private void TriggerBobbingAnimation()
        {
            if (enableBobbing && visualBobTarget != null && _animatorService != null)
            {
                _animatorService.StartComplexBob(visualBobTarget, bobPositionOffset, bobRotationOffset, bobDuration, bobEase);
            }
        }

        private void InitializeActiveObjectsList()
        {
            _activeObjectsList.Clear();
            if (objectsToDisable != null)
            {
                foreach (var obj in objectsToDisable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        _activeObjectsList.Add(obj);
                    }
                }
            }
        }

        protected virtual void Update()
        {
            if (notifiedOfDestruction) return;

            if (useGridFormation) HandleGridMovement();
            else Move();
            
            if (bndCheck != null && bndCheck.enabled && bndCheck.offDown)
            {
                SafeDespawn(gameObject);
            }
        }

        private void InitMovementPattern()
        {
            _patternBirthTime = Time.time;

            switch (movementPattern)
            {
                case MovementPattern.StraightDown:
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

            float padding = bndCheck.radius > 0 ? bndCheck.radius : 1f;
            tempPos.y = Random.Range(0f, bndCheck.camHeight - padding);
            
            pos = tempPos;

            _patternHorizontalDirection = startFromLeft ? 1 : -1;
        }

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

            if (!enableBobbing)
            {
                Vector3 rot = new Vector3(0, sin * waveRotY, 0);
                transform.rotation = Quaternion.Euler(rot);
            }
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

            WeaponConfig config = main.GetWeaponConfig(weaponType);
            float damage = config != null ? config.damageOnHit : 1f;

            if (health > 1)
            {
                HandlePartDetachment();
            }

            health -= damage;

            if (health <= 0)
            {
                if (!notifiedOfDestruction)
                {
                    notifiedOfDestruction = true;

                    if (_explosionParticlePrefab != null)
                    {
                        LeanPool.Spawn(_explosionParticlePrefab, transform.position, Quaternion.identity);
                    }
                    
                    _audioService?.PlayRandomEnemyDeath();

                    _playerScore?.AddScore(score);
            
                    main.ShipDestroyed(this);
                }
            }
        }

        protected virtual void HandlePartDetachment()
        {
            if (_activeObjectsList.Count > 0)
            {
                // Выбираем РАНДОМНЫЙ индекс из оставшихся элементов списка
                int randomIndex = Random.Range(0, _activeObjectsList.Count);
                GameObject targetObj = _activeObjectsList[randomIndex];

                if (targetObj != null)
                {
                    Vector3 partPosition = targetObj.transform.position;
                    Quaternion partRotation = targetObj.transform.rotation;

                    // Выключаем случайный объект
                    targetObj.SetActive(false);

                    // Создаем временный эффект на его месте
                    if (hitEffectPrefab != null)
                    {
                        GameObject spawnedEffect = LeanPool.Spawn(hitEffectPrefab, partPosition, partRotation);
                        StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, effectDestroyDelay));
                    }

                    // Звук попадания
                    _audioService?.PlayRandomEnemyHit();
                }
        
                // Удаляем именно этот обработанный объект, чтобы он больше не выбирался рандомом
                _activeObjectsList.RemoveAt(randomIndex);
            }
        }

        protected System.Collections.IEnumerator DespawnEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            SafeDespawn(effect);
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
    }
}