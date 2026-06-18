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