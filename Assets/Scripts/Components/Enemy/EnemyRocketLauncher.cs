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

        [Header("Interval Settings")]
        [SerializeField] private float _initialShotDelay = 1f;

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

        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null) return;

            switch (_shooterType)
            {
                case WeaponShooterType.StraightDownShooter:
                    _shootStraightDown = true;
                    _fireInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.TargetPlayerShooter:
                    _shootStraightDown = false;
                    _fireInterval = balanceConfig.GetEnemyBulletFireRate(enemyLevel);
                    _rocketSpeed = balanceConfig.GetEnemyBulletSpeed(enemyLevel);
                    break;

                case WeaponShooterType.RocketLauncher:
                    _shootStraightDown = false; 
                    // Строка 24: Скорость запуска ракеты, Строка 25: Скорость полета ракеты
                    //_fireInterval = balanceConfig.GetEnemyRocketFireRate(enemyLevel);
                    //_rocketSpeed = balanceConfig.GetEnemyRocketSpeed(enemyLevel);
                    break;
            }

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({_shooterType}, Lvl: {enemyLevel}) Интервал: {_fireInterval}с, Скорость: {_rocketSpeed}");
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
                _nextFireTime = Time.time + _fireInterval;
            }
        }

        private void ShootRocket()
        {
            if (_rocketPrefab == null) return;
            if (!_shootStraightDown && _player == null) return;

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

            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            
            _diContainer?.InjectGameObject(rocketGo);

            // ИСПРАВЛЕНО/ДОБАВЛЕНО: Если это ракета со скриптом преследования, передаем ей скорость из таблицы
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
                    _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer);
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