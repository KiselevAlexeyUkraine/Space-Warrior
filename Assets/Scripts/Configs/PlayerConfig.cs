using UnityEngine;
using Components.Weapon;
using SO;

namespace Configs
{
    [CreateAssetMenu(menuName = "Configs/Player Config")]
    public class PlayerConfig : ScriptableConfig
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Скорость перемещения игрока")]
        private float _speed = 30f;

        [SerializeField]
        [Tooltip("Множитель крена корабля по оси Z")]
        private float _rollMult = -45f;

        [SerializeField]
        [Tooltip("Множитель наклона корабля по оси X")]
        private float _pitchMult = 30f;

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
        [Tooltip("Начальное количество щита")]
        private float _startShieldLevel = 1f;

        [SerializeField]
        [Tooltip("Максимальное количество щита")]
        private float _maxShieldLevel = 4f;

        [Header("Weapon")]

        [SerializeField]
        [Tooltip("Оружие, которое игрок получает при старте")]
        private WeaponType _startWeapon = WeaponType.blaster;

        public float Speed => _speed;

        public float RollMult => _rollMult;

        public float PitchMult => _pitchMult;

        public float GameRestartDelay => _gameRestartDelay;

        public GameObject ProjectilePrefab => _projectilePrefab;

        public float ProjectileSpeed => _projectileSpeed;

        public float StartShieldLevel => _startShieldLevel;

        public float MaxShieldLevel => _maxShieldLevel;

        public WeaponType StartWeapon => _startWeapon;
    }
}