using UnityEngine;
using System.Collections;
using Zenject;
using Lean.Pool;
using Components.Player;
using Components.Enemy;

namespace Components.Weapon
{
    public class EnemyFollowProjectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Скорость преследования цели (Перезаписывается из таблицы через SetSpeed).")]
        [SerializeField] private float _speed = 12f;

        [Header("Collision Settings")]
        [Tooltip("Слой, при столкновении с которым пуля триггерится и взрывается (например, Player).")]
        [SerializeField] private LayerMask _targetLayer;

        [Header("Explosion AoE Settings")]
        [Tooltip("Радиус уничтожения объектов при взрыве.")]
        [SerializeField] private float _explosionRadius = 3f;
        
        [Tooltip("Слои объектов, которые будут уничтожены/деспавнены при попадании в радиус взрыва (например, Enemy, ProjectileHero, ProjectileEnemy).")]
        [SerializeField] private LayerMask _layersToDestroyInBlast;

        [Header("Visual Settings")]
        [Tooltip("Основной рендерер пули (SpriteRenderer или MeshRenderer), который отключится при взрыве.")]
        [SerializeField] private GameObject _projectileVisual;

        [Header("Explosion (LeanPool Particle) Settings")]
        [Tooltip("Префаб системы частиц взрыва, который будет создаваться через LeanPool.")]
        [SerializeField] private ParticleSystem _explosionParticlePrefab;

        [Header("Explosion (SetActive) Settings")]
        [Tooltip("Дочерний объект взрыва, который включится через SetActive(true).")]
        [SerializeField] private GameObject _explosionObject;

        [Tooltip("Время (в секундах), на которое включается объект взрыва перед тем, как пуля уйдет в пул.")]
        [SerializeField, Min(0f)] private float _explosionDuration = 1f;

        private AudioService _audioService;
        private PlayerMove _targetPlayer;
        private Collider _mainCollider;
        private bool _isExploding;

        [Inject]
        private void Construct(AudioService audioService, PlayerMove player = null)
        {
            _audioService = audioService;
            _targetPlayer = player;
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        private void Awake()
        {
            _mainCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _isExploding = false;
            
            if (_mainCollider != null) _mainCollider.enabled = true;
            if (_projectileVisual != null) _projectileVisual.SetActive(true);
            if (_explosionObject != null) _explosionObject.SetActive(false);
        }

        private void Update()
        {
            if (_isExploding) return;

            if (_targetPlayer == null)
            {
                transform.Translate(Vector3.up * _speed * Time.deltaTime);
                return;
            }

            Vector3 direction = _targetPlayer.transform.position - transform.position;
            direction.z = 0f;

            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }

            transform.Translate(Vector3.up * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Если мы уже взрываемся — игнорируем триггер
            if (_isExploding) return;

            int objectLayer = other.gameObject.layer;

            // Если коснулись объекта из маски _targetLayer, запускаем взрыв
            if ((_targetLayer.value & (1 << objectLayer)) != 0)
            {
                ExecuteExplosion();
            }
        }

        // Публичный метод для цепной детонации
        public void ExecuteExplosion()
        {
            // Проверяем флаг, чтобы корутина не запускалась дважды
            if (_isExploding) return;
            
            StartCoroutine(ExplosionRoutine());
        }

        private IEnumerator ExplosionRoutine()
        {
            _isExploding = true;

            if (_mainCollider != null) _mainCollider.enabled = false;
            if (_projectileVisual != null) _projectileVisual.SetActive(false);

            if (_audioService != null)
            {
                _audioService.PlayRandomEnemyRocketExplosion();
            }

            if (_explosionParticlePrefab != null)
            {
                LeanPool.Spawn(_explosionParticlePrefab, transform.position, Quaternion.identity);
            }

            if (_explosionObject != null)
            {
                _explosionObject.SetActive(true);
            }

            // Ищем и взрываем/деспавним всех вокруг
            ExplodeAoE();

            yield return new WaitForSeconds(_explosionDuration);

            if (_explosionObject != null) _explosionObject.SetActive(false);
            
            SafeDespawn(gameObject);
        }

        private void ExplodeAoE()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, _explosionRadius, _layersToDestroyInBlast);

            foreach (var hitCollider in hitColliders)
            {
                GameObject targetGo = hitCollider.gameObject;

                // Защита от самоподрыва
                if (targetGo == gameObject) continue;

                // 1. ПРОВЕРКА НА РАКЕТУ (Цепная детонация)
                var adjacentRocket = targetGo.GetComponent<EnemyFollowProjectile>();
                if (adjacentRocket != null)
                {
                    adjacentRocket.ExecuteExplosion();
                    continue;
                }

                // 2. ПРОВЕРКА НА ИГРОКА (Новое)
                // Ищем скрипт сбора/здоровья игрока на самом объекте или его родителях
                var player = targetGo.GetComponentInParent<PlayerCollect>();
                if (player != null)
                {
                    // Вызываем публичный/доступный метод нанесения урона у игрока
                    player.SendMessage("ProcessDamage", SendMessageOptions.DontRequireReceiver);
                    continue; // Игрока НЕЛЬЗЯ удалять через SafeDespawn! Он должен умереть сам.
                }

                // 3. ПРОВЕРКА НА ВРАГА
                var enemy = targetGo.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.SendMessage("ProcessDamage", WeaponType.blaster, SendMessageOptions.DontRequireReceiver);
                    continue; 
                }

                // 4. Остальные объекты (например, лазеры) просто убираем в пул
                SafeDespawn(targetGo);
            }
        }

        private void SafeDespawn(GameObject go)
        {
            if (go == null) return;

            if (LeanPool.Links.ContainsKey(go))
            {
                LeanPool.Despawn(go);
            }
            else
            {
                Destroy(go);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
    }
}