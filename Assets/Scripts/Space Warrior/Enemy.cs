using UnityEngine; 

public class Enemy : MonoBehaviour
{
    [Header("Параметры врага")]
    [SerializeField] private float speed = 10f; // Скорость в м/с
    [SerializeField] private float fireRate = 0.3f; // Секунд между выстрелами (не используется)
    [SerializeField] private float health = 10f;
    [SerializeField] private int score = 100; // Очки за уничтожение этого корабля
    public float showDamageDuration = 0.1f; // Длительность эффекта попадания в секундах
    public float powerUpDropChance = 1f; 

    [Header("Set Dynamically: Enemy")]
    [SerializeField] private Color[] originalColors;
    [SerializeField] private Material[] materials; // Все материалы игрового объекта и его потомков
    public bool showingDamage = false;
    public float damageDoneTime; // Время прекращения отображения эффекта
    [SerializeField] private bool notifiedOfDestruction = false; // Будет использовано позже

    public BoundsCheck bndCheck;

    private void Awake()
    {
        bndCheck = GetComponent<BoundsCheck>();
        // Получить материалы и цвет этого игрового объекта и его потомков
        materials = Utiils.GetAllMaterials(gameObject);
        originalColors = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            originalColors[i] = materials[i].color;
        }
    }

    // Это свойство: метод, действующий как поле
    public Vector3 pos
    {
        get
        {
            return (this.transform.position);
        }
        set
        {
            this.transform.position = value;
        }
    }

    private void Update()
    {
        Move();

        if (showingDamage && Time.time > damageDoneTime)
        {
            UnShowDamage();
        }
        
        if (bndCheck != null && bndCheck.offDown)
        {
            // Корабль за нижней границей, поэтому его нужно уничтожить
            Destroy(gameObject);
        }
    }

    public virtual void Move()
    {
        Vector3 tempPos = pos;
        tempPos.y -= speed * Time.deltaTime;
        pos = tempPos;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject otherGo = collision.gameObject; // Получить игровой объект которому принадлежит коллайдер, столкнувшийся с вражеским кораблем
        switch (otherGo.tag)
        {
            case "ProjectileHero":
                Projectile p = otherGo.GetComponent<Projectile>();

                // Если вражеский корабль за границами экрана, не наносить ему повреждений.
                if (!bndCheck.isOnScreen)
                {
                    Destroy(otherGo);
                    break;
                }

                // Поразить вражеский корабль
                ShowDamage();
                // Получить разрущающую силу из WEAP_DICT в клссе Main.
                health -= Main.GetWeaponDefinition(p.type).damageOnHit;
                if (health <= 0)
                {
                    // Сообщить объекту-одиночке Main об уничтожении
                    if (!notifiedOfDestruction)
                    {
                        Main.S.ShipDestroyed(this);
                    }
                    notifiedOfDestruction = true;
                    // Уничтожить этот вражеский корабль
                    Destroy(this.gameObject);
                }
                Destroy(otherGo);
                break;

            default:
                print("Enemy hit by non-ProjectileHero: " + otherGo.name);
                break;
        }
        
    }
    private void ShowDamage()
    {
        foreach (Material m in materials)
        {
            m.color = Color.red;
        }
        showingDamage = true;
        damageDoneTime = Time.time + showDamageDuration;
    }

    private void UnShowDamage()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].color = originalColors[i];
        }
        showingDamage = false;
    }
}
