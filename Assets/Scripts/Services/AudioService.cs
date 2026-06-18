using UnityEngine;

namespace Services
{
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;



        public void PlayOneShot(AudioClip clip)
        {
            if (clip != null) _audioSource.PlayOneShot(clip);
        }
        
    }
}