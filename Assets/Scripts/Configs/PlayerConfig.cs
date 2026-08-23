using System;
using System.Collections.Generic;
using UnityEngine;
using Components.Weapon;
using Components.Enemy;
using SO;

namespace Configs
{
    [Serializable]
    public struct ResourceCost
    {
        public ResourceType resourceType;
        public int amount;
    }

    [Serializable]
    public struct LevelUpgradeRequirement
    {
        [Tooltip("Уровень, ДО которого происходит улучшение (например, 2)")]
        public int targetLevel;
        [Tooltip("Стоимость в очках/деньгах")]
        public int scoreCost;
        [Tooltip("Список необходимых ресурсов")]
        public List<ResourceCost> requiredResources;
    }

    [CreateAssetMenu(menuName = "Configs/Player Config")]
    public class PlayerConfig : ScriptableConfig
    {
        [Header("Movement")]
        [SerializeField] private float _speed = 30f;
        [SerializeField] private float _rollMult = -45f;
        [SerializeField] private float _pitchMult = 30f;

        [Header("Gameplay")]
        [SerializeField] private float _gameRestartDelay = 2f;

        [Header("Projectile")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private float _projectileSpeed = 40f;

        [Header("Shield")]
        [SerializeField] private float _maxShieldLevel = 4f;

        [Header("Weapon")]
        [SerializeField] private WeaponType _startWeapon = WeaponType.blaster;

        [Header("Upgrades Balance (Требования для улучшения)")]
        [SerializeField] private List<LevelUpgradeRequirement> _upgradeRequirements = new List<LevelUpgradeRequirement>();

        public float Speed => _speed;
        public float RollMult => _rollMult;
        public float PitchMult => _pitchMult;
        public float GameRestartDelay => _gameRestartDelay;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float MaxShieldLevel => _maxShieldLevel;
        public WeaponType StartWeapon => _startWeapon;

        public bool TryGetUpgradeRequirement(int targetLevel, out LevelUpgradeRequirement req)
        {
            if (_upgradeRequirements != null)
            {
                foreach (var r in _upgradeRequirements)
                {
                    if (r.targetLevel == targetLevel)
                    {
                        req = r;
                        return true;
                    }
                }
            }

            req = default;
            return false;
        }
    }
}