using System;
using UnityEngine;
using Components.Ui.Pages;
using Components.Items;
using Components.Weapon;
using Zenject;
using Configs;
using Lean.Pool;
using Cysharp.Threading.Tasks;

namespace Components.Player
{
    public sealed class PlayerCollect : MonoBehaviour
    {
        private PlayerConfig _playerConfig;
        private AudioService _audioService;
        private BalanceConfig _balanceConfig;
        private PlayerScore _playerScore;

        [Header("===== Weapons =====")]
        [SerializeField] private Components.Weapon.Weapon[] _weapons;

        [Header("===== Shield Visuals & Particles =====")]
        [SerializeField] private GameObject _shieldParticleSystem;
        [SerializeField] private GameObject shieldHitEffectPrefab;
        [SerializeField] private float shieldHitEffectDestroyDelay = 2f;
        [SerializeField] private GameObject shieldBreakEffectPrefab;
        [SerializeField] private float shieldBreakEffectDestroyDelay = 2f;

        [Header("===== Level Up Visuals =====")]
        [Tooltip("Партикл повышения уровня, который включается/выключается.")]
        [SerializeField] private GameObject _levelUpParticleSystem;

        [Header("===== Player Death Visuals =====")]
        [SerializeField] private GameObject playerDeathEffectPrefab;
        [SerializeField] private float playerDeathEffectDestroyDelay = 3f;
        [SerializeField] private GameObject playerComponent;

        private float _shieldLevel;

        /// <summary>
        /// Событие изменения текущего количества щита у игрока.
        /// </summary>
        public event Action<int> OnShieldLevelChanged;
        
        [Header("===== Layers =====")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask nonDespawnableEnemyLayer;
        [SerializeField] private LayerMask powerUpLayer;
        [SerializeField] private LayerMask laserLayer;

        private GameObject _lastTriggerGo;
        private int _lastTriggerFrame = -1;
        private PageSwitcher _pageSwitcher;
        private bool isShieldParticleActive;
        private bool isLevelUpParticleActive;
        private bool isDead; 

        [Inject]
        private void Construct(
            PageSwitcher pageSwitcher,
            PlayerConfig playerConfig,
            AudioService audioService,
            BalanceConfig balanceConfig,
            PlayerScore playerScore) 
        {
            _pageSwitcher = pageSwitcher;
            _playerConfig = playerConfig;
            _audioService = audioService;
            _balanceConfig = balanceConfig;
            _playerScore = playerScore;
        }

        private void Start()
        {
            _shieldLevel = _balanceConfig.PlayerShield;
            OnShieldLevelChanged?.Invoke(Mathf.Max(0, (int)_shieldLevel));

            isShieldParticleActive = false;
            SetShieldParticleActive(_shieldLevel > 0, forceRestart: true);

            isLevelUpParticleActive = false;
            SetLevelUpParticleActive(false, forceRestart: true);

            ClearWeapons();

            if (_weapons.Length > 0)
            {
                _weapons[0].SetType(_playerConfig.StartWeapon);
            }

            if (_playerScore != null)
            {
                _playerScore.OnLevelChanged += OnPlayerLevelChanged;
            }
        }

        private void OnDestroy()
        {
            if (_playerScore != null)
            {
                _playerScore.OnLevelChanged -= OnPlayerLevelChanged;
            }
        }

        private void OnPlayerLevelChanged(int newLevel)
        {
            if (isDead) return;

            ShieldLevel = _balanceConfig.PlayerShield;

            SetLevelUpParticleActive(true, forceRestart: true);

            DisableLevelUpParticleAfterDelay(2.5f).Forget();
        }

        private async UniTaskVoid DisableLevelUpParticleAfterDelay(float delaySeconds)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds));
            if (!isDead)
            {
                SetLevelUpParticleActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;

            GameObject triggerObject = other.gameObject;

            if (triggerObject == _lastTriggerGo && Time.frameCount == _lastTriggerFrame)
                return;

            _lastTriggerGo = triggerObject;
            _lastTriggerFrame = Time.frameCount;

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

            if ((nonDespawnableEnemyLayer.value & (1 << objectLayer)) != 0)
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
            SafeDespawn(powerUpObject);
        }

        public float ShieldLevel
        {
            get => _shieldLevel;
            set
            {
                if (isDead) return;

                float oldShieldLevel = _shieldLevel;
                _shieldLevel = Mathf.Min(value, _balanceConfig.PlayerShield);

                OnShieldLevelChanged?.Invoke(Mathf.Max(0, (int)_shieldLevel));

                if (_shieldLevel < oldShieldLevel && oldShieldLevel > 0)
                {
                    if (_shieldLevel <= 0)
                    {
                        _shieldLevel = 0;
                        SetShieldParticleActive(false);

                        if (shieldBreakEffectPrefab != null)
                        {
                            GameObject breakFX = LeanPool.Spawn(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
                            LeanPool.Despawn(breakFX, shieldBreakEffectDestroyDelay);
                        }

                        _audioService?.PlayRandomShieldBreak();
                    }
                    else
                    {
                        if (shieldHitEffectPrefab != null)
                        {
                            GameObject hitFX = LeanPool.Spawn(shieldHitEffectPrefab, transform.position, Quaternion.identity);
                            LeanPool.Despawn(hitFX, shieldHitEffectDestroyDelay);
                        }

                        _audioService?.PlayRandomShieldHit();
                    }
                }
                else if (_shieldLevel > 0 && !isShieldParticleActive)
                {
                    SetShieldParticleActive(true);
                }

                if (_shieldLevel < 0 && !isDead)
                {
                    PlayerDeathAsync().Forget();
                }
            }
        }

        private async UniTask PlayerDeathAsync()
        {
            isDead = true;

            Vector3 spawnPosition = transform.position;
            SetShieldParticleActive(false);
            SetLevelUpParticleActive(false);

            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = false;
            }

            if (playerComponent != null)
            {
                playerComponent.SetActive(false);
            }

            if (playerDeathEffectPrefab != null)
            {
                GameObject deathFX = LeanPool.Spawn(playerDeathEffectPrefab, spawnPosition, Quaternion.identity);
                LeanPool.Despawn(deathFX, playerDeathEffectDestroyDelay);
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(playerDeathEffectDestroyDelay));

            if (_pageSwitcher != null)
            {
                _pageSwitcher.Open(PageName.Failed);
            }

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
            _shieldParticleSystem.SetActive(active);
        }

        public void SetLevelUpParticleActive(bool active, bool forceRestart = false)
        {
            if (_levelUpParticleSystem == null) return;

            if (isLevelUpParticleActive == active && !forceRestart)
                return;

            isLevelUpParticleActive = active;
            _levelUpParticleSystem.SetActive(active);
        }

        private Components.Weapon.Weapon GetEmptyWeaponSlot()
        {
            foreach (Components.Weapon.Weapon weapon in _weapons)
            {
                if (weapon.Type == WeaponType.none) return weapon;
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