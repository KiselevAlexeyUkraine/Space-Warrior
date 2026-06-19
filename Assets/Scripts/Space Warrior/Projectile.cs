using UnityEngine;
using Components.Weapon;

public sealed class Projectile : MonoBehaviour 
{

    private Renderer rend;

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
    

    ///<summary>
    ///Изменяет скрытое поле _Type и устанавливает цвет этого снаряда, как определено в WeaponDefinition.
    /// </summary>
    /// <param name="eType"> Тип WeaponType используеммого оружия</param>
    public void SetType(WeaponType eType)
    {
        // Установить _type
        _type = eType;
        WeaponDefinition def = Main.GetWeaponDefinition(_type);
        rend.material.color = def.projectileColor;
    }
}
