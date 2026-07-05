using UnityEngine;
using Zenject;
using Configs;

namespace Components.Player
{
    public sealed class PlayerMove : MonoBehaviour
    {
        private PlayerConfig _playerConfig;
        private DesktopInput _desktopInput;
        private BalanceConfig _balanceConfig;

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
            transform.rotation = Quaternion.Euler(
                _desktopInput.Vertical * _playerConfig.PitchMult,
                _desktopInput.Horizontal * _playerConfig.RollMult,
                0f);
        }
    }
}