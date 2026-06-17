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
        // =========================
        // Системные
        // =========================
        [Header("Системные")]
        [SerializeField] private KeyCode escape = KeyCode.Escape;
        public KeyCode Escape => escape;

        // =========================
        // Передвижение
        // =========================
        [Header("Передвижение")]
        [SerializeField] private KeyCode forward = KeyCode.W;
        [SerializeField] private KeyCode backward = KeyCode.S;
        [SerializeField] private KeyCode left = KeyCode.A;
        [SerializeField] private KeyCode right = KeyCode.D;

        public KeyCode Forward => forward;
        public KeyCode Backward => backward;
        public KeyCode Left => left;
        public KeyCode Right => right;

        // =========================
        // Действия
        // =========================
        [Header("Действия")]
        [SerializeField] private KeyCode jump = KeyCode.Space;
        [SerializeField] private KeyCode attack = KeyCode.Mouse0;
        [SerializeField] private KeyCode interact = KeyCode.E;

        public KeyCode Jump => jump;
        public KeyCode Attack => attack;
        public KeyCode Interact => interact;

        // =========================
        // Спринт
        // =========================
        [Header("Спринт")]
        [SerializeField] private KeyCode sprint = KeyCode.LeftShift;
        public KeyCode Sprint => sprint;
    }
}