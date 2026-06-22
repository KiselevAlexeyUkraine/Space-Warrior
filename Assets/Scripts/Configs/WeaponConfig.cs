using UnityEngine;
using SO;

namespace Components.Weapon
{
    [CreateAssetMenu(
        fileName = "WeaponConfig",
        menuName = "Configs/Weapon Config")]
    public sealed class WeaponConfig : ScriptableConfig
    {
        [Header("Weapon Info")]

        [Tooltip("Тип оружия из перечисления WeaponType.")]
        public WeaponType weaponType;

        [Tooltip("Буква или сокращение для отображения на бонусах и UI.")]
        public string letter;

        [Space]

        [Header("Visual Settings")]

        [Tooltip("Основной цвет оружия (например цвет турели или корпуса).")]
        public Color weaponColor = Color.white;

        [Tooltip("Цвет выпускаемого снаряда.")]
        public Color projectileColor = Color.white;

        [Space]

        [Header("Projectile Settings")]

        [Tooltip("Префаб снаряда, который будет создаваться при выстреле.")]
        public GameObject projectilePrefab;

        [Tooltip("Скорость полета снаряда.")]
        [Min(0f)]
        public float projectileSpeed = 20f;

        [Tooltip("Время жизни снаряда до автоматического возврата в пул.")]
        [Min(0.1f)]
        public float projectileLifeTime = 3f;

        [Space]

        [Header("Damage Settings")]

        [Tooltip("Урон, наносимый при попадании снаряда.")]
        [Min(0f)]
        public float damageOnHit = 1f;

        [Tooltip("Урон в секунду для лазеров и другого непрерывного оружия.")]
        [Min(0f)]
        public float continuousDamage = 0f;

        [Space]

        [Header("Fire Settings")]

        [Tooltip("Интервал между выстрелами в секундах. Чем меньше значение, тем выше скорострельность.")]
        [Min(0.01f)]
        public float fireRate = 0.25f;

        [Tooltip("Количество выпускаемых снарядов за один выстрел.")]
        [Min(1)]
        public int projectileCount = 1;

        [Tooltip("Угол разброса между снарядами для дробовиков и spread-оружия.")]
        [Range(0f, 180f)]
        public float spreadAngle = 10f;

        [Space]

        [Header("Advanced Settings")]

        [Tooltip("Использовать ли физику Rigidbody для движения снаряда.")]
        public bool usePhysicsProjectile = true;

        [Tooltip("Может ли снаряд проходить сквозь несколько целей.")]
        public bool canPierceTargets;

        [Tooltip("Максимальное количество целей, которые может пробить снаряд.")]
        [Min(1)]
        public int maxPierceTargets = 1;

        [Tooltip("Наносит ли снаряд урон по площади.")]
        public bool useExplosion;

        [Tooltip("Радиус взрыва для ракет и гранат.")]
        [Min(0f)]
        public float explosionRadius = 0f;

        [Tooltip("Урон от взрыва по площади.")]
        [Min(0f)]
        public float explosionDamage = 0f;

        [Tooltip("Самонаведение на цель.")]
        public bool useHoming;

        [Tooltip("Скорость поворота самонаводящегося снаряда.")]
        [Min(0f)]
        public float homingTurnSpeed = 5f;
    }
}