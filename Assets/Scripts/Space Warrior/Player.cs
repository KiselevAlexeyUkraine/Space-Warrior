using UnityEngine;
using Zenject;
using Services;
using Configs;

public class Player : MonoBehaviour
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
        float horizontal = _desktopInput.Horizontal;
        float vertical = _desktopInput.Vertical;

        Vector3 position = transform.position;

        position.x += horizontal * _playerConfig.Speed * Time.deltaTime;
        position.y += vertical * _playerConfig.Speed * Time.deltaTime;

        transform.position = position;
    }

    private void Rotate()
    {
        float horizontal = _desktopInput.Horizontal;
        float vertical = _desktopInput.Vertical;

        transform.rotation = Quaternion.Euler(
            vertical * _playerConfig.PitchMult,
            horizontal * _playerConfig.RollMult,
            0f);
    }
}