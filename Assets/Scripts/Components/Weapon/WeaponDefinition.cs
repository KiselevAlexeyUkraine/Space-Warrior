using UnityEngine;

namespace Components.Weapon
{
    [System.Serializable]
    public class WeaponDefinition
    {
        [Tooltip("Тип оружия.")]
        public WeaponType type = WeaponType.none;

        [Tooltip("Буква для отображения на бонусе.")]
        public string letter;

        [Tooltip("Цвет элементов оружия.")]
        public Color color = Color.white;

        [Tooltip("Префаб снаряда.")]
        public GameObject projectilePrefab;

        [Tooltip("Цвет снаряда.")]
        public Color projectileColor = Color.white;

        [Tooltip("Урон при попадании.")]
        public float damageOnHit = 0f;

        [Tooltip("Урон в секунду (для непрерывного оружия).")]
        public float continuousDamage = 0f;

        [Tooltip("Задержка между выстрелами.")]
        public float delayBetweenShots = 0f;

        [Tooltip("Скорость полета снаряда.")]
        public float velocity = 20f;
    }
}