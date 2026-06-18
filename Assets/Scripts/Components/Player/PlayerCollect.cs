using UnityEngine;
using Components.Ui.Pages;
using Components.Items;
using Components.Weapon;
using Zenject;
using Configs;

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
            GameObject triggerObject =
                other.transform.root.gameObject;

            if (triggerObject == _lastTriggerGo)
                return;

            _lastTriggerGo = triggerObject;

            int objectLayer = triggerObject.layer;

            if ((enemyLayer.value & (1 << objectLayer)) != 0)
            {
                // Игрок врезался во врага: воспроизводим звук урона
                _audioService?.PlayTakeDamage();

                ShieldLevel--;
                Destroy(triggerObject);
            }
            else if ((powerUpLayer.value & (1 << objectLayer)) != 0)
            {
                AbsorbPowerUp(triggerObject);
            }
        }

        public void AbsorbPowerUp(GameObject powerUpObject)
        {
            PowerUp powerUp = powerUpObject.GetComponent<PowerUp>();

            // Игрок подобрал бонус: воспроизводим звук подбора
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