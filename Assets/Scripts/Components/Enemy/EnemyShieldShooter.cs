using UnityEngine;
using Lean.Pool;
using Zenject;
using Servise;

namespace Components.Enemy
{
    public class EnemyReflector : EnemyBase
    {
        [Header("Shield Settings")]
        [Tooltip("Объект щита (с Renderer), который висит на враге.")]
        [SerializeField] private GameObject _shieldObject;
        
        [Tooltip("Максимальный уровень щита (сколько снарядов он может отразить). Перезаписывается из таблицы.")]
        [SerializeField] private int _maxShieldLevel = 3;
        
        [Tooltip("Скорость вращения щита вокруг своей оси.")]
        [SerializeField] private float _rotationsPerSecond = 0.1f;

        [Header("Reflection (Shooting) Settings")]
        [Tooltip("Префаб вражеского снаряда, который спавнится ПРИ ОТРАЖЕНИИ.")]
        [SerializeField] private GameObject _reflectedProjectilePrefab;

        [Tooltip("Скорость полета отраженного снаряда вниз. Перезаписывается из таблицы.")]
        [SerializeField] private float _reflectedProjectileSpeed = 15f;

        private int _currentShieldLevel;
        private Material _shieldMat;
        private int _lastShownLevel = -1;

        private DiContainer _diContainer;
        private AnimatorService _animatorService;

        [Inject]
        private void ConstructReflector(DiContainer diContainer, AnimatorService animatorService)
        {
            _diContainer = diContainer;
            _animatorService = animatorService; 
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
        }

        // Перезаписываем параметры отражения из таблицы баланса
        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null)
            {
                if (debugBalanceValues)
                {
                    Debug.LogWarning($"[{gameObject.name}] BalanceConfig не внедрён — рефлектор использует значения из инспектора: maxShieldLevel={_maxShieldLevel}, reflectedProjectileSpeed={_reflectedProjectileSpeed}");
                }
                return;
            }

            // Получаем количество отражений и скорость полета снаряда из обновленного BalanceConfig
            _maxShieldLevel = Mathf.RoundToInt(balanceConfig.GetEnemyReflectionCount(enemyLevel));
            _reflectedProjectileSpeed = balanceConfig.GetEnemyReflectedProjectileSpeed(enemyLevel);

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({GetType().Name}, уровень {enemyLevel}) из баланса: " +
                          $"Количество отражений = {_maxShieldLevel}, Скорость отраженного снаряда = {_reflectedProjectileSpeed}");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _currentShieldLevel = _maxShieldLevel;
            _lastShownLevel = -1;
            
            if (_shieldObject != null)
            {
                // Включаем или выключаем щит изначально в зависимости от того, есть ли у него заряды отражения
                _shieldObject.SetActive(_currentShieldLevel > 0);
            }
            
            UpdateShieldVisual(forceUpdate: true);
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        protected override void Update()
        {
            base.Update();

            // Вращаем щит, если он активен
            if (_shieldObject != null && _shieldObject.activeSelf)
            {
                float rZ = -(_rotationsPerSecond * Time.time * 360f) % 360f;
                _shieldObject.transform.rotation = Quaternion.Euler(0, 0, rZ);
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            // Проверяем уровень прочности щита напрямую, как в EnemyWithShield
            if (_currentShieldLevel > 0 && other.CompareTag("ProjectileHero"))
            {
                // Спавним ответный снаряд в точке столкновения
                SpawnReflectedProjectile(other.transform.position);

                // Отражение забирает ровно 1 единицу прочности щита
                _currentShieldLevel -= 1;
                ShowDamage(); 

                if (_currentShieldLevel <= 0)
                {
                    _currentShieldLevel = 0;
                    if (_shieldObject != null)
                    {
                        _shieldObject.SetActive(false); // Полностью ломаем щит без регенерации
                    }
                }

                UpdateShieldVisual(forceUpdate: true);

                // Деспавним оригинальный патрон игрока
                SafeDespawn(other.gameObject);
                return; 
            }

            // Если щит уничтожен (_currentShieldLevel <= 0) — урон проходит по корпусу врага
            base.OnTriggerEnter(other);
        }

        private void SpawnReflectedProjectile(Vector3 spawnPosition)
        {
            if (_reflectedProjectilePrefab == null) return;

            // Разворачиваем снаряд на 180 градусов, чтобы он полетел вниз в сторону игрока
            GameObject projectileGo = LeanPool.Spawn(_reflectedProjectilePrefab, spawnPosition, Quaternion.Euler(0, 0, 180f));
            
            _diContainer?.InjectGameObject(projectileGo);

            if (_animatorService != null)
            {
                _animatorService.RegisterProjectile(projectileGo, _reflectedProjectileSpeed);
            }
            else
            {
                Debug.LogError("[EnemyReflector] AnimatorService не найден!");
            }
        }

        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (_shieldMat == null) return;

            if (_lastShownLevel != _currentShieldLevel || forceUpdate)
            {
                _lastShownLevel = _currentShieldLevel;
                // Сдвигаем текстуру в зависимости от оставшегося количества отражений
                _shieldMat.mainTextureOffset = new Vector2(0.2f * _lastShownLevel, 0);
            }
        }
    }
}