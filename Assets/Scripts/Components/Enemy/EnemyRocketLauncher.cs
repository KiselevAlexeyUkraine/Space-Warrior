using UnityEngine;
using Lean.Pool;
using Zenject;
using Servise;
using Components.Player;

namespace Components.Enemy
{
    public class EnemyRocketLauncher : EnemyBase
    {
        [Header("Rocket Launcher Settings")]
        [Tooltip("Префаб ракеты (без скриптов, только визуал и коллайдер).")]
        [SerializeField] private GameObject _rocketPrefab;

        [Tooltip("Точка вылета ракеты.")]
        [SerializeField] private Transform _firePoint;

        [Header("Randomized Interval Settings")]
        [SerializeField, Min(0.1f)] private float minFireInterval = 2f;
        [SerializeField, Min(0.1f)] private float maxFireInterval = 4f;
        [SerializeField] private float _initialShotDelay = 1f;

        [Tooltip("Скорость полета ракеты в сторону игрока.")]
        [SerializeField] private float _rocketSpeed = 12f;

        private DiContainer _diContainer;
        private AnimatorService _animatorService;
        private PlayerMove _player;
        private float _nextFireTime;

        [Inject]
        private void ConstructLauncher(DiContainer diContainer, AnimatorService animatorService, PlayerMove player)
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
            if (_player == null)
            {
                Debug.LogWarning($"[{name}] Не могу выстрелить: PlayerComponent не найден в DiContainer!");
                return;
            }

            Transform spawnPoint = _firePoint != null ? _firePoint : transform;

            // 1. Считаем направление до игрока на момент выстрела
            Vector3 directionToPlayer = _player.transform.position - spawnPoint.position;
            directionToPlayer.z = 0f; // Оставляем полет строго в 2D (X, Y)

            // 2. Рассчитываем поворот ракеты в сторону направления полета (синей стрелкой Z вперед)
            Quaternion rocketRotation = spawnPoint.rotation;
            if (directionToPlayer != Vector3.zero)
            {
                float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                // Смещаем на -90 градусов, если префаб ракеты изначально смотрит «вверх»
                rocketRotation = Quaternion.Euler(0, 0, angle - 90f); 
            }

            // 3. Спавним ракету
            GameObject rocketGo = LeanPool.Spawn(_rocketPrefab, spawnPoint.position, rocketRotation);
            _diContainer?.InjectGameObject(rocketGo);

            // 4. Регистрируем в сервисе с вектором направления
            if (_animatorService != null)
            {
                _animatorService.RegisterDirectedProjectile(rocketGo, _rocketSpeed, directionToPlayer);
            }
        }

        public override void Move()
        {
            // Обычное поведение вне сетки (полет вниз)
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }
    }
}