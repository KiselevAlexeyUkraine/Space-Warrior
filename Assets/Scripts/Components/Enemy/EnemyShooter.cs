using UnityEngine;
using Lean.Pool;
using Zenject;
using Servise;

namespace Components.Enemy
{
    public class EnemyShooter : EnemyBase
    {
        [Header("Shooting Settings")]
        [Tooltip("Префаб вражеского снаряда (может быть пустым префабом без скриптов).")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Точка (Трансформ), из которой будут вылетать снаряды.")]
        [SerializeField] private Transform _firePoint;

        // ИЗМЕНЕНО: Заменили фиксированный fireRate на рандомный интервал
        [Header("Randomized Interval Settings")]
        [Tooltip("Минимальное время ожидания между выстрелами (в секундах).")]
        [SerializeField, Min(0.1f)] private float minFireInterval = 1f;

        [Tooltip("Максимальное время ожидания между выстрелами (в секундах).")]
        [SerializeField, Min(0.1f)] private float maxFireInterval = 3f;

        [Tooltip("Задержка перед самым первым выстрелом после спавна врага.")]
        [SerializeField] private float _initialShotDelay = 0.5f;

        [Tooltip("Скорость полета пули вниз. Считывается AnimatorService.")]
        [SerializeField] private float _projectileSpeed = 15f;

        private DiContainer _diContainer;
        private AnimatorService _animatorService;
        private float _nextFireTime;

        [Inject]
        private void ConstructShooter(DiContainer diContainer, AnimatorService animatorService)
        {
            _diContainer = diContainer;
            _animatorService = animatorService; 
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Задаем время первого выстрела при активации из пула
            _nextFireTime = Time.time + _initialShotDelay;
        }

        protected override void Update()
        {
            base.Update();

            // Логика стрельбы по плавающему таймеру
            if (Time.time >= _nextFireTime)
            {
                Shoot();
                CalculateNextFireTime(); // Рассчитываем случайный интервал для следующего выстрела
            }
        }

        /// <summary>
        /// Выбирает случайный интервал времени и сдвигает точку следующего выстрела.
        /// </summary>
        private void CalculateNextFireTime()
        {
            // Подстраховка на случай, если в инспекторе перепутали min и max местами
            float min = Mathf.Min(minFireInterval, maxFireInterval);
            float max = Mathf.Max(minFireInterval, maxFireInterval);

            float randomDelay = Random.Range(min, max);
            _nextFireTime = Time.time + randomDelay;
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        private void Shoot()
        {
            if (_projectilePrefab == null) return;
            
            Transform spawnPoint = _firePoint != null ? _firePoint : transform;

            GameObject projectileGo = LeanPool.Spawn(_projectilePrefab, spawnPoint.position, spawnPoint.rotation);
            
            _diContainer?.InjectGameObject(projectileGo);

            if (_animatorService != null)
            {
                _animatorService.RegisterProjectile(projectileGo, _projectileSpeed);
            }
            else
            {
                Debug.LogError("[EnemyShooter] AnimatorService не найден! Пуля будет стоять на месте.");
            }
        }
    }
}