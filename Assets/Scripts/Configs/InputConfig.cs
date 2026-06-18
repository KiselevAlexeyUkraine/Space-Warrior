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

        [Header("Передвижение")]
        [SerializeField] [Tooltip("Клавиша движения вперед")] private KeyCode forward = KeyCode.W;
        [SerializeField] [Tooltip("Клавиша движения назад")] private KeyCode backward = KeyCode.S;
        [SerializeField] [Tooltip("Клавиша движения влево")] private KeyCode left = KeyCode.A;
        [SerializeField] [Tooltip("Клавиша движения вправо")] private KeyCode right = KeyCode.D;

        public KeyCode Forward => forward;
        public KeyCode Backward => backward;
        public KeyCode Left => left;
        public KeyCode Right => right;
        
    }
}