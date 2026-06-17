using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioHelper : MonoBehaviour
{
    [System.Serializable]
    public struct SoundEffect
    {
        public string name;      // Название для вызова (например, "QuestPickUp")
        public AudioClip clip;   // Сам файл звука
        [Range(0, 1)] public float volume;
    }

    public List<SoundEffect> sounds = new List<SoundEffect>();
    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // Если громкость не настроена, ставим на максимум по умолчанию
        foreach (var sound in sounds)
        {
            if (sound.volume <= 0) Debug.LogWarning($"Звук {sound.name} имеет нулевую громкость!");
        }
    }

    // Этот метод мы будем вызывать из анимации
    public void PlaySound(string soundName)
    {
        SoundEffect effect = sounds.Find(s => s.name == soundName);

        if (effect.clip != null)
        {
            _audioSource.PlayOneShot(effect.clip, effect.volume > 0 ? effect.volume : 1.0f);
        }
        else
        {
            Debug.LogWarning($"Звук с именем {soundName} не найден в списке AudioHelper на {gameObject.name}");
        }
    }
}