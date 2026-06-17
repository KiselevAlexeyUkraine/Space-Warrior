using UnityEngine;
using SO;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player Config")]
    public class PlayerConfig : ScriptableConfig
    {
        [Header("Movement")]
        public float WalkSpeed = 3f;
        public float RunSpeed = 5f;
        public float SprintSpeed = 8f;
        public float AccelerationTime = 0.15f;
        public float RotationSpeed = 12f;

        [Header("Jump")]
        public float JumpForce = 8f;
        public float Gravity = -20f;

        [Header("Idle")]
        public float IdleGreetingDelay = 5f;
        
        [Header("Stamina Balance")]
        public int MaxStamina = 100;
        public int SprintConsumePerSecond = 10;
        public int RegenPerSecond = 8;
        public float RegenDelay = 1.5f;
        public float KnockdownDuration = 3f;

        [Header("Health")]
        public int MaxHealth = 3;
        
        public float SprintThreshold = 0.9f;
    }
}