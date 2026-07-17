using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using Zenject;
using Servise;
using Components.Player;
using Components.Weapon;

namespace Components.Enemy
{
    [SelectionBase]
    public class EnemyOfficer : EnemyBase
    {
        // ==========================================
        // ЩИТ (Reflector + Regen)
        // ==========================================

        public enum WeaponShooterType
        {
            StraightDownShooter,
            TargetPlayerShooter,
            RocketLauncher
        }

        [Header("Shield Objects")]
        [Tooltip("Объект щита (с Renderer), который висит на враге и вращается, пока щит активен.")]
        [SerializeField] private GameObject _shieldObject;

        [Tooltip("Партикл, спавнящийся при каждом отражении снаряда, пока щит не сбит до 0.")]
        [SerializeField] private GameObject _shieldHitEffectPrefab;
        [Tooltip("Время жизни партикла отражения (в секундах).")]
        [SerializeField] private float _shieldHitEffectDestroyDelay = 2f;

        [Tooltip("Партикл, спавнящийся один раз в момент полного уничтожения щита (щит доходит до 0).")]
        [SerializeField] private GameObject _shieldBreakEffectPrefab;
        [Tooltip("Время жизни партикла уничтожения щита (в секундах).")]
        [SerializeField] private float _shieldBreakEffectDestroyDelay = 2f;

        [Header("Shield Settings")]
        [Tooltip("Максимальный уровень щита (сколько снарядов он может отразить). Перезаписывается из BalanceConfig.")]
        [SerializeField] private int _maxShieldLevel = 5;

        [Tooltip("Через сколько секунд восстанавливается 1 единица щита. Перезаписывается из BalanceConfig.")]
        [SerializeField] private float _shieldRegenIntervalSeconds = 15f;

        [Tooltip("Скорость вращения щита вокруг своей оси (оборотов в секунду), пока щит активен.")]
        [SerializeField] private float _shieldRotationsPerSecond = 0.1f;

        [Header("Reflection (Shooting) Settings")]
        [Tooltip("Префаб вражеского снаряда, который спавнится ПРИ ОТРАЖЕНИИ.")]
        [SerializeField] private GameObject _reflectedProjectilePrefab;

        [Tooltip("Скорость полета отраженного снаряда вниз. Перезаписывается из BalanceConfig.")]
        [SerializeField] private float _reflectedProjectileSpeed = 15f;

        private int _currentShieldLevel;
        private Material _shieldMat;
        private int _lastShownShieldLevel = -1;
        private float _nextShieldRegenTime;
        private bool _isShieldRegenScheduled;


        // ==========================================
        // ЛАЗЕР
        // ==========================================

        [Header("Laser Objects")]
        [Tooltip("Объект предупреждения перед выстрелом лазера (включается на время подготовки, коллайдера нет).")]
        [SerializeField] private GameObject _laserPrepareObject;

        [Tooltip("Объект самого лазера (содержит BoxCollider, растягивается по X во время выстрела).")]
        [SerializeField] private GameObject _laserActiveObject;

        [Header("Laser Timing")]
        [Tooltip("Минимальная задержка перед самым первым выстрелом лазера после спавна.")]
        [SerializeField, Min(0f)] private float _laserMinInitialDelay = 2f;
        [Tooltip("Максимальная задержка перед самым первым выстрелом лазера после спавна.")]
        [SerializeField, Min(0f)] private float _laserMaxInitialDelay = 2f;

        [Tooltip("Минимальное время отдыха между выстрелами лазера.")]
        [SerializeField, Min(0.1f)] private float _laserMinFireInterval = 4f;
        [Tooltip("Максимальное время отдыха между выстрелами лазера.")]
        [SerializeField, Min(0.1f)] private float _laserMaxFireInterval = 7f;

        [Tooltip("Время ожидания ПОСЛЕ включения объекта подготовки, ДО начала выстрела (коллайдер выключен).")]
        [SerializeField, Min(0f)] private float _laserPrepareDuration = 0.6f;

        [Tooltip("Время, за которое лазер вырастает от 0 до максимального масштаба.")]
        [SerializeField] private float _laserGrowDuration = 0.3f;

        [Tooltip("Продолжительность удержания лазера в максимальном масштабе.")]
        [SerializeField] private float _laserActiveDuration = 1.5f;

        [Tooltip("Время, за которое лазер сжимается обратно до 0.")]
        [SerializeField] private float _laserShrinkDuration = 0.2f;

        [Tooltip("Максимальный масштаб лазера по оси X.")]
        [SerializeField] private float _laserTargetScaleX = 2.5f;

        private BoxCollider _laserCollider;
        private Coroutine _laserRoutine;


        // ==========================================
        // РАКЕТНИЦА
        // ==========================================

        [Header("Rocket: Универсальный тип стрелка")]
        [SerializeField] private WeaponShooterType _shooterType = WeaponShooterType.RocketLauncher;

        [Header("Rocket Weapon Settings")]
        [Tooltip("Префаб снаряда (для ракетчика — префаб со скриптом EnemyFollowProjectile).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета снаряда.")]
        [SerializeField] private Transform _rocketFirePoint;

        [Header("Rocket Rotation Settings (для тех, кто целится в игрока)")]
        [Tooltip("Объект, который будет вращаться в сторону игрока (например, пушка/голова турели).")]
        [SerializeField] private Transform _rotatingPart;
        [Tooltip("Скорость поворота пушки к игроку.")]
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Rocket Interval Settings")]
        [SerializeField] private float _rocketInitialShotDelay = 1f;
        [Tooltip("Минимальный интервал между выстрелами. Перезаписывается из BalanceConfig.")]
        [SerializeField] private float _rocketMinFireInterval = 2f;
        [Tooltip("Максимальный интервал между выстрелами. Перезаписывается из BalanceConfig.")]
        [SerializeField] private float _rocketMaxFireInterval = 4f;

        private float _rocketSpeed = 12f;
        private bool _rocketShootStraightDown;
        private float _nextRocketFireTime;


        // ==========================================
        // ЗАВИСИМОСТИ (Zenject)
        // ==========================================

        private DiContainer _diContainer;
        private PlayerMove _player;

        [Inject]
        private void ConstructOfficer(DiContainer diContainer, PlayerMove player = null)
        {
            _diContainer = diContainer;
            _player = player;
        }

        protected override void Awake()
        {
            base.Awake();

            if (_shieldObject != null)
            {
                Renderer shieldRend = _shieldObject.GetComponent<Renderer>();
                if (shieldRend != null)
                {
                    _shieldMat = shieldRend.material;
                }
            }

            if (_laserActiveObject != null)
            {
                _laserCollider = _laserActiveObject.GetComponent<BoxCollider>();
            }
        }

        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null) return;

            // --- Щит ---
            _maxShieldLevel = Mathf.RoundToInt(balanceConfig.GetEnemyReflectionCount(enemyLevel));
            _shieldRegenIntervalSeconds = balanceConfig.GetEnemyShieldRegenInterval(enemyLevel);
            _reflectedProjectileSpeed = balanceConfig.GetEnemyReflectedProjectileSpeed(enemyLevel);

            // --- Ракетница ---
            float baseInterval = 3f;

            switch (_shooterType)
            {
                case WeaponShooterType.StraightDownShooter:
                    _rocketShootStraightDown = true;
                    baseInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.TargetPlayerShooter:
                    _rocketShootStraightDown = false;
                    baseInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.RocketLauncher:
                    _rocketShootStraightDown = false;
                    break;
            }

            _rocketMinFireInterval = baseInterval * 0.75f;
            _rocketMaxFireInterval = baseInterval * 1.25f;

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({GetType().Name}, уровень {enemyLevel}) баланс: " +
                          $"Щит={_maxShieldLevel}, RegenInterval={_shieldRegenIntervalSeconds}с, " +
                          $"ReflectedSpeed={_reflectedProjectileSpeed}, RocketInterval={_rocketMinFireInterval:F1}-{_rocketMaxFireInterval:F1}с, RocketSpeed={_rocketSpeed}");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Пересобираем группы деталей (пары по 2) для последовательного отстрела
            BuildPartGroups();

            // --- Щит ---
            _currentShieldLevel = _maxShieldLevel;
            _lastShownShieldLevel = -1;
            _isShieldRegenScheduled = false;

            if (_shieldObject != null) _shieldObject.SetActive(_currentShieldLevel > 0);
            UpdateShieldVisual(forceUpdate: true);
            ScheduleShieldRegenIfNeeded();

            // --- Лазер ---
            if (_laserActiveObject != null) _laserActiveObject.SetActive(false);
            if (_laserCollider != null) _laserCollider.enabled = false;
            if (_laserPrepareObject != null) _laserPrepareObject.SetActive(false);

            if (_laserRoutine != null) StopCoroutine(_laserRoutine);
            _laserRoutine = StartCoroutine(LaserAttackRoutine());

            // --- Ракетница ---
            _nextRocketFireTime = Time.time + _rocketInitialShotDelay + Random.Range(0f, 0.5f);
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
            base.Update();

            // --- Вращение щита ---
            if (_shieldObject != null && _shieldObject.activeSelf)
            {
                float rZ = -(_shieldRotationsPerSecond * Time.time * 360f) % 360f;
                _shieldObject.transform.rotation = Quaternion.Euler(0, 0, rZ);
            }

            HandleShieldRegeneration();

            // --- Поворот турели ракетницы к игроку ---
            if (!_rocketShootStraightDown && _player != null && _rotatingPart != null)
            {
                RotateTowardsPlayer();
            }

            // --- Стрельба ракетницей ---
            if (Time.time >= _nextRocketFireTime)
            {
                ShootRocket();
                _nextRocketFireTime = Time.time + Random.Range(_rocketMinFireInterval, _rocketMaxFireInterval);
            }
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }


        // ==========================================
        // ЩИТ: ОТРАЖЕНИЕ + РЕГЕНЕРАЦИЯ
        // ==========================================

        protected override void OnTriggerEnter(Collider other)
        {
            if (_shieldObject != null && _shieldObject.activeSelf && other.CompareTag("ProjectileHero"))
            {
                SpawnReflectedProjectile(other.transform.position);

                int previousShieldLevel = _currentShieldLevel;
                _currentShieldLevel -= 1;

                if (_currentShieldLevel <= 0)
                {
                    _currentShieldLevel = 0;
                    _shieldObject.SetActive(false);

                    if (_shieldBreakEffectPrefab != null)
                    {
                        GameObject spawnedEffect = LeanPool.Spawn(_shieldBreakEffectPrefab, transform.position, Quaternion.identity);
                        StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, _shieldBreakEffectDestroyDelay));
                    }

                    _audioService?.PlayRandomShieldBreak();
                }
                else
                {
                    if (_shieldHitEffectPrefab != null)
                    {
                        GameObject spawnedEffect = LeanPool.Spawn(_shieldHitEffectPrefab, other.transform.position, Quaternion.identity);
                        StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, _shieldHitEffectDestroyDelay));
                    }

                    _audioService?.PlayRandomShieldHit();
                }

                UpdateShieldVisual(forceUpdate: true);

                // Перезапускаем таймер регенерации от момента удара
                _nextShieldRegenTime = Time.time + _shieldRegenIntervalSeconds;
                _isShieldRegenScheduled = true;

                SafeDespawn(other.gameObject);
                return;
            }

            base.OnTriggerEnter(other);
        }

        private void SpawnReflectedProjectile(Vector3 spawnPosition)
        {
            if (_reflectedProjectilePrefab == null) return;

            GameObject projectileGo = LeanPool.Spawn(_reflectedProjectilePrefab, spawnPosition, Quaternion.Euler(0, 0, 0f));
            _diContainer?.InjectGameObject(projectileGo);

            if (_animatorService != null)
            {
                _animatorService.RegisterProjectile(projectileGo, _reflectedProjectileSpeed);
            }
            else
            {
                Debug.LogError("[EnemyOfficer] AnimatorService не найден!");
            }
        }

        private void ScheduleShieldRegenIfNeeded()
        {
            if (_maxShieldLevel <= 0 || _currentShieldLevel >= _maxShieldLevel)
            {
                _isShieldRegenScheduled = false;
                return;
            }

            if (!_isShieldRegenScheduled)
            {
                _nextShieldRegenTime = Time.time + _shieldRegenIntervalSeconds;
                _isShieldRegenScheduled = true;
            }
        }

        private void HandleShieldRegeneration()
        {
            if (_maxShieldLevel <= 0 || _currentShieldLevel >= _maxShieldLevel)
            {
                _isShieldRegenScheduled = false;
                return;
            }

            if (!_isShieldRegenScheduled)
            {
                _nextShieldRegenTime = Time.time + _shieldRegenIntervalSeconds;
                _isShieldRegenScheduled = true;
                return;
            }

            if (Time.time >= _nextShieldRegenTime)
            {
                int previousShieldLevel = _currentShieldLevel;

                _currentShieldLevel += 1;
                _currentShieldLevel = Mathf.Min(_currentShieldLevel, _maxShieldLevel);

                _audioService?.PlayRandomShieldRegen();

                if (previousShieldLevel == 0 && _currentShieldLevel > 0 && _shieldObject != null)
                {
                    _shieldObject.SetActive(true);
                }

                UpdateShieldVisual(forceUpdate: true);

                if (_currentShieldLevel < _maxShieldLevel)
                {
                    _nextShieldRegenTime = Time.time + _shieldRegenIntervalSeconds;
                }
                else
                {
                    _isShieldRegenScheduled = false;
                }
            }
        }

        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (_shieldMat == null) return;

            if (_lastShownShieldLevel != _currentShieldLevel || forceUpdate)
            {
                _lastShownShieldLevel = _currentShieldLevel;
                _shieldMat.mainTextureOffset = new Vector2(0.2f * _lastShownShieldLevel, 0);
            }
        }


        // ==========================================
        // ПОРЯДОК ОТСТРЕЛА ДЕТАЛЕЙ (парами по 2, слева направо по списку)
        // ==========================================

        private readonly List<List<GameObject>> _partGroups = new List<List<GameObject>>();

        /// <summary>
        /// Разбивает _activeObjectsList (уже собранный в EnemyBase из Objects To Disable)
        /// на группы по 2 элемента, сохраняя исходный порядок из инспектора.
        /// </summary>
        private void BuildPartGroups()
        {
            _partGroups.Clear();

            for (int i = 0; i < _activeObjectsList.Count; i += 2)
            {
                List<GameObject> group = new List<GameObject>();
                group.Add(_activeObjectsList[i]);

                if (i + 1 < _activeObjectsList.Count)
                {
                    group.Add(_activeObjectsList[i + 1]);
                }

                _partGroups.Add(group);
            }
        }

        /// <summary>
        /// Переопределённая логика отстрела деталей: сначала случайно выбирается объект
        /// из ПЕРВОЙ пары (индексы 0-1) списка Objects To Disable, пока пара не опустеет —
        /// затем переходим к следующей паре (2-3) и так далее.
        /// </summary>
        protected override void HandlePartDetachment()
        {
            List<GameObject> currentGroup = null;

            foreach (List<GameObject> group in _partGroups)
            {
                if (group.Count > 0)
                {
                    currentGroup = group;
                    break;
                }
            }

            // Все детали уже отстрелены
            if (currentGroup == null)
                return;

            int randomIndex = Random.Range(0, currentGroup.Count);
            GameObject targetObj = currentGroup[randomIndex];

            if (targetObj != null)
            {
                Vector3 partPosition = targetObj.transform.position;
                Quaternion partRotation = targetObj.transform.rotation;

                targetObj.SetActive(false);

                if (hitEffectPrefab != null)
                {
                    GameObject spawnedEffect = LeanPool.Spawn(hitEffectPrefab, partPosition, partRotation);
                    StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, effectDestroyDelay));
                }

                _audioService?.PlayRandomEnemyHit();
            }

            currentGroup.RemoveAt(randomIndex);
            _activeObjectsList.Remove(targetObj);
        }


        // ==========================================
        // ЛАЗЕР
        // ==========================================

        private IEnumerator LaserAttackRoutine()
        {
            float minInitial = Mathf.Min(_laserMinInitialDelay, _laserMaxInitialDelay);
            float maxInitial = Mathf.Max(_laserMinInitialDelay, _laserMaxInitialDelay);
            float randomInitialDelay = Random.Range(minInitial, maxInitial);

            yield return new WaitForSeconds(randomInitialDelay);

            while (true)
            {
                // --- ПОДГОТОВКА (коллайдер выключен) ---
                if (_laserPrepareObject != null) _laserPrepareObject.SetActive(true);
                if (_laserCollider != null) _laserCollider.enabled = false;

                _audioService?.PlayRandomLaserPrepare();

                yield return new WaitForSeconds(_laserPrepareDuration);

                if (_laserPrepareObject != null) _laserPrepareObject.SetActive(false);


                // --- ВЫСТРЕЛ (коллайдер включен) ---
                if (_laserActiveObject != null)
                {
                    SetLaserXScale(0.05f);
                    _laserActiveObject.SetActive(true);
                }

                if (_laserCollider != null) _laserCollider.enabled = true;

                _audioService?.PlayRandomLaserShoot();

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
                if (_laserActiveObject != null) _laserActiveObject.SetActive(false);

                float min = Mathf.Min(_laserMinFireInterval, _laserMaxFireInterval);
                float max = Mathf.Max(_laserMinFireInterval, _laserMaxFireInterval);
                yield return new WaitForSeconds(Random.Range(min, max));
            }
        }

        private void SetLaserXScale(float scaleX)
        {
            if (_laserActiveObject == null) return;
            Vector3 currentScale = _laserActiveObject.transform.localScale;
            currentScale.x = scaleX;
            _laserActiveObject.transform.localScale = currentScale;
        }


        // ==========================================
        // РАКЕТНИЦА
        // ==========================================

        private void RotateTowardsPlayer()
        {
            Vector3 worldDirection = _player.transform.position - _rotatingPart.position;
            worldDirection.z = 0f;

            if (worldDirection != Vector3.zero)
            {
                Vector3 localDirection = _rotatingPart.parent != null
                    ? _rotatingPart.parent.InverseTransformDirection(worldDirection)
                    : worldDirection;

                localDirection.z = 0f;

                float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
                float targetZAngle = angle + 90f;

                Quaternion targetLocalRotation = Quaternion.Euler(0f, 0f, targetZAngle);
                Quaternion nextLocalRotation = Quaternion.Slerp(_rotatingPart.localRotation, targetLocalRotation, _rotationSpeed * Time.deltaTime);

                _rotatingPart.localEulerAngles = new Vector3(0f, 0f, nextLocalRotation.eulerAngles.z);
            }
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
            }

            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            _diContainer?.InjectGameObject(rocketGo);

            PlayRocketShootSound();

            if (rocketGo.TryGetComponent<EnemyFollowProjectile>(out var followProjectile))
            {
                followProjectile.SetSpeed(_rocketSpeed);
                return;
            }

            if (_animatorService != null)
            {
                if (_rocketShootStraightDown)
                    _animatorService.RegisterProjectile(rocketGo, _rocketSpeed);
                else
                    _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer.normalized);
            }
        }

        private void PlayRocketShootSound()
        {
            if (_audioService == null) return;

            if (_shooterType == WeaponShooterType.RocketLauncher)
            {
                _audioService.PlayRandomEnemyRocketLaunch();
            }
            else
            {
                _audioService.PlayRandomEnemyRegularShoot();
            }
        }
    }
}