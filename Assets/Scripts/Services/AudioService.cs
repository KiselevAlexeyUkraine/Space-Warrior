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
        
        [Header("Enemy Clips")]
        [Tooltip("Пул звуков смерти врагов. При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _enemyDeathClips;
        
        [Tooltip("Пул звуков попадания по врагу (без уничтожения). При каждом вызове выбирается случайный клип.")]
        [SerializeField] private AudioClip[] _enemyHitClips;
        
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

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
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