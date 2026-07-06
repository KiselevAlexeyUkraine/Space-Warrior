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
            StraightDownShooter, // Обычный стрелок (стреляет строго вниз)
            TargetPlayerShooter, // Обычный стрелок (целится в игрока)
            RocketLauncher       // Враг с ракетницами
        }

        [Header("Универсальный тип стрелка")]
        [Tooltip("Определяет, из каких строк таблицы считывать параметры и куда вести огонь.")]
        [SerializeField] private WeaponShooterType _shooterType = WeaponShooterType.RocketLauncher;

        [Header("Weapon Settings")]
        [Tooltip("Префаб снаряда (пуля или ракета).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета снаряда.")]
        [SerializeField] private Transform _firePoint;

        [Header("Interval Settings")]
        [SerializeField] private float _initialShotDelay = 1f;

        // Эти параметры теперь динамически перезаписываются на основе типа и уровня врага из таблицы
        private float _rocketSpeed = 12f;
        private float _fireInterval = 3f;
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

        // Динамическое считывание параметров для каждого уровня и типа врага
        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null)
            {
                if (debugBalanceValues)
                {
                    Debug.LogWarning($"[{gameObject.name}] BalanceConfig не внедрён — используются дефолтные инспекторные значения.");
                }
                return;
            }

            switch (_shooterType)
            {
                case WeaponShooterType.StraightDownShooter:
                    _shootStraightDown = true;
                    // Строка 21 и 22: Обычные стрелки
                    _fireInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.TargetPlayerShooter:
                    _shootStraightDown = false;
                    // Строка 21 и 22: Обычные стрелки
                    _fireInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.RocketLauncher:
                    _shootStraightDown = false; 
                    // Строка 24 и 25: Ракетницы
                    _fireInterval = balanceConfig.GetEnemyRocketFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyRocketSpeed(enemyLevel);
                    break;
            }

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({GetType().Name} -> {_shooterType}, Уровень: {enemyLevel}) Успешно обновлен из таблицы: " +
                          $"Интервал стрельбы = {_fireInterval} сек, Скорость полета = {_rocketSpeed}, Направление вниз = {_shootStraightDown}");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _nextFireTime = Time.time + _initialShotDelay;
        }

        protected override void Update()
        {
            base.Update();

            if (Time.time >= _nextFireTime)
            {
                ShootRocket();
                CalculateNextFireTime();
            }
        }

        private void CalculateNextFireTime()
        {
            // Берем строгое значение интервала из таблицы баланса (для текущего уровня)
            _nextFireTime = Time.time + _fireInterval;
        }

        private void ShootRocket()
        {
            if (_rocketPrefab == null) return;
            
            if (!_shootStraightDown && _player == null)
            {
                Debug.LogWarning($"[{name}] Не могу выстрелить с наведением: PlayerComponent не найден в DiContainer!");
                return;
            }

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
                directionToPlayer = _player.transform.position - spawnPoint.position;
                directionToPlayer.z = 0f;

                rocketRotation = spawnPoint.rotation;
                if (directionToPlayer != Vector3.zero)
                {
                    float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    rocketRotation = Quaternion.Euler(0, 0, angle - 90f); 
                }
            }

            // Спавним снаряд/ракету
            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            
            // Прокидываем контекст и зависимости Zenject (AudioService, PlayerMove и др.)
            _diContainer?.InjectGameObject(rocketGo);

            // Если на префабе есть скрипт умного движения, он сам перехватит инициализацию
            if (rocketGo.TryGetComponent<EnemyFollowProjectile>(out var followProjectile))
            {
                return;
            }

            // Если «тупой» снаряд — пускаем его по заданной траектории со скоростью из таблицы
            if (_animatorService != null)
            {
                if (_shootStraightDown)
                {
                    _animatorService.RegisterProjectile(rocketGo, _rocketSpeed);
                }
                else
                {
                    _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer);
                }
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