using UnityEngine;

public class Hero : MonoBehaviour
{
    static public Hero S; //         

    [Header("     ,                              ")]
    //     ,                              
    public float            speed = 30f;
    public float            rollMult = -45f;
    public float            pitchMult = 30f;
    public float            gameRestartDelay = 2f;
    public GameObject       projectilePrefab;
    public float            projectileSpeed = 40f;
    public Weapon[]         weapons;

    [Header("Set Dynamically")]
    [SerializeField] private float _shieldLevel = 1;

    //                                                                       
    private GameObject lastTriggerGo = null;

    //                                 WeaponfireDelegate
    public delegate void WeaponFireDelegate();
    //                   WeaponFireDelegate c        fireDelegate.
    public WeaponFireDelegate fireDelegate;

    void Start()
    {
        S = this; //                             
        //                 weapons                  1          
        ClearWeapons();
        weapons[0].SetType(WeaponType.blaster);
    }


    void Update()
    {
        //                              Input
        float xAxis = Input.GetAxis("Horizontal");
        float yAxis = Input.GetAxis("Vertical");

        //          transform/position,                               
        Vector3 pos = transform.position;
        pos.x += xAxis * speed * Time.deltaTime;
        pos.y += yAxis * speed * Time.deltaTime;
        transform.position = pos;

        //                  ,                                 
        transform.rotation = Quaternion.Euler(yAxis * pitchMult, xAxis * rollMult, 0);


        //                                                fireDelegate
        //                                 : Axis("Jump")
        //                ,              fireDelegate          null,                      
        if(Input.GetAxis("Jump") == 1 && fireDelegate != null)
        {
            fireDelegate();
        }
    }

    void TempFire()
    {
        GameObject projGo = Instantiate<GameObject>(projectilePrefab);
        projGo.transform.position = transform.position;
        Rigidbody rigidB = projGo.GetComponent<Rigidbody>();

        Projectile proj = projGo.GetComponent<Projectile>();
        proj.type = WeaponType.blaster;
        float tSpeed = Main.GetWeaponDefinition(proj.type).velocity;
        rigidB.linearVelocity = Vector3.up * tSpeed;
    }
    void OnTriggerEnter(Collider other)
    {
        Transform rootT = other.gameObject.transform.root;
        GameObject go = rootT.gameObject;
        
        //print("Triggered: " + go.name);

        //                                                                      
        if (go == lastTriggerGo)
        {
            return;
        }
        lastTriggerGo = go;

        if (go.tag == "Enemy") //                                                     
        {
            shieldLevel--; //                             1
            Destroy(go); // ...                   
        }
        else if (go.tag == "PowerUp")
        {
            //                                         
            AbsorbPowerUp(go);
        }
        else
        {
            print("Triggered by non-Enemy: " + go.name);
        }
    }

    public void AbsorbPowerUp(GameObject go)
    {
        PowerUp pu = go.GetComponent<PowerUp>();
        switch (pu.type)
        {
            case WeaponType.shield:
                shieldLevel++;
                break;

            default:
                if (pu.type == weapons[0].type) //                         
                {
                    Weapon w = GetEmptyweaponSlot();
                    if (w != null)
                    {
                        //              pu.type
                        w.SetType(pu.type);
                    }
                }
                else //                         
                {
                    ClearWeapons();
                    weapons[0].SetType(pu.type);
                }
                break;      
        }
        pu.AbsorbedBy(this.gameObject);
    }
    public float shieldLevel
    {
        get
        {
            return (_shieldLevel);
        }
        set
        {
            _shieldLevel = Mathf.Min(value, 4);
            //                                        
            if (value < 0)
            {
                Destroy(this.gameObject);
                //                  Main.S                                    
                Main.S.DelayedRestart(gameRestartDelay);
            }
        }
    }
    
    Weapon GetEmptyweaponSlot()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == WeaponType.none)
            {
                return (weapons[i]);
            }
        }
        return (null);
    }

    void ClearWeapons()
    {
        foreach(Weapon w in weapons)
        {
            w.SetType(WeaponType.none);
        }
    }

}
