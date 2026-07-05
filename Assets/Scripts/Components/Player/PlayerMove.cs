using UnityEngine;
using Zenject;
using Configs;

namespace Components.Player
{
    public sealed class PlayerMove : MonoBehaviour
    {
        private PlayerConfig _playerConfig;
        private DesktopInput _desktopInput;

        [Inject]
        private void Construct(
            DesktopInput desktopInput,
            PlayerConfig playerConfig)
        {
            _desktopInput = desktopInput;
            _playerConfig = playerConfig;
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
                          _playerConfig.Speed *
                          Time.deltaTime;

            position.y += _desktopInput.Vertical *
                          _playerConfig.Speed *
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