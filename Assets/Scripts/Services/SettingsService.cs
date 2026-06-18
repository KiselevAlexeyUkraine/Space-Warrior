using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Components.Player
{
    public class SettingsService : MonoBehaviour
    {
        public event Action<bool> OnChanged;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private float _defaultMasterVolume = 0.5f;
        [SerializeField] private float _defaultSoundsVolume = 0.5f;
        [SerializeField] private float _defaultMusicVolume = 0.5f;
        [SerializeField] private float _defaultMouseSensitivity = 2f;
        [SerializeField] private float _defaultBrightness = 1f;
        [SerializeField] private int _defaultFpsLock = -1;
        [SerializeField] private int _defaultFpsCounter = 0;
        [SerializeField] private int _defaultVsync = 0;


        private Resolution[] _resolutions =
        {
            new Resolution { width = 3840, height = 2160 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 2460, height = 1080 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 1760, height = 990 },
            new Resolution { width = 1680, height = 1050 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1440, height = 900 },
            new Resolution { width = 1366, height = 768 },
            new Resolution { width = 1280, height = 1024 },
            new Resolution { width = 1280, height = 960 },
            new Resolution { width = 1280, height = 800 },
            new Resolution { width = 1280, height = 768 },
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1152, height = 864 },
            new Resolution { width = 1024, height = 768 },
            new Resolution { width = 800, height = 600 }
        };

        public Resolution[] GetAvailableResolutions() => _resolutions;
        public int SavedLanguageIndex { get; private set; }

        private const string MasterVolumePrefKey = "MasterVolume";
        private const string SoundsVolumePrefKey = "SoundsVolume";
        private const string MusicVolumePrefKey = "MusicVolume";
        private const string MouseSensitivityPrefKey = "MouseSensitivity";
        private const string FpsCounterPrefKey = "FpsCounter";
        private const string MaxFpsLockPrefKey = "FpsLock";
        private const string VsyncPrefKey = "Vsync";
        private const string ResolutionWidthKey = "ResolutionWidth";
        private const string ResolutionHeightKey = "ResolutionHeight";
        private const string BrightnessPrefKey = "Brightness";
        private const string LanguagePrefKey = "Language";

        public float SavedMasterVolume { get; private set; }
        public float SavedSoundsVolume { get; private set; }
        public float SavedMusicVolume { get; private set; }
        public float SavedMouseSensitivity { get; private set; }
        public float SavedBrightness { get; private set; }

        public float SavedMouseSensitivityMultiplier { get; set; } = 1f;

        public bool SavedFpsCounter { get; private set; }
        public bool SavedVsync { get; private set; }
        public int SavedMaxFpsLock { get; private set; }
        public int SavedResolutionWidth { get; private set; }
        public int SavedResolutionHeight { get; private set; }
        
        

        private void Awake()
        {
            var current = Screen.currentResolution;

            SavedMasterVolume = PlayerPrefs.GetFloat(MasterVolumePrefKey, _defaultMasterVolume);
            SavedSoundsVolume = PlayerPrefs.GetFloat(SoundsVolumePrefKey, _defaultSoundsVolume);
            SavedMusicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, _defaultMusicVolume);
            SavedMouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityPrefKey, _defaultMouseSensitivity);
            SavedFpsCounter = PlayerPrefs.GetInt(FpsCounterPrefKey, _defaultFpsCounter) == 1;
            SavedMaxFpsLock = PlayerPrefs.GetInt(MaxFpsLockPrefKey, _defaultFpsLock);
            SavedVsync = PlayerPrefs.GetInt(VsyncPrefKey, _defaultVsync) == 1;
            SavedBrightness = PlayerPrefs.GetFloat(BrightnessPrefKey, _defaultBrightness);
            
            SavedLanguageIndex = PlayerPrefs.GetInt(LanguagePrefKey);

            // ✅ Выбираем разрешение 1440x900, если нет сохраненного
            if (!PlayerPrefs.HasKey(ResolutionWidthKey) || !PlayerPrefs.HasKey(ResolutionHeightKey))
            {
                var defaultRes = Array.Find(_resolutions, r => r.width == 1440 && r.height == 900);
                if (defaultRes.width == 0 && defaultRes.height == 0) // если не найдено, fallback на минимальное
                    defaultRes = _resolutions[_resolutions.Length - 1];
                SavedResolutionWidth = defaultRes.width;
                SavedResolutionHeight = defaultRes.height;
                PlayerPrefs.SetInt(ResolutionWidthKey, SavedResolutionWidth);
                PlayerPrefs.SetInt(ResolutionHeightKey, SavedResolutionHeight);
            }
            else
            {
                SavedResolutionWidth = PlayerPrefs.GetInt(ResolutionWidthKey, current.width);
                SavedResolutionHeight = PlayerPrefs.GetInt(ResolutionHeightKey, current.height);
            }
        }

        private void Start()
        {
            SetMasterVolume(SavedMasterVolume);
            SetSoundsVolume(SavedSoundsVolume);
            SetMusicVolume(SavedMusicVolume);
            SetMouseSensitivity(SavedMouseSensitivity);
            SetBrightness(SavedBrightness);
            SetFpsCounter(SavedFpsCounter);
            SetMaxFpsLock(SavedMaxFpsLock);
            SetResolution(SavedResolutionHeight, SavedResolutionWidth);
            SetVsync(SavedVsync);
        }
        
        public void SetLanguage(int index)
        {
            SavedLanguageIndex = index;
            PlayerPrefs.SetInt(LanguagePrefKey, index);
        }

        public void SetMasterVolume(float value)
        {
            SavedMasterVolume = value;
            SetVolume(value, MasterVolumePrefKey);
        }

        public void SetSoundsVolume(float value)
        {
            SavedSoundsVolume = value;
            SetVolume(value, SoundsVolumePrefKey);
        }

        public void SetMusicVolume(float value)
        {
            SavedMusicVolume = value;
            SetVolume(value, MusicVolumePrefKey);
        }

        public void SetMouseSensitivity(float value)
        {
            SavedMouseSensitivity = value;
            PlayerPrefs.SetFloat(MouseSensitivityPrefKey, value);
        }

        public void SetFpsCounter(bool value)
        {
            SavedFpsCounter = value;
            PlayerPrefs.SetInt(FpsCounterPrefKey, value ? 1 : 0);
            OnChanged?.Invoke(value);
        }

        public void SetMaxFpsLock(int value)
        {
            SavedMaxFpsLock = value;
            Application.targetFrameRate = value;
            PlayerPrefs.SetInt(MaxFpsLockPrefKey, value);
        }

        public void SetVsync(bool value)
        {
            SavedVsync = value;
            PlayerPrefs.SetInt(VsyncPrefKey, value ? 1 : 0);
            ApplyVsync(value);
        }

        private void ApplyVsync(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
            Application.targetFrameRate = value ? 60 : SavedMaxFpsLock;
        }

        public void SetResolution(int height, int width)
        {
            SavedResolutionWidth = width;
            SavedResolutionHeight = height;
            PlayerPrefs.SetInt(ResolutionHeightKey, height);
            PlayerPrefs.SetInt(ResolutionWidthKey, width);
        }

        public void SetBrightness(float value)
        {
            SavedBrightness = value;
            PlayerPrefs.SetFloat(BrightnessPrefKey, value);
        }

        public void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        private void SetVolume(float value, string key)
        {
            value = Mathf.Clamp(value, 0.0001f, 1f);
            var volume = Mathf.Log10(value) * 20f;
            _mixer.SetFloat(key, volume);
            PlayerPrefs.SetFloat(key, value);
        }
    }
}
