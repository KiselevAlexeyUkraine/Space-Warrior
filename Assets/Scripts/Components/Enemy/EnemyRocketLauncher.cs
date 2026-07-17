using UnityEngine;
using Lean.Pool;
using Zenject;
using Servise;
using Components.Player;
using Components.Weapon;

namespace Components.Enemy
{
    public class EnemyRocketLauncher : EnemyBase
    {
        public enum WeaponShooterType
        {
            StraightDownShooter, // Обычный стрелок (вниз)
            TargetPlayerShooter, // Обычный стрелок (в игрока)
            RocketLauncher       // Враг с ракетами (использует EnemyFollowProjectile)
        }

        [Header("Универсальный тип стрелка")]
        [SerializeField] private WeaponShooterType _shooterType = WeaponShooterType.RocketLauncher;

        [Header("Weapon Settings")]
        [Tooltip("Префаб снаряда (для ракетчика — префаб со скриптом EnemyFollowProjectile).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета снаряда.")]
        [SerializeField] private Transform _firePoint;

        [Header("Rotation Settings (Для тех, кто целится в игрока)")]
        [Tooltip("Объект, который будет вращаться в сторону игрока (например, пушка/голова турели).")]
        [SerializeField] private Transform _rotatingPart;
        [Tooltip("Скорость поворота пушки к игроку.")]
        [SerializeField] private float _rotationSpeed = 10f;

        [Header("Interval Settings")]
        [SerializeField] private float _initialShotDelay = 1f;
        [Tooltip("Минимальный интервал между выстрелами (вычисляется из конфига или задается вручную).")]
        [SerializeField] private float _minFireInterval = 2f;
        [Tooltip("Максимальный интервал между выстрелами (вычисляется из конфига или задается вручную).")]
        [SerializeField] private float _maxFireInterval = 4f;

        private float _rocketSpeed = 12f;
        private bool _shootStraightDown = false;
        private float _nextFireTime;

        private DiContainer _diContainer;
        private AnimatorService _animatorService;
        private PlayerMove _player;

        [Inject]
        private void ConstructLauncher(DiContainer diContainer, AnimatorService animatorService, PlayerMove player = null)
        {
            _diContainer = diContainer;
            _animatorService = animatorService;
            _player = player; 
        }

        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null) return;

            float baseInterval = 3f;

            switch (_shooterType)
            {
                case WeaponShooterType.StraightDownShooter:
                    _shootStraightDown = true;
                    baseInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.TargetPlayerShooter:
                    _shootStraightDown = false;
                    baseInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.RocketLauncher:
                    _shootStraightDown = false; 
                    // Если в балансе настроены ракеты, раскомментируйте строки ниже:
                    // baseInterval = balanceConfig.GetEnemyRocketFireRate(enemyLevel);
                    // _rocketSpeed = balanceConfig.GetEnemyRocketSpeed(enemyLevel);
                    break;
            }

            // Настраиваем разброс времени стрельбы (например, +- 25% от базового интервала)
            _minFireInterval = baseInterval * 0.75f;
            _maxFireInterval = baseInterval * 1.25f;

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({_shooterType}, Lvl: {enemyLevel}) Диапазон стрельбы: {_minFireInterval:F1}с - {_maxFireInterval:F1}с, Скорость: {_rocketSpeed}");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Первый выстрел произойдет через начальную задержку со случайным смещением
            _nextFireTime = Time.time + _initialShotDelay + Random.Range(0f, 0.5f);
        }

        protected override void Update()
        {
            base.Update();

            // Поворот пушки в сторону игрока (если это не StraightDownShooter)
            if (!_shootStraightDown && _player != null && _rotatingPart != null)
            {
                RotateTowardsPlayer();
            }

            if (Time.time >= _nextFireTime)
            {
                ShootRocket();
                // Рассчитываем случайное время до следующего выстрела
                _nextFireTime = Time.time + Random.Range(_minFireInterval, _maxFireInterval);
            }
        }

        private void RotateTowardsPlayer()
        {
            // 1. Получаем направление на игрока в мировых координатах
            Vector3 worldDirection = _player.transform.position - _rotatingPart.position;
            worldDirection.z = 0f; // Работаем в плоскости 2D

            if (worldDirection != Vector3.zero)
            {
                // 2. Переводим это направление в ЛОКАЛЬНОЕ пространство родителя нашей пушки
                Vector3 localDirection = _rotatingPart.parent != null 
                    ? _rotatingPart.parent.InverseTransformDirection(worldDirection) 
                    : worldDirection;

                localDirection.z = 0f;

                // 3. Считаем локальный угол
                float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
        
                float targetZAngle = angle + 90f; 

                // 4. Создаем целевой ЛОКАЛЬНЫЙ кватернион
                Quaternion targetLocalRotation = Quaternion.Euler(0f, 0f, targetZAngle);

                // 5. Интерполируем именно ЛОКАЛЬНЫЙ поворот
                Quaternion nextLocalRotation = Quaternion.Slerp(_rotatingPart.localRotation, targetLocalRotation, _rotationSpeed * Time.deltaTime);

                // 6. Жестко вбиваем локальные X и Y в 0.0, а Z берем из интерполяции
                _rotatingPart.localEulerAngles = new Vector3(0f, 0f, nextLocalRotation.eulerAngles.z);
            }
        }

        private void ShootRocket()
        {
            if (_rocketPrefab == null) return;
            if (!_shootStraightDown && _player == null) return;

            // Если есть вращающаяся часть, спавним от её направления, иначе от обычной точки спавна
            Transform spawnPoint = _firePoint != null ? _firePoint : transform;
            Vector3 directionToPlayer;
            Quaternion rocketRotation;

            if (_shootStraightDown)
            {
                directionToPlayer = Vector3.down;
                rocketRotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                // Направление полета ракеты
                directionToPlayer = _player.transform.position - spawnPoint.position;
                directionToPlayer.z = 0f;

                // Направление спавна берем из точки вылета (которая крутится вместе с турелью)
                rocketRotation = spawnPoint.rotation;
            }

            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            
            _diContainer?.InjectGameObject(rocketGo);

            // --- ВОСПРОИЗВЕДЕНИЕ ЗВУКА ВЫСТРЕЛА ---
            PlayShootSound();

            // Если это ракета со скриптом преследования, передаем ей скорость из таблицы
            if (rocketGo.TryGetComponent<EnemyFollowProjectile>(out var followProjectile))
            {
                followProjectile.SetSpeed(_rocketSpeed);
                return;
            }

            // Логика для обычных "тупых" пуль стрелков
            if (_animatorService != null)
            {
                if (_shootStraightDown)
                    _animatorService.RegisterProjectile(rocketGo, _rocketSpeed);
                else
                    _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer.normalized);
            }
        }

        private void PlayShootSound()
        {
            if (_audioService == null) return;

            // Вызываем подходящий метод вашего AudioService
            if (_shooterType == WeaponShooterType.RocketLauncher)
            {
                // Замените название метода на то, которое используется у вас для ракеты
                _audioService.PlayRandomEnemyRocketLaunch(); 
            }
            else
            {
                // Замените название метода на то, которое используется у вас для обычного выстрела
                _audioService.PlayRandomEnemyRegularShoot(); 
            }
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }
    }
}