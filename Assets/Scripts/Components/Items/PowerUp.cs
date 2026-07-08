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
        [SerializeField] 
        [Tooltip("Минимальная и максимальная скорость вращения куба по осям.")]
        private Vector2 rotMinMax = new Vector2(15, 90);

        [SerializeField] 
        [Tooltip("Диапазон скорости движения вниз (X — минимальная, Y — максимальная).")]
        private Vector2 driftMinMax = new Vector2(0.25f, 2f);

        [SerializeField] 
        [Tooltip("Время жизни объекта до начала исчезновения (в секундах).")]
        private float lifeTime = 6f;

        [SerializeField] 
        [Tooltip("Длительность фазы плавного исчезновения (в секундах).")]
        private float fadeTime = 4f;

        [Header("Set Dynamically")]
        [SerializeField] 
        [Tooltip("Текущий тип оружия, который дает этот PowerUp.")]
        private WeaponType type;
        
        public WeaponType Type => type;

        [SerializeField] 
        [Tooltip("Ссылка на дочерний объект куба, который визуально вращается.")]
        private GameObject cube;

        [SerializeField] 
        [Tooltip("Текстовый меш для отображения буквы оружия.")]
        private TextMesh letter;

        [SerializeField] 
        [Tooltip("Рассчитанная скорость вращения куба в секунду для этой итерации.")]
        private Vector3 rotPerSecond;

        [SerializeField] 
        [Tooltip("Время (Time.time) появления объекта в игре.")]
        private float birthTime;

        [SerializeField] 
        [Tooltip("Рендерер куба для управления прозрачностью при исчезновении.")]
        private Renderer cubeRend;

        private Vector3 movementVelocity; 
        private Main _main;
        
        [Inject]
        private void Construct(Main main)
        {
            _main = main;
        }

        // ВАЖНО: инициализация перенесена из Awake() в OnEnable().
        // Awake() вызывается Unity только ОДИН раз за всё время жизни объекта.
        // Так как PowerUp переиспользуется через LeanPool, при повторном спавне
        // Awake() больше не срабатывает — а OnEnable() срабатывает каждый раз
        // при повторной активации объекта из пула, поэтому сюда и переносим
        // весь сброс состояния (birthTime, скорость, вращение, прозрачность).
        private void OnEnable()
        {
            // Направление движения строго вниз по оси Y
            Vector3 direction = Vector3.down; 
            
            /* Если вам нужен небольшой случайный наклон по бокам при падении, 
               раскомментируйте строчку ниже:
               direction.x = Random.Range(-0.2f, 0.2f); direction.Normalize();
            */

            // Задаем скорость падения на основе диапазона driftMinMax
            float speed = Random.Range(driftMinMax.x, driftMinMax.y);
            movementVelocity = direction * speed;

            transform.rotation = Quaternion.identity;

            rotPerSecond = new Vector3(
                Random.Range(rotMinMax.x, rotMinMax.y),
                Random.Range(rotMinMax.x, rotMinMax.y),
                Random.Range(rotMinMax.x, rotMinMax.y)
            );

            birthTime = Time.time;

            // Сбрасываем прозрачность, иначе объект из пула может появиться
            // уже полупрозрачным/невидимым, если был деспавнен во время фазы затухания.
            if (cubeRend != null && cubeRend.material.HasProperty("_Color"))
            {
                Color c = cubeRend.material.color;
                c.a = 1f;
                cubeRend.material.color = c;
            }

            if (letter != null)
            {
                Color c = letter.color;
                c.a = 1f;
                letter.color = c;
            }
        }

        private void Update()
        {
            // Движение вниз в мировых координатах
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