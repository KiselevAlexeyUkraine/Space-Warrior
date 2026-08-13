using Components.Weapon;
using SO;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player Config")]
    public class PlayerConfig : ScriptableConfig
    {
        [Header("Movement & Animation")]
        [SerializeField]
        [Tooltip("Множитель крена корабля по оси Z (Roll)")]
        private float _rollMult = -45f;

        [SerializeField]
        [Tooltip("Множитель наклона корабля по оси X (Pitch)")]
        private float _pitchMult = 30f;

        [SerializeField]
        [Tooltip("Скорость плавности наклона/поворота корабля (чем выше, тем быстрее наклон)")]
        private float _rotationSpeed = 10f;

        [Header("Gameplay")]
        [SerializeField]
        [Tooltip("Задержка перед перезапуском уровня")]
        private float _gameRestartDelay = 2f;

        [Header("Projectile")]
        [SerializeField]
        [Tooltip("Префаб стандартного снаряда")]
        private GameObject _projectilePrefab;

        [SerializeField]
        [Tooltip("Скорость полёта снаряда")]
        private float _projectileSpeed = 40f;

        [Header("Shield")]
        [SerializeField]
        [Tooltip("Максимальное количество щита")]
        private float _maxShieldLevel = 4f;

        [Header("Weapon")]
        [SerializeField]
        [Tooltip("Оружие, которое игрок получает при старте")]
        private WeaponType _startWeapon = WeaponType.blaster;

        public float RollMult => _rollMult;
        public float PitchMult => _pitchMult;
        public float RotationSpeed => _rotationSpeed;

        public float GameRestartDelay => _gameRestartDelay;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float MaxShieldLevel => _maxShieldLevel;
        public WeaponType StartWeapon => _startWeapon;
    }
}