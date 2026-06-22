using UnityEngine;
using SO;

namespace Configs
{
    [CreateAssetMenu(
        menuName = "Configs/InputConfig",
        fileName = "InputConfig"
    )]
    public class InputConfig : ScriptableConfig
    {
        [Header("Системные")]
        [SerializeField] [Tooltip("Клавиша для вызова меню / выхода")] private KeyCode escape = KeyCode.Escape;
        public KeyCode Escape => escape;

        [Header("Передвижение (Основное - WASD)")]
        [SerializeField] private KeyCode forward = KeyCode.W;
        [SerializeField] private KeyCode backward = KeyCode.S;
        [SerializeField] private KeyCode left = KeyCode.A;
        [SerializeField] private KeyCode right = KeyCode.D;

        [Header("Передвижение (Альтернативное - Стрелочки)")]
        [SerializeField] private KeyCode arrowUp = KeyCode.UpArrow;
        [SerializeField] private KeyCode arrowDown = KeyCode.DownArrow;
        [SerializeField] private KeyCode arrowLeft = KeyCode.LeftArrow;
        [SerializeField] private KeyCode arrowRight = KeyCode.RightArrow;

        public KeyCode Forward => forward;
        public KeyCode Backward => backward;
        public KeyCode Left => left;
        public KeyCode Right => right;

        // Геттеры для стрелочек
        public KeyCode ArrowUp => arrowUp;
        public KeyCode ArrowDown => arrowDown;
        public KeyCode ArrowLeft => arrowLeft;
        public KeyCode ArrowRight => arrowRight;
    }
}