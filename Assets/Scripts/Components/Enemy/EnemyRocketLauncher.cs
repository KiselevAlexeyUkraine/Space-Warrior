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
        [Header("Rocket Launcher Settings")]
        [Tooltip("Префаб ракеты (без скриптов, только визуал и коллайдер).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета ракеты.")]
        [SerializeField] private Transform _firePoint;

        [Tooltip("Если включено, ракета летит строго вниз. Если выключено — целится в игрока.")]
        [SerializeField] private bool _shootStraightDown = false;

        [Header("Randomized Interval Settings")]
        [SerializeField, Min(0.1f)] private float minFireInterval = 2f;
        [SerializeField, Min(0.1f)] private float maxFireInterval = 4f;
        [SerializeField] private float _initialShotDelay = 1f;

        [Tooltip("Скорость полета ракеты (используется, только если нет скрипта авто-движения).")]
        [SerializeField] private float _rocketSpeed = 12f;

        private DiContainer _diContainer;
        private AnimatorService _animatorService;
        private PlayerMove _player;
        private float _nextFireTime;

        [Inject]
        private void ConstructLauncher(DiContainer diContainer, AnimatorService animatorService, PlayerMove player = null)
        {
            _diContainer = diContainer;
            _animatorService = animatorService;
            _player = player; 
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
            float min = Mathf.Min(minFireInterval, maxFireInterval);
            float max = Mathf.Max(minFireInterval, maxFireInterval);
            _nextFireTime = Time.time + Random.Range(min, max);
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

            // Спавним ракету
            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            
            // КРИТИЧЕСКИ ВАЖНО: Инжектим зависимости, чтобы EnemyFollowProjectile внутри себя получил AudioService и PlayerMove
            _diContainer?.InjectGameObject(rocketGo);

            // ИСПРАВЛЕНО: Проверяем, есть ли на снаряде скрипт EnemyFollowProjectile
            if (rocketGo.TryGetComponent<EnemyFollowProjectile>(out var followProjectile))
            {
                // Если скрипт есть, он сам управляет движением в своем Update().
                // Никуда в AnimatorService его не пихаем, просто выходим из метода!
                return;
            }

            // Если скриптаEnemyFollowProjectile нет — значит это обычная «тупая» пуля,
            // отдаем её управление стандартному сервису анимации
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