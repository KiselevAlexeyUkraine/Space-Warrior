using UnityEngine;
using Unity.Cinemachine; // Если у вас Cinemachine v3.хх, или Cinemachine (для Cinemachine v2.x)

namespace Components.Player
{
    public sealed class PlayerComponent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _camera;

        [Tooltip("Скрипт BoundsCheck, который ограничивает передвижение игрока на экране.")]
        [SerializeField] private BoundsCheck _boundsCheck;

        [Tooltip("Cinemachine камера для анимации победы/взлета.")]
        [SerializeField] private CinemachineCamera _winCinemachineCamera; // Или CinemachineVirtualCamera для старых версий Cinemachine

        public BoundsCheck BoundsCheck => _boundsCheck;
        public CinemachineCamera WinCinemachineCamera => _winCinemachineCamera;
    }
}