using UnityEngine;
using Components.Ui.Pages;
using Components.Items;
using Components.Weapon;
using Zenject;
using Configs;
using Lean.Pool;
using Cysharp.Threading.Tasks; // Подключаем UniTask

namespace Components.Player
{
    public sealed class PlayerCollect : MonoBehaviour
    {
        private PlayerConfig _playerConfig;
        private AudioService _audioService;
        private BalanceConfig _balanceConfig;

        [Header("===== Weapons =====")]
        [Tooltip("Список всех оружейных слотов, установленных на корабле игрока.")]
        [SerializeField] private Components.Weapon.Weapon[] _weapons;

        [Header("===== Shield Visuals & Particles =====")]
        [Tooltip("Партикл-система щита, висящая на игроке. Использует Play/Stop.")]
        [SerializeField] private ParticleSystem _shieldParticleSystem;

        [Tooltip("Партикл, спавнящийся при каждом попадании по щиту, пока он жив.")]
        [SerializeField] private GameObject shieldHitEffectPrefab;
        [SerializeField] private float shieldHitEffectDestroyDelay = 2f;

        [Tooltip("Партикл, спавнящийся один раз в момент полного уничтожения щита.")]
        [SerializeField] private GameObject shieldBreakEffectPrefab;
        [SerializeField] private float shieldBreakEffectDestroyDelay = 2f;

        [Header("===== Player Death Visuals =====")]
        [Tooltip("Партикл взрыва самого игрока при смерти (когда здоровье/щиты уходят в минус).")]
        [SerializeField] private GameObject playerDeathEffectPrefab;
        [SerializeField] private float playerDeathEffectDestroyDelay = 3f;

        [Tooltip("Компонент или объект игрока, который нужно скрыть в самом начале смерти.")]
        [SerializeField] private GameObject playerComponent;

        private float _shieldLevel;
        
        [Header("===== Layers =====")]
        [Tooltip("Слой, на котором находятся объекты вражеских кораблей.")]
        [SerializeField] private LayerMask enemyLayer;

        [Tooltip("Слой, на котором находятся подбираемые бонусы (PowerUps).")]
        [SerializeField] private LayerMask powerUpLayer;
        
        [Tooltip("Слой, выделенный под лазерные лучи и лазерные ловушки.")]
        [SerializeField] private LayerMask laserLayer;

        private GameObject _lastTriggerGo;
        private PageSwitcher _pageSwitcher;
        private bool isShieldParticleActive;
        private bool isDead; 

        [Inject]
        private void Construct(
            PageSwitcher pageSwitcher,
            PlayerConfig playerConfig,
            AudioService audioService,
            BalanceConfig balanceConfig) 
        {
            _pageSwitcher = pageSwitcher;
            _playerConfig = playerConfig;
            _audioService = audioService;
            _balanceConfig = balanceConfig;
        }

        private void Start()
        {
            _shieldLevel = _balanceConfig.PlayerShield;

            isShieldParticleActive = false;
            SetShieldParticleActive(_shieldLevel > 0, forceRestart: true);

            ClearWeapons();

            if (_weapons.Length > 0)
            {
                _weapons[0].SetType(_playerConfig.StartWeapon);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;

            GameObject triggerObject = other.gameObject;

            if (triggerObject == _lastTriggerGo)
                return;

            _lastTriggerGo = triggerObject;

            int objectLayer = triggerObject.layer;

            bool isLaser = (laserLayer.value & (1 << objectLayer)) != 0;

            try
            {
                if (triggerObject.CompareTag("Laser"))
                {
                    isLaser = true;
                }
            }
            catch { }

            if (isLaser)
            {
                ProcessDamage();
                return; 
            }

            if ((enemyLayer.value & (1 << objectLayer)) != 0)
            {
                ProcessDamage();

                var enemy = triggerObject.GetComponentInParent<Components.Enemy.EnemyBase>();
        
                if (enemy != null)
                {
                    SafeDespawn(enemy.gameObject);
                }
                else
                {
                    SafeDespawn(triggerObject);
                }
                return;
            }

            if ((powerUpLayer.value & (1 << objectLayer)) != 0)
            {
                AbsorbPowerUp(triggerObject);
            }
        }

        private void ProcessDamage()
        {
            if (_shieldLevel > 0)
            {
                ShieldLevel--; 
            }
            else
            {
                _audioService?.PlayTakeDamage();
                ShieldLevel--; 
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

        public void AbsorbPowerUp(GameObject powerUpObject)
        {
            if (isDead) return;

            PowerUp powerUp = powerUpObject.GetComponent<PowerUp>();

            switch (powerUp.Type)
            {
                case WeaponType.shield:
                    float previousShield = _shieldLevel;
                    ShieldLevel++;
                    
                    _audioService?.PlayRandomShieldRegen();

                    if (previousShield <= 0 && _shieldLevel > 0)
                    {
                        SetShieldParticleActive(true, forceRestart: true);
                    }
                    break;

                default:
                    _audioService?.PlayItemPickup();

                    if (powerUp.Type == _weapons[0].Type)
                    {
                        Components.Weapon.Weapon emptyWeapon = GetEmptyWeaponSlot();
                        if (emptyWeapon != null)
                        {
                            emptyWeapon.SetType(powerUp.Type);
                        }
                    }
                    else
                    {
                        ClearWeapons();
                        _weapons[0].SetType(powerUp.Type);
                    }
                    break;
            }

            powerUp.AbsorbedBy(gameObject);
        }

        public float ShieldLevel
        {
            get => _shieldLevel;
            set
            {
                if (isDead) return;

                float oldShieldLevel = _shieldLevel;
                _shieldLevel = Mathf.Min(value, _balanceConfig.PlayerShield);

                if (_shieldLevel < oldShieldLevel && oldShieldLevel > 0)
                {
                    if (_shieldLevel <= 0)
                    {
                        _shieldLevel = 0;
                        SetShieldParticleActive(false);

                        if (shieldBreakEffectPrefab != null)
                        {
                            GameObject breakFX = LeanPool.Spawn(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
                            Destroy(breakFX, shieldBreakEffectDestroyDelay);
                        }

                        _audioService?.PlayRandomShieldBreak();
                    }
                    else
                    {
                        if (shieldHitEffectPrefab != null)
                        {
                            GameObject hitFX = LeanPool.Spawn(shieldHitEffectPrefab, transform.position, Quaternion.identity);
                            Destroy(hitFX, shieldHitEffectDestroyDelay);
                        }

                        _audioService?.PlayRandomShieldHit();
                    }
                }
                else if (_shieldLevel > 0 && !isShieldParticleActive)
                {
                    SetShieldParticleActive(true);
                }

                // Запуск логики смерти через UniTask
                if (_shieldLevel < 0 && !isDead)
                {
                    PlayerDeathAsync().Forget();
                }
            }
        }

        /// <summary>
        /// Асинхронный метод смерти через UniTask. Не прерывается при выключении GameObject.
        /// </summary>
        /// <summary>
        /// Асинхронный метод смерти через UniTask. Не прерывается при выключении GameObject.
        /// </summary>
        private async UniTask PlayerDeathAsync()
        {
            isDead = true;

            // Запоминаем позицию для спавна эффекта до скрытия/удаления
            Vector3 spawnPosition = transform.position;

            // Выключаем щит
            SetShieldParticleActive(false);

            // Отключаем физику текущего объекта на всякий случай
            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            // Полностью гасим целевой playerComponent.
            if (playerComponent != null)
            {
                playerComponent.SetActive(false);
            }

            // Спавним партикл взрыва на сохраненной позиции
            if (playerDeathEffectPrefab != null)
            {
                GameObject deathFX = LeanPool.Spawn(playerDeathEffectPrefab, spawnPosition, Quaternion.identity);
                Destroy(deathFX, playerDeathEffectDestroyDelay);
            }

            // Ожидаем завершения времени жизни партикла
            await UniTask.Delay(System.TimeSpan.FromSeconds(playerDeathEffectDestroyDelay));

            // Открываем страницу поражения. 
            // Если Open() возвращает UniTask, пишем просто await. Если возвращает void/UniTaskVoid, уберите await и оставьте метод.
            if (_pageSwitcher != null)
            {
                _pageSwitcher.Open(PageName.Failed);
            }

            // Уничтожаем корневой объект игрока
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }

        private void SetShieldParticleActive(bool active, bool forceRestart = false)
        {
            if (_shieldParticleSystem == null) return;

            if (isShieldParticleActive == active && !forceRestart)
                return;

            isShieldParticleActive = active;

            if (active)
            {
                _shieldParticleSystem.Play();
            }
            else
            {
                _shieldParticleSystem.Stop();
            }
        }

        private Components.Weapon.Weapon GetEmptyWeaponSlot()
        {
            foreach (Components.Weapon.Weapon weapon in _weapons)
            {
                if (weapon.Type == WeaponType.none)
                {
                    return weapon;
                }
            }
            return null;
        }

        private void ClearWeapons()
        {
            foreach (Components.Weapon.Weapon weapon in _weapons)
            {
                weapon.SetType(WeaponType.none);
            }
        }

        public Components.Weapon.Weapon[] Weapons => _weapons;
    }
}