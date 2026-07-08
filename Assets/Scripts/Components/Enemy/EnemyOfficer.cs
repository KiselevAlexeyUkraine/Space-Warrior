using UnityEngine;
using System.Collections;
using Lean.Pool;
using Zenject;
using Servise;
using Components.Player;
using Components.Weapon;

namespace Components.Enemy
{
    // Гарантируем, что Unity понимает иерархию и не ломает компонент при потере фокуса
    [SelectionBase] 
    public class EnemyOfficer : EnemyBase
    {
        // ИСПРАВЛЕНО: Упрощены строки заголовков [Header] (убраны знаки "====="), 
        // так как они часто ломают стандартный рендерер инспектора в дочерних классах Unity.
        
        [Header("Shield Reflector Settings")]
        [Tooltip("Объект щита (с Renderer), который висит на враге.")]
        [SerializeField] private GameObject _shieldObject;
        
        [Tooltip("Максимальный уровень щита (сколько снарядов он может отразить).")]
        [SerializeField] private int _maxShieldLevel = 5;
        
        [Tooltip("Скорость вращения щита вокруг своей оси.")]
        [SerializeField] private float _rotationsPerSecond = 0.1f;

        [Tooltip("Префаб вражеского снаряда, который спавнится ПРИ ОТРАЖЕНИИ.")]
        [SerializeField] private GameObject _reflectedProjectilePrefab;

        [Tooltip("Скорость полета отраженного снаряда вниз.")]
        [SerializeField] private float _reflectedProjectileSpeed = 15f;


        [Header("Laser Weapon Settings")]
        [Tooltip("Сам объект лазера (дочерний объект с рендерером и BoxCollider).")]
        [SerializeField] private GameObject _laserObject;
        
        [Tooltip("Цвет лазера в фазе подготовки (когда коллайдер еще выключен).")]
        [SerializeField] private Color _prepareColor = Color.white;
        
        [Tooltip("Цвет лазера в активной фазе (при расширении и жарке).")]
        [SerializeField] private Color _activeColor = Color.blue;

        [Tooltip("Задержка перед самым первым выстрелом лазера после спавна.")]
        [SerializeField] private float _laserInitialDelay = 2f;

        [Tooltip("Время ожидания ПОСЛЕ включения объекта, но ДО начала расширения.")]
        [SerializeField, Min(0f)] private float _laserPrepareDuration = 0.6f;

        [Tooltip("Время, за которое лазер вырастает от 0 до максимального масштаба.")]
        [SerializeField] private float _laserGrowDuration = 0.3f;

        [Tooltip("Продолжительность удержания лазера в максимальном масштабе.")]
        [SerializeField] private float _laserActiveDuration = 1.5f;

        [Tooltip("Время, за которое лазер сжимается обратно до 0.")]
        [SerializeField] private float _laserShrinkDuration = 0.2f;

        [Tooltip("Максимальный масштаб лазера по оси X.")]
        [SerializeField] private float _laserTargetScaleX = 2.5f;


        [Header("Rocket Launcher Settings")]
        [Tooltip("Префаб ракеты (с EnemyFollowProjectile или без).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета ракет.")]
        [SerializeField] private Transform _rocketFirePoint;

        [Tooltip("Если включено, ракета летит строго вниз. Если выключено — целится в игрока.")]
        [SerializeField] private bool _rocketShootStraightDown = false;

        [SerializeField, Min(0.1f)] private float rocketMinFireInterval = 3f;
        [SerializeField, Min(0.1f)] private float rocketMaxFireInterval = 5f;
        [SerializeField] private float _rocketInitialShotDelay = 1f;

        [Tooltip("Скорость полета ракеты (если нет скрипта авто-движения).")]
        [SerializeField] private float _rocketSpeed = 10f;


        // Приватные переменные состояния щита
        private float _currentShieldLevel;
        private Material _shieldMat;
        private int _lastShownLevel = -1;

        // Приватные переменные состояния лазера
        private BoxCollider _laserCollider;
        private Renderer _laserRenderer;
        private Coroutine _laserRoutine;

        // Приватные переменные состояния ракет
        private float _nextRocketFireTime;

        // Зависимости (Zenject)
        private DiContainer _diContainer;
        private AnimatorService _animatorService;
        private PlayerMove _player;

        [Inject]
        private void ConstructOfficer(DiContainer diContainer, AnimatorService animatorService, PlayerMove player = null)
        {
            _diContainer = diContainer;
            _animatorService = animatorService;
            _player = player;
        }

        protected override void Awake()
        {
            base.Awake(); // Строго вызываем Awake базового класса EnemyBase
            
            if (_shieldObject != null)
            {
                Renderer shieldRend = _shieldObject.GetComponent<Renderer>();
                if (shieldRend != null)
                {
                    _shieldMat = shieldRend.material;
                }
            }

            if (_laserObject != null)
            {
                _laserCollider = _laserObject.GetComponent<BoxCollider>();
                _laserRenderer = _laserObject.GetComponent<Renderer>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable(); // Строго восстанавливаем здоровье и логику из EnemyBase
            
            _currentShieldLevel = _maxShieldLevel;
            _lastShownLevel = -1;
            if (_shieldObject != null) _shieldObject.SetActive(true);
            UpdateShieldVisual(forceUpdate: true);

            if (_laserObject != null) _laserObject.SetActive(false);
            if (_laserCollider != null) _laserCollider.enabled = false;
            
            // Защита от дублирования корутин
            if (_laserRoutine != null) StopCoroutine(_laserRoutine);
            _laserRoutine = StartCoroutine(LaserAttackRoutine());

            _nextRocketFireTime = Time.time + _rocketInitialShotDelay;
        }

        private void OnDisable()
        {
            if (_laserRoutine != null)
            {
                StopCoroutine(_laserRoutine);
                _laserRoutine = null;
            }
        }

        protected override void Update()
        {
            base.Update(); // Даем EnemyBase обработать мигание от урона и Grid-движение

            if (_shieldObject != null && _shieldObject.activeSelf)
            {
                float rZ = -(_rotationsPerSecond * Time.time * 360f) % 360f;
                _shieldObject.transform.rotation = Quaternion.Euler(0, 0, rZ);
            }

            if (Time.time >= _nextRocketFireTime)
            {
                ShootRocket();
                CalculateNextRocketFireTime();
            }
        }

        public override void Move()
        {
            // Эта логика выполнится, только если useGridFormation == false в базовом классе
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime; 
            pos = tempPos;
        }

        // ==========================================
        // РЕФЛЕКТОР
        // ==========================================
        protected override void OnTriggerEnter(Collider other)
        {
            if (_shieldObject != null && _shieldObject.activeSelf && other.CompareTag("ProjectileHero"))
            {
                SpawnReflectedProjectile(other.transform.position);

                _currentShieldLevel -= 1f;


                if (_currentShieldLevel <= 0)
                {
                    _currentShieldLevel = 0;
                    _shieldObject.SetActive(false); 
                }

                UpdateShieldVisual();
                SafeDespawn(other.gameObject);
                return; 
            }

            base.OnTriggerEnter(other);
        }

        private void SpawnReflectedProjectile(Vector3 spawnPosition)
        {
            if (_reflectedProjectilePrefab == null) return;

            GameObject projectileGo = LeanPool.Spawn(_reflectedProjectilePrefab, spawnPosition, Quaternion.Euler(0, 0, 180f));
            _diContainer?.InjectGameObject(projectileGo);

            if (_animatorService != null)
            {
                _animatorService.RegisterProjectile(projectileGo, _reflectedProjectileSpeed);
            }
        }

        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (_shieldMat == null) return;

            int currentVisualLevel = Mathf.FloorToInt(_currentShieldLevel);

            if (_lastShownLevel != currentVisualLevel || forceUpdate)
            {
                _lastShownLevel = currentVisualLevel;
                _shieldMat.mainTextureOffset = new Vector2(0.2f * _lastShownLevel, 0);
            }
        }

        // ==========================================
        // РАКЕТЫ
        // ==========================================
        private void CalculateNextRocketFireTime()
        {
            float min = Mathf.Min(rocketMinFireInterval, rocketMaxFireInterval);
            float max = Mathf.Max(rocketMinFireInterval, rocketMaxFireInterval);
            _nextRocketFireTime = Time.time + Random.Range(min, max);
        }

        private void ShootRocket()
        {
            if (_rocketPrefab == null) return;
            if (!_rocketShootStraightDown && _player == null) return;

            Transform spawnPoint = _rocketFirePoint != null ? _rocketFirePoint : transform;
            Vector3 directionToPlayer;
            Quaternion rocketRotation;

            if (_rocketShootStraightDown)
            {
                directionToPlayer = Vector3.down;
                rocketRotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                directionToPlayer = _player.transform.position - spawnPoint.position;
                directionToPlayer.z = 0f;

                rocketRotation = spawnPoint.rotation;
                if (directionToPlayer != Vector3.zero)
                {
                    float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    rocketRotation = Quaternion.Euler(0, 0, angle - 90f); 
                }
            }

            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            _diContainer?.InjectGameObject(rocketGo);

            if (rocketGo.TryGetComponent<EnemyFollowProjectile>(out var followProjectile))
            {
                return;
            }

            if (_animatorService != null)
            {
                if (_rocketShootStraightDown)
                {
                    _animatorService.RegisterProjectile(rocketGo, _rocketSpeed);
                }
                else
                {
                    _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer);
                }
            }
        }

        // ==========================================
        // ЛАЗЕР
        // ==========================================
        private IEnumerator LaserAttackRoutine()
        {
            yield return new WaitForSeconds(_laserInitialDelay);

            while (true)
            {
                if (_laserObject != null)
                {
                    SetLaserXScale(0.05f);
                    if (_laserRenderer != null && _laserRenderer.material.HasProperty("_Color"))
                    {
                        _laserRenderer.material.color = _prepareColor;
                    }
                    _laserObject.SetActive(true);
                }

                if (_laserCollider != null) _laserCollider.enabled = false;
                yield return new WaitForSeconds(_laserPrepareDuration);

                if (_laserRenderer != null && _laserRenderer.material.HasProperty("_Color"))
                {
                    _laserRenderer.material.color = _activeColor;
                }

                if (_laserCollider != null) _laserCollider.enabled = true;

                float elapsed = 0f;
                while (elapsed < _laserGrowDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _laserGrowDuration;
                    SetLaserXScale(Mathf.Lerp(0.05f, _laserTargetScaleX, progress));
                    yield return null;
                }
                SetLaserXScale(_laserTargetScaleX);

                yield return new WaitForSeconds(_laserActiveDuration);

                elapsed = 0f;
                while (elapsed < _laserShrinkDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / _laserShrinkDuration;
                    SetLaserXScale(Mathf.Lerp(_laserTargetScaleX, 0f, progress));
                    yield return null;
                }
                SetLaserXScale(0f);

                if (_laserCollider != null) _laserCollider.enabled = false;
                if (_laserObject != null) _laserObject.SetActive(false);

                yield return new WaitForSeconds(Random.Range(4f, 7f));
            }
        }

        private void SetLaserXScale(float scaleX)
        {
            if (_laserObject == null) return;
            Vector3 currentScale = _laserObject.transform.localScale;
            currentScale.x = scaleX;
            _laserObject.transform.localScale = currentScale;
        }
    }
}