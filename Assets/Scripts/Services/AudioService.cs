using UnityEngine;

namespace Services
{
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        [Header("Quest Sounds")]
        [SerializeField] private AudioClip _itemPickupClip;    // Звук подбора предмета
        [SerializeField] private AudioClip _questActivateClip; // Звук активации квеста
        [SerializeField] private AudioClip _questCompleteClip; // Звук завершения квеста

        public void PlayOneShot(AudioClip clip)
        {
            if (clip != null) _audioSource.PlayOneShot(clip);
        }

        // Удобные методы для вызова конкретных звуков
        public void PlayItemPickup() => PlayOneShot(_itemPickupClip);
        public void PlayQuestActivate() => PlayOneShot(_questActivateClip);
        public void PlayQuestComplete() => PlayOneShot(_questCompleteClip);
    }
}