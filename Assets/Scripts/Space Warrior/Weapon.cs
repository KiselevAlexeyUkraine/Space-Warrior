using UnityEngine;

public enum WeaponType
{
    none,
    blaster,
    spread,
    phaser,
    missile,
    laser,
    shield
}

[System.Serializable]
public class WeaponDefinition
{
    public WeaponType type = WeaponType.none;

    public string letter;

    public Color color = Color.white;

    public GameObject projectilePrefab;

    public Color projectileColor = Color.white;

    public float damageOnHit = 0f;

    public float continuousDamage = 0f;

    public float delayBetweenShots = 0f;

    public float velocity = 20f;
}

public class Weapon : MonoBehaviour
{
    public static Transform PROJECTILE_ANCHOR;

    [Header("Weapon")]
    [SerializeField]
    private WeaponType _type = WeaponType.none;

    [SerializeField]
    private WeaponDefinition def;

    [Header("References")]
    [SerializeField]
    private GameObject collar;

    private Renderer collarRenderer;

    private float lastShotTime;

    private void Awake()
    {
        if (collar == null)
        {
            Transform collarTransform =
                transform.Find("Collar");

            if (collarTransform != null)
            {
                collar = collarTransform.gameObject;
            }
        }

        if (collar != null)
        {
            collarRenderer =
                collar.GetComponent<Renderer>();
        }

        if (PROJECTILE_ANCHOR == null)
        {
            GameObject anchor =
                new GameObject("_ProjectileAnchor");

            PROJECTILE_ANCHOR = anchor.transform;
        }
    }

    private void Start()
    {
        SetType(_type);
    }

    private void Update()
    {
        Fire();
    }

    public WeaponType Type => _type;

    public void SetType(WeaponType weaponType)
    {
        _type = weaponType;

        if (_type == WeaponType.none)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        def = Main.GetWeaponDefinition(_type);

        if (collarRenderer != null)
        {
            collarRenderer.material.color = def.color;
        }

        lastShotTime = 0f;
    }

    private void Fire()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (def == null)
            return;

        if (Time.time - lastShotTime < def.delayBetweenShots)
            return;

        Vector3 velocity =
            Vector3.up * def.velocity;

        if (transform.up.y < 0)
        {
            velocity.y *= -1f;
        }

        switch (_type)
        {
            case WeaponType.blaster:
            {
                Projectile projectile =
                    MakeProjectile();

                projectile.rigid.linearVelocity =
                    velocity;

                break;
            }

            case WeaponType.spread:
            {
                Projectile projectile =
                    MakeProjectile();

                projectile.rigid.linearVelocity =
                    velocity;

                projectile =
                    MakeProjectile();

                projectile.transform.rotation =
                    Quaternion.AngleAxis(
                        10f,
                        Vector3.back);

                projectile.rigid.linearVelocity =
                    projectile.transform.rotation *
                    velocity;

                projectile =
                    MakeProjectile();

                projectile.transform.rotation =
                    Quaternion.AngleAxis(
                        -10f,
                        Vector3.back);

                projectile.rigid.linearVelocity =
                    projectile.transform.rotation *
                    velocity;

                break;
            }
        }

        lastShotTime = Time.time;
    }

    private Projectile MakeProjectile()
    {
        if (def.projectilePrefab == null)
            return null;

        GameObject projectileObject =
            Instantiate(def.projectilePrefab);

        if (transform.root.CompareTag("Hero"))
        {
            projectileObject.tag =
                "ProjectileHero";

            projectileObject.layer =
                LayerMask.NameToLayer(
                    "ProjectileHero");
        }


        projectileObject.transform.position =
            collar.transform.position;

        projectileObject.transform.SetParent(
            PROJECTILE_ANCHOR,
            true);

        Projectile projectile =
            projectileObject.GetComponent<Projectile>();

        projectile.type = _type;

        return projectile;
    }

    public WeaponDefinition Definition => def;
}