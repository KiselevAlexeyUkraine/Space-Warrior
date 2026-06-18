using UnityEngine;
using Components.Weapon;

namespace Components.Items
{
    public sealed class PowerUp : MonoBehaviour
{
    [Header("Set in Inspector")]
    [Tooltip("Минимальная и максимальная скорость вращения по осям X, Y, Z.")]
    [SerializeField] private Vector2 rotMinMax = new Vector2(15, 90);

    [Tooltip("Минимальная и максимальная скорость линейного движения (дрейфа).")]
    [SerializeField] private Vector2 driftMinMax = new Vector2(0.25f, 2f);

    [Tooltip("Время жизни бонуса до начала исчезновения (в секундах).")]
    [SerializeField] private float lifeTime = 6f;

    [Tooltip("Продолжительность эффекта исчезновения (в секундах).")]
    [SerializeField] private float fadeTime = 4f;

    [Header("Set Dynamically")]
    [Tooltip("Текущий тип оружия, который дает этот бонус.")]
    [SerializeField] private WeaponType type;
    
    public WeaponType Type => type;

    [Tooltip("Ссылка на дочерний объект Cube.")]
    [SerializeField] private GameObject cube;

    [Tooltip("Ссылка на компонент TextMesh для отображения буквы бонуса.")]
    [SerializeField] private TextMesh letter;

    [Tooltip("Скорость вращения куба в секунду по трем осям.")]
    [SerializeField] private Vector3 rotPerSecond;

    [Tooltip("Время появления (рождения) бонуса.")]
    [SerializeField] private float birthTime;

    private Renderer cubeRend;
    private Vector3 movementVelocity; 

    private void Awake()
    {
        // Инициализация компонентов
        cube = transform.Find("Cube").gameObject;
        letter = GetComponent<TextMesh>();
        cubeRend = cube.GetComponent<Renderer>();

        // Направление движения на плоскости XY
        Vector3 vel = Random.onUnitSphere; 
        vel.z = 0;      
        vel.Normalize();
        
        // Скорость дрейфа
        vel *= Random.Range(driftMinMax.x, driftMinMax.y);
        movementVelocity = vel;

        // Сброс глобального поворота
        transform.rotation = Quaternion.identity;

        // Рандомное вращение для визуального куба
        rotPerSecond = new Vector3(
            Random.Range(rotMinMax.x, rotMinMax.y),
            Random.Range(rotMinMax.x, rotMinMax.y),
            Random.Range(rotMinMax.x, rotMinMax.y)
        );

        birthTime = Time.time;
    }

    private void Update()
    {
        // Движение без Rigidbody
        transform.Translate(movementVelocity * Time.deltaTime, Space.World);

        // Вращение куба
        cube.transform.rotation = Quaternion.Euler(rotPerSecond * Time.time);

        // Логика плавного исчезновения
        float u = (Time.time - (birthTime + lifeTime)) / fadeTime;

        if (u >= 1)
        {
            Destroy(gameObject);
            return;
        }

        if (u > 0)
        {
            // Прозрачность куба
            Color c = cubeRend.material.color;
            c.a = 1f - u;
            cubeRend.material.color = c;

            // Прозрачность текста
            c = letter.color;
            c.a = 1f - (u * 0.5f);
            letter.color = c;
        }
    }

    public void SetType(WeaponType wt)
    {
        WeaponDefinition def = Main.GetWeaponDefinition(wt);
        
        cubeRend.material.color = def.color;
        letter.text = def.letter;
        type = wt;
    }

    public void AbsorbedBy(GameObject target)
    {
        Destroy(gameObject);
    }
}
}
