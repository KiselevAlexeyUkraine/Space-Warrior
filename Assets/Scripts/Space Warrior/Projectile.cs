using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour 
{
    private BoundsCheck bndCheck;
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
        bndCheck = GetComponent<BoundsCheck>();
        rend = GetComponent<Renderer>();
        rigid = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (bndCheck.offUp) // Если снаряд вышел за верхнюю границу экрана, уничтожить его
        {
            Destroy(gameObject);
        }
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
