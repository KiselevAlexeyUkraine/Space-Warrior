using Components.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Components.Effects;
using Components.Player.Localization;
using System.Collections.Generic;

namespace Components.Ui.Pages.Menu
{
    public class SettingsPage : BasePage
    {
        [Header("Основные элементы")]
        [SerializeField] private Button _back;

        [Header("Tекстовые элементы")]
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text soundsVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text mouseSensitivityText;
        [SerializeField] private TMP_Text brightness;
        [SerializeField] private TMP_Text frameLimit;
        [SerializeField] private TMP_Text fpsCounter;
        [SerializeField] private TMP_Text vSync;
        [SerializeField] private TMP_Text resolutionText;
        [SerializeField] private TMP_Text languageText;
        [SerializeField] private TMP_Text backText;

        [Header("Аудио")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private TMP_Text _masterVolumeValue;
        [SerializeField] private Slider _soundsVolumeSlider;
        [SerializeField] private TMP_Text _soundsVolumeValue;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private TMP_Text _musicVolumeValue;

        [Header("Управление")]
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private TMP_Text _mouseSensitivityValue;

        [Header("Производительность")]
        [SerializeField] private Slider _fpsLockSlider;
        [SerializeField] private TMP_Text _fpsLockValue;
        [SerializeField] private Toggle _fpsCounterToggle;
        [SerializeField] private TMP_Text _fpsCounterValue;
        [SerializeField] private Toggle _vsyncToggle;
        [SerializeField] private TMP_Text _vsyncValue;

        [Header("Графика")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Slider _brightnessSlider;
        [SerializeField] private TMP_Text _brightnessValue;

        [Header("Локализация")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        private SettingsService _settingsService;
        private PostProcessingController _postProcessingController;
        private LocalizationManager _localizationManager;

        private Resolution[] _resolutions;
        private bool _initialized = false;

        [Inject]
        private void Construct(
            SettingsService settingsService,
            PostProcessingController postProcessingController,
            LocalizationManager localizationManager)
        {
            _settingsService = settingsService;
            _postProcessingController = postProcessingController;
            _localizationManager = localizationManager;
        }

        private void Start()
        {
            // Запуск эффектов и получение разрешений
            _postProcessingController.EnableEffects(true);
            _resolutions = _settingsService.GetAvailableResolutions();

            // Настройка UI и логики
            SetupUI();

            // Первичное обновление текстов
            UpdateTexts();

            _initialized = true;
        }


        private void OnEnable()
        {
            if (_initialized)
            {
                RefreshSettingsState();
                UpdateTexts();
                //_localizationManager.OnLanguageChanged += UpdateTexts;
            }
        }

        private void OnDisable()
        {
            // Сохраняем настройки при выходе
            _settingsService.SaveSettings();
            //_localizationManager.OnLanguageChanged -= UpdateTexts;
        }

        private void OnDestroy()
        {
            //_localizationManager.OnLanguageChanged -= UpdateTexts;
        }

        private void SetupUI()
        {
            OnClose += _settingsService.SaveSettings;
            _back.onClick.AddListener(() => PageSwitcher.Open(PageName.Menu).Forget());

            // === Обработка звука ===
            _masterVolumeSlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetMasterVolume(value);
                _masterVolumeValue.text = $"{value:0.00}";
            });

            _soundsVolumeSlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetSoundsVolume(value);
                _soundsVolumeValue.text = $"{value:0.00}";
            });

            _musicVolumeSlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetMusicVolume(value);
                _musicVolumeValue.text = $"{value:0.00}";
            });

            // === Сенса мыши ===
            _mouseSensitivitySlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetMouseSensitivity(value);
                _mouseSensitivityValue.text = $"{value:0.00}";
            });

            // === FPS и VSync ===
            _fpsLockSlider.onValueChanged.AddListener(value =>
            {
                int intValue = (int)value;
                if (!_vsyncToggle.isOn)
                    _settingsService.SetMaxFpsLock(intValue < (int)_fpsLockSlider.minValue + 1 ? -1 : intValue);

                _fpsLockValue.text = _vsyncToggle.isOn
                    ? "VSync"
                    : (intValue < (int)_fpsLockSlider.minValue + 1
                        ? _localizationManager.ToggleOff
                        : $"{intValue}");
            });

            _fpsCounterToggle.onValueChanged.AddListener(value =>
            {
                _settingsService.SetFpsCounter(value);
                _fpsCounterValue.text = value
                    ? _localizationManager.ToggleOn
                    : _localizationManager.ToggleOff;
            });

            _vsyncToggle.onValueChanged.AddListener(value =>
            {
                _settingsService.SetVsync(value);
                _vsyncValue.text = value
                    ? _localizationManager.ToggleOn
                    : _localizationManager.ToggleOff;

                _fpsLockSlider.interactable = !value;
                _fpsLockValue.text = value
                    ? "VSync"
                    : (_fpsLockSlider.value < _fpsLockSlider.minValue + 1
                        ? _localizationManager.ToggleOff
                        : $"{(int)_fpsLockSlider.value}");
            });

            // === Разрешения ===
            _resolutionDropdown.onValueChanged.AddListener(value =>
            {
                var res = _resolutions[value];
                _settingsService.SetResolution(res.height, res.width);
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            });

            // === Яркость ===
            _brightnessSlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetBrightness(value);
                _postProcessingController.ApplyBrightness(value);
                _brightnessValue.text = $"{value:0.00}";
            });
            
            // === Язык (ИЗМЕНЕН) ===
            _languageDropdown.onValueChanged.AddListener(value =>
            {
                // 1. Сохраняем выбранный индекс в SettingsService
                _settingsService.SetLanguage(value); 
                
                // 2. Устанавливаем язык в менеджере (который сам вызовет UpdateTexts)
                var selectedLanguage = (LocalizationManager.Language)value;
                _localizationManager.SetLanguage(selectedLanguage);
                UpdateTexts();

            });

            RefreshSettingsState();
        }

        private void RefreshSettingsState()
        {
            // === Загружаем сохранённые настройки ===
            _masterVolumeSlider.value = _settingsService.SavedMasterVolume;
            _soundsVolumeSlider.value = _settingsService.SavedSoundsVolume;
            _musicVolumeSlider.value = _settingsService.SavedMusicVolume;
            _mouseSensitivitySlider.value = _settingsService.SavedMouseSensitivity;

            _fpsLockSlider.value = _settingsService.SavedMaxFpsLock;
            _fpsCounterToggle.isOn = _settingsService.SavedFpsCounter;
            _vsyncToggle.isOn = _settingsService.SavedVsync;

            QualitySettings.vSyncCount = _settingsService.SavedVsync ? 1 : 0;
            Application.targetFrameRate = _settingsService.SavedVsync ? 60 : _settingsService.SavedMaxFpsLock;

            _fpsLockSlider.interactable = !_settingsService.SavedVsync;

            // === Разрешения ===
            _resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            int selectedIndex = 0;
            var currentRes = Screen.currentResolution;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                var res = _resolutions[i];
                string label = $"{res.width} x {res.height}";
                options.Add(label);

                if (res.width == currentRes.width && res.height == currentRes.height)
                    selectedIndex = i;
            }

            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.value = selectedIndex;
            _resolutionDropdown.RefreshShownValue();

            // === Яркость ===
            _brightnessSlider.value = _settingsService.SavedBrightness;
            _brightnessValue.text = $"{_settingsService.SavedBrightness:0.00}";
            _postProcessingController.ApplyBrightness(_settingsService.SavedBrightness);

            // === Язык ===
            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new List<string> { "Русский", "English" });

            // Выставляем сохранённый язык
            _languageDropdown.value = _settingsService.SavedLanguageIndex;

            _languageDropdown.RefreshShownValue();
            
            //_localizationManager.SetLanguage((LocalizationManager.Language)_settingsService.SavedLanguageIndex);
        }

        private void UpdateTexts()
        {
            if (_localizationManager == null) return;

            masterVolumeText.text = _localizationManager.MasterVolume;
            soundsVolumeText.text = _localizationManager.SoundVolume;
            musicVolumeText.text = _localizationManager.MusicVolume;
            mouseSensitivityText.text = _localizationManager.MouseSensitivity;
            brightness.text = _localizationManager.Brightness;
            frameLimit.text = _localizationManager.FrameLimit;
            fpsCounter.text = _localizationManager.FpsCounter;
            vSync.text = _localizationManager.VSync;
            resolutionText.text = _localizationManager.Resolution;
            languageText.text = _localizationManager.Languages;
            backText.text = _localizationManager.Back;

            _vsyncValue.text = _vsyncToggle.isOn ? _localizationManager.ToggleOn : _localizationManager.ToggleOff;
            _fpsCounterValue.text = _fpsCounterToggle.isOn ? _localizationManager.ToggleOn : _localizationManager.ToggleOff;

            _fpsLockValue.text = _vsyncToggle.isOn
                ? "VSync"
                : (_fpsLockSlider.value < _fpsLockSlider.minValue + 1
                    ? _localizationManager.ToggleOff
                    : $"{(int)_fpsLockSlider.value}");
        }
    }
}
