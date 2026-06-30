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

    // ДОБАВЛЕНО: Слой ракеты, настраиваемый в инспекторе
    [Tooltip("Выберите слой ракеты (Rocket). Объекты на этом слое НЕ будут деспавниться при попадании пули.")]
    [SerializeField] private LayerMask _rocketLayer;

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
        int objectLayer = otherGo.layer;

        // Проверяем, входит ли задетый слой в общую маску целей
        if (((1 << objectLayer) & targetLayerMask.value) != 0)
        {
            Debug.Log($"[Пуля] Столкновение с: {otherGo.name} на слое {LayerMask.LayerToName(objectLayer)}");

            // ИСПРАВЛЕНО: Проверяем, является ли этот объект ракетой (через маску из инспектора)
            if (((1 << objectLayer) & _rocketLayer.value) != 0)
            {
                Debug.Log($"[Пуля] Попадание в ракету. Объект {otherGo.name} игнорирует деспавн пули.");
            }
            else
            {
                // Для всех остальных слоев (например, обычных врагов) — удаляем цель
                SafeDespawn(otherGo);
            }
            
            // Саму пулю игрока убираем в любом случае
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