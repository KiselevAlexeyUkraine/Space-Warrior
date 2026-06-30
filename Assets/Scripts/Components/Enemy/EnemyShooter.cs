using UnityEngine;
using Components.Weapon;
using Lean.Pool;
using Zenject;
using Servise;

namespace Components.Enemy
{
    public class EnemyShieldShooter : EnemyBase
    {
        [Header("Shield Settings")]
        [Tooltip("Объект щита (с Renderer), который висит на враге.")]
        [SerializeField] private GameObject _shieldObject;
        
        [Tooltip("Максимальный уровень щита (сколько снарядов он может отразить).")]
        [SerializeField] private int _maxShieldLevel = 3;
        
        [Tooltip("Скорость вращения щита вокруг своей оси.")]
        [SerializeField] private float _rotationsPerSecond = 0.1f;

        [Header("Reflection (Shooting) Settings")]
        [Tooltip("Префаб вражеского снаряда, который спавнится ПРИ ОТРАЖЕНИИ.")]
        [SerializeField] private GameObject _reflectedProjectilePrefab;

        [Tooltip("Скорость полета отраженного снаряда вниз. Считывается AnimatorService.")]
        [SerializeField] private float _reflectedProjectileSpeed = 15f;

        private float _currentShieldLevel;
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
                    // Используем .material, чтобы у каждого врага была своя копия материала
                    _shieldMat = shieldRend.material;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // Сброс щита при активации из пула
            _currentShieldLevel = _maxShieldLevel;
            _lastShownLevel = -1;
            
            if (_shieldObject != null)
            {
                _shieldObject.SetActive(true);
            }
            
            UpdateShieldVisual(forceUpdate: true);
        }

        public override void Move()
        {
            // Стандартное движение вниз (если не Grid Formation)
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }

        protected override void Update()
        {
            base.Update();

            // Вращаем щит точно так же, как у игрока
            if (_shieldObject != null && _shieldObject.activeSelf)
            {
                float rZ = -(_rotationsPerSecond * Time.time * 360f) % 360f;
                _shieldObject.transform.rotation = Quaternion.Euler(0, 0, rZ);
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            // Если щит активен и в него попал снаряд игрока
            if (_shieldObject != null && _shieldObject.activeSelf && other.CompareTag("ProjectileHero"))
            {
                // Сначала спавним ответный снаряд в точке столкновения
                SpawnReflectedProjectile(other.transform.position);

                // Тратим прочность щита
                _currentShieldLevel -= 1f;
                ShowDamage(); // Мигание самого корабля для фидбека

                // Проверяем прочность щита
                if (_currentShieldLevel <= 0)
                {
                    _currentShieldLevel = 0;
                    if (_shieldObject != null)
                    {
                        _shieldObject.SetActive(false); // Ломаем щит
                    }
                }

                UpdateShieldVisual();

                // Уничтожаем (деспавним) оригинальный патрон игрока
                SafeDespawn(other.gameObject);
                return; 
            }

            // Если щит пробит или это не пуля игрока — обычная логика урона
            base.OnTriggerEnter(other);
        }

        /// <summary>
        /// Спавнит новый вражеский префаб из точки, куда попал патрон игрока
        /// </summary>
        private void SpawnReflectedProjectile(Vector3 spawnPosition)
        {
            if (_reflectedProjectilePrefab == null) return;

            // Спавним префаб. Разворачиваем на 180 градусов (Quaternion.Euler(0, 0, 180f)), чтобы он летел вниз
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

        /// <summary>
        /// Сдвиг текстуры щита по принципу игрока
        /// </summary>
        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (_shieldMat == null) return;

            int currentVisualLevel = Mathf.FloorToInt(_currentShieldLevel);

            if (_lastShownLevel != currentVisualLevel || forceUpdate)
            {
                _lastShownLevel = currentVisualLevel;
                // Сдвигаем текстуру на 0.2 за каждый шаг прочности
                _shieldMat.mainTextureOffset = new Vector2(0.2f * _lastShownLevel, 0);
            }
        }
    }
}