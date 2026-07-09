using UnityEngine;
using Lean.Pool;
using Zenject;

namespace Components.Enemy
{
    public class EnemyReflector : EnemyBase
    {
        [Header("Shield Settings")]
        [Tooltip("Объект щита (с Renderer), который висит на враге.")]
        [SerializeField] private GameObject _shieldObject;

        [Tooltip("Партикл, спавнящийся при каждом отражении снаряда, пока щит не сбит до 0.")]
        [SerializeField] private GameObject _shieldHitEffectPrefab;

        [Tooltip("Время жизни спавнящегося партикла отражения (в секундах).")]
        [SerializeField] private float _shieldHitEffectDestroyDelay = 2f;

        [Tooltip("Партикл, спавнящийся один раз в момент полного уничтожения щита (когда щит доходит до 0).")]
        [SerializeField] private GameObject _shieldBreakEffectPrefab;

        [Tooltip("Время жизни спавнящегося партикла уничтожения щита (в секундах).")]
        [SerializeField] private float _shieldBreakEffectDestroyDelay = 2f;

        [Tooltip("Максимальный уровень щита (сколько снарядов он может отразить). Перезаписывается из таблицы.")]
        [SerializeField] private int _maxShieldLevel = 3;
        

        [Header("Reflection (Shooting) Settings")]
        [Tooltip("Префаб вражеского снаряда, который спавнится ПРИ ОТРАЖЕНИИ.")]
        [SerializeField] private GameObject _reflectedProjectilePrefab;

        [Tooltip("Скорость полета отраженного снаряда вниз. Перезаписывается из таблицы.")]
        [SerializeField] private float _reflectedProjectileSpeed = 15f;

        private int _currentShieldLevel;
        private Material _shieldMat;
        private int _lastShownLevel = -1;

        private DiContainer _diContainer;
        // УДАЛЕНО: private AnimatorService _animatorService; (теперь берется из EnemyBase)

        [Inject]
        private void ConstructReflector(DiContainer diContainer)
        {
            _diContainer = diContainer;
            // УДАЛЕНО: _animatorService = animatorService; (базовый класс сам инжектит его в своем Construct)
        }

        protected override void Awake()
        {
            base.Awake();
            if (_shieldObject != null)
            {
                var renderer = _shieldObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    _shieldMat = renderer.material;
                }
            }
        }

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
        

        protected override void OnTriggerEnter(Collider other)
        {
            if (_currentShieldLevel > 0 && other.CompareTag("ProjectileHero"))
            {
                SpawnReflectedProjectile(other.transform.position);

                _currentShieldLevel -= 1;

                if (_currentShieldLevel <= 0)
                {
                    _currentShieldLevel = 0;
                    if (_shieldObject != null)
                    {
                        _shieldObject.SetActive(false); 
                    }

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

            // _animatorService здесь доступен из базового класса EnemyBase
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
                _shieldMat.mainTextureOffset = new Vector2(0.2f * _lastShownLevel, 0);
            }
        }

        private System.Collections.IEnumerator DespawnEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (effect != null)
            {
                LeanPool.Despawn(effect);
            }
        }
    }
}