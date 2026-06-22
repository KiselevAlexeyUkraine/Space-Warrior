using UnityEngine;
using Components.Weapon;
using Level;
using Lean.Pool;
using Zenject;

public sealed class Projectile : MonoBehaviour 
{
    private Renderer rend;
    private Main _main; 

    [Header("Collision Settings")]
    [Tooltip("Слой (или слои), при столкновении с которыми пуля деспавнит цель и себя.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Header("Set Dynamically")]
    public Rigidbody rigid;
    [SerializeField] private WeaponType _type;

    public WeaponType type
    {
        get => _type;
        set => SetType(value);
    }

    [Inject]
    private void Construct(Main main)
    {
        _main = main;
    }

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rigid = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject otherGo = other.gameObject;

        if (((1 << otherGo.layer) & targetLayerMask) != 0)
        {
            Debug.Log($"[Пуля] Столкновение с: {otherGo.name} на слое {LayerMask.LayerToName(otherGo.layer)}");

            // ИСПРАВЛЕНО: Просто деспавним объект, с которым столкнулись, без проверок на скрипты
            SafeDespawn(otherGo);
            
            // Деспавним саму пулю
            SafeDespawn(gameObject);
        }
    }

    private void SafeDespawn(GameObject go)
    {
        if (go == null) return;

        if (LeanPool.Links.ContainsKey(go))
        {
            LeanPool.Despawn(go);
        }
        else
        {
            Destroy(go);
        }
    }

    public void SetType(WeaponType eType)
    {
        _type = eType;

        if (_main == null)
        {
            _main = FindFirstObjectByType<Main>();
        }

        if (_main == null) return;

        WeaponConfig config = _main.GetWeaponConfig(_type);

        if (config == null) return;

        if (rend != null)
        {
            rend.material.color = config.projectileColor;
        }
    }
}