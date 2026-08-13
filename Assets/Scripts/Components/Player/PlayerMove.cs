using Configs;
using UnityEngine;
using Zenject;

namespace Components.Player
{
    public sealed class PlayerMove : MonoBehaviour
    {
        [Header("Engine Particle Systems")]
        [SerializeField] 
        [Tooltip("Партикл двигателей при движении ВПЕРЕД (+Y)")]
        private ParticleSystem _forwardParticle;

        [SerializeField] 
        [Tooltip("Партикл двигателей при движении ВЛЕВО (-X)")]
        private ParticleSystem _leftParticle;

        [SerializeField] 
        [Tooltip("Партикл двигателей при движении ВПРАВО (+X)")]
        private ParticleSystem _rightParticle;

        [SerializeField] 
        [Tooltip("Первый партикл двигателей при движении НАЗАД (-Y)")]
        private ParticleSystem _backParticleLeft;

        [SerializeField] 
        [Tooltip("Второй партикл двигателей при движении НАЗАД (-Y)")]
        private ParticleSystem _backParticleRight;

        private PlayerConfig _playerConfig;
        private DesktopInput _desktopInput;
        private BalanceConfig _balanceConfig;

        // Порог срабатывания ввода (чтобы избежать микро-срабатываний от геймпада)
        private const float InputThreshold = 0.01f;

        [Inject]
        private void Construct(
            DesktopInput desktopInput,
            PlayerConfig playerConfig,
            BalanceConfig balanceConfig)
        {
            _desktopInput = desktopInput;
            _playerConfig = playerConfig;
            _balanceConfig = balanceConfig;
        }

        private void Update()
        {
            Move();
            Rotate();
            UpdateEngineParticles();
        }

        private void Move()
        {
            Vector3 position = transform.position;

            position.x += _desktopInput.Horizontal *
                          _balanceConfig.PlayerMoveSpeed *
                          Time.deltaTime;

            position.y += _desktopInput.Vertical *
                          _balanceConfig.PlayerMoveSpeed *
                          Time.deltaTime;

            transform.position = position;
        }

        private void Rotate()
        {
            // Вычисляем целевой угол поворота на основе ввода
            float targetPitch = _desktopInput.Vertical * _playerConfig.PitchMult;
            float targetRoll = _desktopInput.Horizontal * _playerConfig.RollMult;

            Quaternion targetRotation = Quaternion.Euler(targetPitch, targetRoll, 0f);

            // Плавный переход от текущего поворота к целевому
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _playerConfig.RotationSpeed
            );
        }

        private void UpdateEngineParticles()
        {
            float horizontal = _desktopInput.Horizontal;
            float vertical = _desktopInput.Vertical;

            // Движение вперед (+Y)
            bool isMovingForward = vertical > InputThreshold;
            SetParticleState(_forwardParticle, isMovingForward);

            // Движение назад (-Y) — управляет двумя партиклами
            bool isMovingBack = vertical < -InputThreshold;
            SetParticleState(_backParticleLeft, isMovingBack);
            SetParticleState(_backParticleRight, isMovingBack);

            // Движение влево (-X)
            bool isMovingLeft = horizontal < -InputThreshold;
            SetParticleState(_leftParticle, isMovingLeft);

            // Движение вправо (+X)
            bool isMovingRight = horizontal > InputThreshold;
            SetParticleState(_rightParticle, isMovingRight);
        }

        /// <summary>
        /// Включает или выключает эмиссию частиц без лишних перезапусков
        /// </summary>
        private void SetParticleState(ParticleSystem particle, bool shouldPlay)
        {
            if (particle == null) return;

            if (shouldPlay)
            {
                if (!particle.isEmitting)
                    particle.Play();
            }
            else
            {
                if (particle.isEmitting)
                    particle.Stop();
            }
        }
    }
}