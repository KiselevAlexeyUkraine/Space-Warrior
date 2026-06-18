using UnityEngine;
using Components.Ui.Pages;
using Zenject;
using Configs;

public class PlayerCollect : MonoBehaviour
{
    private PlayerConfig _playerConfig;

    [Header("Weapons")]
    [SerializeField]
    private Weapon[] _weapons;

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

    public static PlayerCollect S;

    [Inject]
    private void Construct(
        PageSwitcher pageSwitcher,
        PlayerConfig playerConfig)
    {
        _pageSwitcher = pageSwitcher;
        _playerConfig = playerConfig;
    }

    private void Awake()
    {
        S = this;
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

        switch (powerUp.type)
        {
            case WeaponType.shield:
                ShieldLevel++;
                break;

            default:

                if (powerUp.type == _weapons[0].Type)
                {
                    Weapon emptyWeapon = GetEmptyWeaponSlot();

                    if (emptyWeapon != null)
                    {
                        emptyWeapon.SetType(powerUp.type);
                    }
                }
                else
                {
                    ClearWeapons();
                    _weapons[0].SetType(powerUp.type);
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

    private Weapon GetEmptyWeaponSlot()
    {
        foreach (Weapon weapon in _weapons)
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
        foreach (Weapon weapon in _weapons)
        {
            weapon.SetType(WeaponType.none);
        }
    }

    public Weapon[] Weapons => _weapons;
}