using UnityEngine;
using Components.Weapon;
using Level;
using Lean.Pool;

public sealed class Projectile : MonoBehaviour 
{
    private Renderer rend;

    [Header("Collision Settings")]
    [Tooltip("Слой (или слои), при столкновении с которыми пуля уничтожит цель и деспавнится сама.")]
    [SerializeField] private LayerMask targetLayerMask;

    [Header("Set Dynamically")]
    public Rigidbody rigid;
    [SerializeField] private WeaponType _type;

    // Это общедоступное свойство маскирует поле _type и обрабатывает операции присваивания ему нового значения
    public WeaponType type
    {
        get
        {
            return (_type);
        }
        set
        {
            SetType(value);
        }
    }

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rigid = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Метод обработки физического триггера.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        GameObject otherGo = other.gameObject;

        // Проверяем, входит ли слой столкнувшегося объекта в маску targetLayerMask
        if (((1 << otherGo.layer) & targetLayerMask) != 0)
        {
            Debug.Log($"[Пуля] Попадание во враждебный слой: {otherGo.name}");

            // Отправляем объект, в который попали, в пул (или уничтожаем)
            SafeDespawn(otherGo);

            // Отправляем саму пулю обратно в пул
            SafeDespawn(gameObject);
        }
    }

    /// <summary>
    /// Безопасный возврат объекта в пул LeanPool или обычное уничтожение, если объекта нет в пуле.
    /// </summary>
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

    ///<summary>
    ///Изменяет скрытое поле _Type и устанавливает цвет этого снаряда, как определено в WeaponDefinition.
    /// </summary>
    /// <param name="eType"> Тип WeaponType используеммого оружия</param>
    public void SetType(WeaponType eType)
    {
        _type = eType;
        WeaponDefinition def = Main.GetWeaponDefinition(_type);
        
        if (rend != null && def != null)
        {
            rend.material.color = def.projectileColor;
        }
    }
}