using UnityEngine;
using System.Collections;
using Zenject;
using Lean.Pool;
using Components.Player;

namespace Components.Weapon
{
    public class EnemyFollowProjectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Скорость преследования цели.")]
        [SerializeField] private float _speed = 12f;

        [Header("Collision Settings")]
        [Tooltip("Слой, при столкновении с которым пуля взрывается (например, Player).")]
        [SerializeField] private LayerMask _targetLayer;

        [Header("Visual Settings")]
        // ИСПРАВЛЕНО: Теперь здесь компонент Renderer вместо GameObject
        [Tooltip("Основной рендерер пули (SpriteRenderer или MeshRenderer), который отключится при взрыве.")]
        [SerializeField] private Renderer _projectileVisual;

        [Header("Explosion (LeanPool Particle) Settings")]
        [Tooltip("Префаб системы частиц взрыва, который будет создаваться через LeanPool.")]
        [SerializeField] private ParticleSystem _explosionParticlePrefab;

        [Header("Explosion (SetActive) Settings")]
        [Tooltip("Дочерний объект взрыва, который включится через SetActive(true).")]
        [SerializeField] private GameObject _explosionObject;

        [Tooltip("Время (в секундах), на которое включается объект взрыва перед тем, как пуля уйдет в пул.")]
        [SerializeField, Min(0f)] private float _explosionDuration = 1f;

        [Header("Audio Settings")]
        [Tooltip("Звуковой клип взрыва снаряда.")]
        [SerializeField] private AudioClip _explosionSound;

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

        private void Awake()
        {
            _mainCollider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _isExploding = false;
            
            if (_mainCollider != null) _mainCollider.enabled = true;
            // ИСПРАВЛЕНО: Включаем рендерер обратно при выходе из пула
            if (_projectileVisual != null) _projectileVisual.enabled = true;
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
            if (_isExploding) return;

            int objectLayer = other.gameObject.layer;

            if ((_targetLayer.value & (1 << objectLayer)) != 0)
            {
                StartCoroutine(ExplosionRoutine());
            }
        }

        private IEnumerator ExplosionRoutine()
        {
            _isExploding = true;

            if (_mainCollider != null) _mainCollider.enabled = false;
            // ИСПРАВЛЕНО: Выключаем только рендерер, визуал скрывается, но объект активен
            if (_projectileVisual != null) _projectileVisual.enabled = false;

            if (_audioService != null && _explosionSound != null)
            {
                _audioService.PlayOneShot(_explosionSound);
            }

            if (_explosionParticlePrefab != null)
            {
                LeanPool.Spawn(_explosionParticlePrefab, transform.position, Quaternion.identity);
            }

            if (_explosionObject != null)
            {
                _explosionObject.SetActive(true);
            }

            yield return new WaitForSeconds(_explosionDuration);

            if (_explosionObject != null) _explosionObject.SetActive(false);
            
            SafeDespawn();
        }

        private void SafeDespawn()
        {
            if (LeanPool.Links.ContainsKey(gameObject))
            {
                LeanPool.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}