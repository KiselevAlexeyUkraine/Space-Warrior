using UnityEngine;
using Components.Weapon;
using Lean.Pool;

namespace Components.Enemy
{
    public class EnemyWithShield : EnemyBase
    {
        [Header("Настройки Щита Врага (партиклы)")]
        [Tooltip("Партикл-система щита, висящая на враге. Play/Stop вместо SetActive.")]
        [SerializeField] private GameObject shieldParticle;

        [Tooltip("Партикл, спавнящийся при каждом попадании по щиту, пока щит не сбит до 0.")]
        [SerializeField] private GameObject shieldHitEffectPrefab;

        [Tooltip("Время жизни спавнящегося партикла попадания по щиту (в секундах).")]
        [SerializeField] private float shieldHitEffectDestroyDelay = 2f;
        
        [Tooltip("Партикл, спавнящийся один раз в момент полного уничтожения щита (когда щит доходит до 0).")]
        [SerializeField] private GameObject shieldBreakEffectPrefab;

        [Tooltip("Время жизни спавнящегося партикла уничтожения щита (в секундах).")]
        [SerializeField] private float shieldBreakEffectDestroyDelay = 2f;

        [Tooltip("Максимальное количество единиц щита. Перезаписывается из BalanceConfig по enemyLevel.")]
        [SerializeField] private int maxShieldLevel = 4;

        [Tooltip("Через сколько секунд восстанавливается 1 единица щита. Перезаписывается из BalanceConfig (значение из таблицы, столбец уровня).")]
        [SerializeField] private float regenIntervalSeconds = 15f;

        private int currentShieldLevel;
        private Material shieldMat;
        private int lastShownLevel = -1;
        private float nextRegenTime;
        private bool isRegenScheduled;
        private bool isShieldParticleActive;
        

        // Перезаписываем щитовые параметры из таблицы баланса поверх базовых (speed/health/collisionDamage)
        protected override void ApplyBalanceConfig()
        {
            base.ApplyBalanceConfig();

            if (balanceConfig == null)
            {
                if (debugBalanceValues)
                {
                    Debug.LogWarning($"[{gameObject.name}] BalanceConfig не внедрён — щит использует значения из инспектора: maxShieldLevel={maxShieldLevel}, regenIntervalSeconds={regenIntervalSeconds}");
                }
                return;
            }

            maxShieldLevel = Mathf.RoundToInt(balanceConfig.GetEnemyShield(enemyLevel));
            regenIntervalSeconds = balanceConfig.GetEnemyShieldRegenInterval(enemyLevel);

            if (debugBalanceValues)
            {
                Debug.Log($"[{gameObject.name}] ({GetType().Name}, уровень {enemyLevel}) щит из баланса: " +
                          $"MaxShieldLevel = {maxShieldLevel}, RegenInterval = {regenIntervalSeconds} сек/единица");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            currentShieldLevel = maxShieldLevel;
            lastShownLevel = -1;
            isRegenScheduled = false;
            isShieldParticleActive = false;

            SetShieldParticleActive(currentShieldLevel > 0, forceRestart: true);

            UpdateShieldVisual(forceUpdate: true);

            // Если щит изначально не полный (например, задан меньше максимума), сразу запускаем таймер восстановления
            ScheduleRegenIfNeeded();
        }

        public override void Move()
        {
            pos -= new Vector3(0, speed * Time.deltaTime, 0);
        }

        protected override void Update()
        {
            base.Update();

            HandleShieldRegeneration();
        }

        // Ставит таймер на восстановление следующей единицы, если щит не полный и таймер ещё не запущен
        private void ScheduleRegenIfNeeded()
        {
            if (maxShieldLevel <= 0 || currentShieldLevel >= maxShieldLevel)
            {
                isRegenScheduled = false;
                return;
            }

            if (!isRegenScheduled)
            {
                nextRegenTime = Time.time + regenIntervalSeconds;
                isRegenScheduled = true;
            }
        }

        // Пошаговая регенерация: раз в regenIntervalSeconds добавляется ровно 1 единица щита.
        // Работает одинаково и когда щит частично поврежден, и когда он полностью сбит до 0 —
        // таймер в обоих случаях считает от момента последнего попадания.
        private void HandleShieldRegeneration()
        {
            if (maxShieldLevel <= 0 || currentShieldLevel >= maxShieldLevel)
            {
                isRegenScheduled = false;
                return;
            }

            if (!isRegenScheduled)
            {
                nextRegenTime = Time.time + regenIntervalSeconds;
                isRegenScheduled = true;
                return;
            }

            if (Time.time >= nextRegenTime)
            {
                // Запоминаем старое значение до прибавления
                int previousShieldLevel = currentShieldLevel;

                currentShieldLevel += 1;
                currentShieldLevel = Mathf.Min(currentShieldLevel, maxShieldLevel);

                // Звук восстановления щита
                _audioService?.PlayRandomShieldRegen();

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Если щит поднялся с 0 до 1, 
                // включаем партиклы принудительно с флагом forceRestart = true
                if (previousShieldLevel == 0 && currentShieldLevel > 0)
                {
                    SetShieldParticleActive(true, forceRestart: true);
                }
                else if (currentShieldLevel > 0 && !isShieldParticleActive)
                {
                    SetShieldParticleActive(true);
                }

                UpdateShieldVisual(forceUpdate: true);

                if (currentShieldLevel < maxShieldLevel)
                {
                    nextRegenTime = Time.time + regenIntervalSeconds;
                }
                else
                {
                    isRegenScheduled = false;
                }
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (isShieldParticleActive && other.CompareTag("ProjectileHero"))
            {
                Projectile p = other.GetComponent<Projectile>();
                if (p != null)
                {
                    ProcessShieldDamage(p.type);
                }

                SafeDespawn(other.gameObject);
                return;
            }

            base.OnTriggerEnter(other);
        }

        private void ProcessShieldDamage(WeaponType weaponType)
        {
            WeaponConfig config = main.GetWeaponConfig(weaponType);
            if (config != null)
            {
                currentShieldLevel -= Mathf.RoundToInt(config.damageOnHit);
            }

            if (currentShieldLevel <= 0)
            {
                currentShieldLevel = 0;
                SetShieldParticleActive(false);

                // Партикл разрушения щита
                if (shieldBreakEffectPrefab != null)
                {
                    GameObject spawnedEffect = LeanPool.Spawn(shieldBreakEffectPrefab, transform.position, Quaternion.identity);
                    StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, shieldBreakEffectDestroyDelay));
                }

                // Звук разрушения
                _audioService?.PlayRandomShieldBreak();
            }
            else
            {
                // Щит ещё жив — партикл попадания и звук попадания
                if (shieldHitEffectPrefab != null)
                {
                    GameObject spawnedEffect = LeanPool.Spawn(shieldHitEffectPrefab, transform.position, Quaternion.identity);
                    StartCoroutine(DespawnEffectAfterDelay(spawnedEffect, shieldHitEffectDestroyDelay));
                }

                _audioService?.PlayRandomShieldHit();
            }

            // Сразу перезапускаем отсчёт до следующей единицы, не дожидаясь следующего Update —
            // гарантирует, что щит (в том числе сбитый до 0) начнёт восстанавливаться ровно через regenIntervalSeconds
            nextRegenTime = Time.time + regenIntervalSeconds;
            isRegenScheduled = true;

            UpdateShieldVisual();
        }

        // Включает/выключает партиклы щита. Play/Stop вместо SetActive(GameObject).
        private void SetShieldParticleActive(bool active, bool forceRestart = false)
        {


            if (isShieldParticleActive == active && !forceRestart)
                return;

            isShieldParticleActive = active;

            if (active)
            {
                shieldParticle.SetActive(true);
            }
            else
            {
                shieldParticle.SetActive(false);
            }
        }

        private void UpdateShieldVisual(bool forceUpdate = false)
        {
            if (shieldMat == null) return;

            if (lastShownLevel != currentShieldLevel || forceUpdate)
            {
                lastShownLevel = currentShieldLevel;
                shieldMat.mainTextureOffset = new Vector2(0.2f * lastShownLevel, 0);
            }
        }
    }
}