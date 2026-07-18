using UnityEngine;

namespace Components.Player
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioService : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Player Clips")]
        [SerializeField] private AudioClip _itemPickupClip;
        [SerializeField] private AudioClip _takeDamageClip;
        [SerializeField] private AudioClip _levelUpClip; // НОВОЕ: Клип для повышения уровня
        
        [Header("Enemy Clips")]
        [Tooltip("Пул звуков смерти врагов. При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _enemyDeathClips;
        
        [Tooltip("Пул звуков попадания по врагу (без уничтожения). При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _enemyHitClips;

        [Header("Enemy Weapon Clips")]
        [Tooltip("Пул звуков для обычных выстрелов стрелков (StraightDown / TargetPlayer).")]
        [SerializeField] private AudioClip[] _enemyRegularShootClips;

        [Tooltip("Пул звуков для запуска ракет (RocketLauncher).")]
        [SerializeField] private AudioClip[] _enemyRocketLaunchClips;

        [Tooltip("Пул звуков для взрыва ракет (EnemyFollowProjectile).")]
        [SerializeField] private AudioClip[] _enemyRocketExplosionClips;

        [Header("Enemy Laser Clips")]
        [Tooltip("Пул звуков перед выстрелом лазера (фаза подготовки). При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _laserPrepareClips;

        [Tooltip("Пул звуков самого выстрела лазера. При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _laserShootClips;
        
        [Header("Shield Clips")]
        [Tooltip("Пул звуков попадания по щиту (щит ещё жив после удара).")]
        [SerializeField] private AudioClip[] _shieldHitClips;
        [Tooltip("Пул звуков уничтожения щита (щит сбит до 0).")]
        [SerializeField] private AudioClip[] _shieldBreakClips;
        
        [Tooltip("Пул звуков восстановления щита (при получении +1 единицы щита).")]
        [SerializeField] private AudioClip[] _shieldRegenClips;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Воспроизводит звук повышения уровня.
        /// </summary>
        public void PlayLevelUp()
        {
            PlayOneShot(_levelUpClip);
        }

        /// <summary>
        /// Воспроизводит случайный звук взрыва ракеты врага.
        /// </summary>
        public void PlayRandomEnemyRocketExplosion()
        {
            if (_enemyRocketExplosionClips == null || _enemyRocketExplosionClips.Length == 0) return;

            AudioClip clip = _enemyRocketExplosionClips[Random.Range(0, _enemyRocketExplosionClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук обычного выстрела врага.
        /// </summary>
        public void PlayRandomEnemyRegularShoot()
        {
            if (_enemyRegularShootClips == null || _enemyRegularShootClips.Length == 0) return;

            AudioClip clip = _enemyRegularShootClips[Random.Range(0, _enemyRegularShootClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук запуска ракеты врага.
        /// </summary>
        public void PlayRandomEnemyRocketLaunch()
        {
            if (_enemyRocketLaunchClips == null || _enemyRocketLaunchClips.Length == 0) return;

            AudioClip clip = _enemyRocketLaunchClips[Random.Range(0, _enemyRocketLaunchClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук восстановления щита из пула _shieldRegenClips.
        /// </summary>
        public void PlayRandomShieldRegen()
        {
            if (_shieldRegenClips == null || _shieldRegenClips.Length == 0) return;

            AudioClip clip = _shieldRegenClips[Random.Range(0, _shieldRegenClips.Length)];
            PlayOneShot(clip);
        }
        
        /// <summary>
        /// Воспроизводит случайный звук попадания по щиту из пула _shieldHitClips.
        /// </summary>
        public void PlayRandomShieldHit()
        {
            if (_shieldHitClips == null || _shieldHitClips.Length == 0) return;

            AudioClip clip = _shieldHitClips[Random.Range(0, _shieldHitClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук уничтожения щита из пула _shieldBreakClips.
        /// </summary>
        public void PlayRandomShieldBreak()
        {
            if (_shieldBreakClips == null || _shieldBreakClips.Length == 0) return;

            AudioClip clip = _shieldBreakClips[Random.Range(0, _shieldBreakClips.Length)];
            PlayOneShot(clip);
        }
        
        /// <summary>
        /// Воспроизводит случайный звук смерти врага из пула _enemyDeathClips.
        /// </summary>
        public void PlayRandomEnemyDeath()
        {
            if (_enemyDeathClips == null || _enemyDeathClips.Length == 0) return;

            AudioClip clip = _enemyDeathClips[Random.Range(0, _enemyDeathClips.Length)];
            PlayOneShot(clip);
        }
        
        /// <summary>
        /// Воспроизводит случайный звук попадания по врагу из пула _enemyHitClips.
        /// </summary>
        public void PlayRandomEnemyHit()
        {
            if (_enemyHitClips == null || _enemyHitClips.Length == 0) return;

            AudioClip clip = _enemyHitClips[Random.Range(0, _enemyHitClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук из пула _laserPrepareClips (фаза подготовки лазера к выстрелу).
        /// </summary>
        public void PlayRandomLaserPrepare()
        {
            if (_laserPrepareClips == null || _laserPrepareClips.Length == 0) return;

            AudioClip clip = _laserPrepareClips[Random.Range(0, _laserPrepareClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит случайный звук из пула _laserShootClips (момент самого выстрела лазера).
        /// </summary>
        public void PlayRandomLaserShoot()
        {
            if (_laserShootClips == null || _laserShootClips.Length == 0) return;

            AudioClip clip = _laserShootClips[Random.Range(0, _laserShootClips.Length)];
            PlayOneShot(clip);
        }

        /// <summary>
        /// Воспроизводит звук подбора предмета.
        /// </summary>
        public void PlayItemPickup()
        {
            PlayOneShot(_itemPickupClip);
        }

        /// <summary>
        /// Воспроизводит звук получения урона.
        /// </summary>
        public void PlayTakeDamage()
        {
            PlayOneShot(_takeDamageClip);
        }

        /// <summary>
        /// Воспроизводит аудиоклип один раз без прерывания уже звучащих звуков.
        /// </summary>
        /// <param name="clip">Аудиоклип для воспроизведения.</param>
        public void PlayOneShot(AudioClip clip)
        {
            if (clip == null) return;

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"[{nameof(AudioService)}]: Не удалось воспроизвести звук {clip.name}, так как {nameof(AudioSource)} отсутствует на объекте!", this);
            }
        }
    }
}