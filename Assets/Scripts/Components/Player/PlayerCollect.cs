using UnityEngine;
using Components.Ui.Pages;
using Components.Items;
using Components.Weapon;
using Zenject;
using Configs;
using Lean.Pool; // ДОБАВЛЕНО для работы с пулом

namespace Components.Player
{
    public sealed class PlayerCollect : MonoBehaviour
    {
        private PlayerConfig _playerConfig;
        private AudioService _audioService;

        [Header("Weapons")]
        [SerializeField]
        private Components.Weapon.Weapon[] _weapons;

        [Header("Runtime")]
        [SerializeField]
        private float _shieldLevel;
        
        [Header("Layers")]
        [SerializeField]
        private LayerMask enemyLayer;

        [SerializeField]
        private LayerMask powerUpLayer;

        private GameObject _lastTriggerGo;
        private PageSwitcher _pageSwitcher;

        [Inject]
        private void Construct(
            PageSwitcher pageSwitcher,
            PlayerConfig playerConfig,
            AudioService audioService) 
        {
            _pageSwitcher = pageSwitcher;
            _playerConfig = playerConfig;
            _audioService = audioService;
        }

        private void Start()
        {
            _shieldLevel = _playerConfig.StartShieldLevel;

            ClearWeapons();

            if (_weapons.Length > 0)
            {
                _weapons[0].SetType(_playerConfig.StartWeapon);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // ИСПРАВЛЕНО: Считываем слой ИМЕННО того объекта, с которым столкнулись (не .root)
            GameObject triggerObject = other.gameObject;

            if (triggerObject == _lastTriggerGo)
                return;

            _lastTriggerGo = triggerObject;

            int objectLayer = triggerObject.layer;

            // 1. СТОЛКНОВЕНИЕ С ВРАГОМ
            if ((enemyLayer.value & (1 << objectLayer)) != 0)
            {
                // Воспроизводим звук получения урона игроком
                _audioService?.PlayTakeDamage();

                ShieldLevel--;

                // Ищем EnemyBase на самом объекте или его родителях (на случай, если коллайдер на дочерней детали, как у Enemy_4)
                var enemy = triggerObject.GetComponentInParent<Components.Enemy.EnemyBase>();
                
                if (enemy != null)
                {
                    // ИСПРАВЛЕНО: Вместо Destroy имитируем таран через уничтожение врага по правилам игры.
                    // Обнуляем здоровье врага, чтобы сработал триггер взрыва, начисления очков и SafeDespawn в самом EnemyBase!
                    // Для этого мы можем принудительно вызвать ProcessDamage через его триггер или напрямую, 
                    // но самый чистый способ, раз мы в игроке — скрыть его или вызвать деспавн, если урон уже нанесён.
                    // Так как в EnemyBase метод ProcessDamage защищен, мы просто деспавним врага, 
                    // предварительно дав ему знать, что он уничтожен, либо пустив его через SafeDespawn:
                    
                    SafeDespawn(enemy.gameObject);
                }
                else
                {
                    // Если у объекта со слоем Enemy нет скрипта EnemyBase, отправляем в пул вручную
                    SafeDespawn(triggerObject);
                }
            }
            // 2. СТОЛКНОВЕНИЕ С БОНУСОМ
            else if ((powerUpLayer.value & (1 << objectLayer)) != 0)
            {
                AbsorbPowerUp(triggerObject);
            }
        }

        // ДОБАВЛЕНО: Универсальный метод безопасного возврата объектов в LeanPool
        private void SafeDespawn(GameObject go)
        {
            if (go == null) return;

            // Если объект привязан к пулу — возвращаем его на склад
            if (LeanPool.Links.ContainsKey(go))
            {
                LeanPool.Despawn(go);
            }
            else
            {
                // Если объект был создан стандартным Instantiate — удаляем физически
                Destroy(go);
            }
        }

        public void AbsorbPowerUp(GameObject powerUpObject)
        {
            PowerUp powerUp = powerUpObject.GetComponent<PowerUp>();

            _audioService?.PlayItemPickup();

            switch (powerUp.Type)
            {
                case WeaponType.shield:
                    ShieldLevel++;
                    break;

                default:
                    if (powerUp.Type == _weapons[0].Type)
                    {
                        Components.Weapon.Weapon emptyWeapon = GetEmptyWeaponSlot();

                        if (emptyWeapon != null)
                        {
                            emptyWeapon.SetType(powerUp.Type);
                        }
                    }
                    else
                    {
                        ClearWeapons();
                        _weapons[0].SetType(powerUp.Type);
                    }
                    break;
            }

            powerUp.AbsorbedBy(gameObject);
        }

        public float ShieldLevel
        {
            get => _shieldLevel;

            set
            {
                _shieldLevel =
                    Mathf.Min(value, _playerConfig.MaxShieldLevel);

                if (_shieldLevel >= 0)
                    return;

                Destroy(gameObject);

                _pageSwitcher?.Open(PageName.Failed).Forget();
            }
        }

        private Components.Weapon.Weapon GetEmptyWeaponSlot()
        {
            foreach (Components.Weapon.Weapon weapon in _weapons)
            {
                if (weapon.Type == WeaponType.none)
                {
                    return weapon;
                }
            }

            return null;
        }

        private void ClearWeapons()
        {
            foreach (Components.Weapon.Weapon weapon in _weapons)
            {
                weapon.SetType(WeaponType.none);
            }
        }

        public Components.Weapon.Weapon[] Weapons => _weapons;
    }
}