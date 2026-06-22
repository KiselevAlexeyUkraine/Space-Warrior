using UnityEngine;
using Components.Weapon;
using Level;
using Lean.Pool;
using Zenject;

namespace Components.Items
{
    public sealed class PowerUp : MonoBehaviour
    {
        [Header("Set Inspector")]
        [SerializeField] private Vector2 rotMinMax = new Vector2(15, 90);
        [SerializeField] private Vector2 driftMinMax = new Vector2(0.25f, 2f);
        [SerializeField] private float lifeTime = 6f;
        [SerializeField] private float fadeTime = 4f;

        [Header("Set Dynamically")]
        [SerializeField] private WeaponType type;
        
        public WeaponType Type => type;

        [SerializeField] private GameObject cube;
        [SerializeField] private TextMesh letter;
        [SerializeField] private Vector3 rotPerSecond;
        [SerializeField] private float birthTime;

        private Renderer cubeRend;
        private Vector3 movementVelocity; 
        private Main _main;

        // Внедряем Main через Zenject
        [Inject]
        private void Construct(Main main)
        {
            _main = main;
        }

        private void Awake()
        {
            cube = transform.Find("Cube").gameObject;
            letter = GetComponent<TextMesh>();
            cubeRend = cube.GetComponent<Renderer>();

            Vector3 vel = Random.onUnitSphere; 
            vel.z = 0;      
            vel.Normalize();
            
            vel *= Random.Range(driftMinMax.x, driftMinMax.y);
            movementVelocity = vel;

            transform.rotation = Quaternion.identity;

            rotPerSecond = new Vector3(
                Random.Range(rotMinMax.x, rotMinMax.y),
                Random.Range(rotMinMax.x, rotMinMax.y),
                Random.Range(rotMinMax.x, rotMinMax.y)
            );

            birthTime = Time.time;
        }

        private void Update()
        {
            transform.Translate(movementVelocity * Time.deltaTime, Space.World);
            cube.transform.rotation = Quaternion.Euler(rotPerSecond * Time.time);

            float u = (Time.time - (birthTime + lifeTime)) / fadeTime;

            if (u >= 1)
            {
                SafeDespawn(gameObject);
                return;
            }

            if (u > 0)
            {
                Color c = cubeRend.material.color;
                c.a = 1f - u;
                cubeRend.material.color = c;

                c = letter.color;
                c.a = 1f - (u * 0.5f);
                letter.color = c;
            }
        }

        public void SetType(WeaponType wt)
        {
            if (_main == null)
            {
                _main = FindFirstObjectByType<Main>();
            }

            if (_main == null) return;

            // Исправлено: вызываем экземплярный метод у _main
            WeaponConfig config = _main.GetWeaponConfig(wt);

            if (config == null) return;

            cubeRend.material.color = config.weaponColor;
            letter.text = config.letter;
            type = wt;
        }

        public void AbsorbedBy(GameObject target)
        {
            SafeDespawn(gameObject);
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
    }
}