using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Components.Effects;
using Services.Localization;

namespace Components.Ui.Pages.Game
{
    public class GameSettingsPage : BasePage
    {
        [Header("Основные элементы")]
        [SerializeField] private Button _back;

        // НОВЫЕ ПОЛЯ: Текстовые элементы для локализации
        [Header("Текстовые элементы")]
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text soundsVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text mouseSensitivityText;
        [SerializeField] private TMP_Text brightnessText; // Переименовано, чтобы не конфликтовать со SliderValue
        [SerializeField] private TMP_Text frameLimitText; // Добавлен для текста "Ограничение кадров"
        [SerializeField] private TMP_Text fpsCounterText; // Добавлен
        [SerializeField] private TMP_Text vSyncText; // Добавлен
        [SerializeField] private TMP_Text backText; // Добавлен для кнопки назад

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
        [SerializeField] private Slider _brightnessSlider;
        [SerializeField] private TMP_Text _brightnessValue;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;

        private SettingsService _settingsService;
        private PostProcessingController _postProcessingController;
        private LocalizationManager _localizationManager;
        private Resolution[] _resolutions;

        [Inject]
        private void Construct(
            SettingsService settingsService, 
            PostProcessingController postProcessingController,
            LocalizationManager localizationManager) 
        {
            _settingsService = settingsService;
            _postProcessingController = postProcessingController;
            _localizationManager = localizationManager; // Сохраняем ссылку
            _resolutions = _settingsService.GetAvailableResolutions();
            OnClose += settingsService.SaveSettings;
        }

        private void Awake()
        {
            _back.onClick.AddListener(() => PageSwitcher.Open(PageName.Pause).Forget());

            // ... (Обработчики слайдеров, без изменений)
            
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

            _mouseSensitivitySlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetMouseSensitivity(value);
                _mouseSensitivityValue.text = $"{value:0.00}";
            });
            
            // ИЗМЕНЕНИЕ: Используем локализованные строки
            _fpsLockSlider.onValueChanged.AddListener(value =>
            {
                int intValue = (int)value;
                if (!_vsyncToggle.isOn)
                    _settingsService.SetMaxFpsLock(intValue < (int)_fpsLockSlider.minValue + 1 ? -1 : intValue);
                _fpsLockValue.text = _vsyncToggle.isOn 
                    ? "VSync" 
                    : (intValue < (int)_fpsLockSlider.minValue + 1 
                        ? _localizationManager.ToggleOff // Используем локализацию
                        : $"{intValue}");
            });

            // ИЗМЕНЕНИЕ: Используем локализованные строки
            _fpsCounterToggle.onValueChanged.AddListener(value =>
            {
                _settingsService.SetFpsCounter(value);
                _fpsCounterValue.text = value 
                    ? _localizationManager.ToggleOn // Используем локализацию
                    : _localizationManager.ToggleOff; // Используем локализацию
            });

            // ИЗМЕНЕНИЕ: Используем локализованные строки
            _vsyncToggle.onValueChanged.AddListener(value =>
            {
                _settingsService.SetVsync(value);
                _vsyncValue.text = value 
                    ? _localizationManager.ToggleOn // Используем локализацию
                    : _localizationManager.ToggleOff; // Используем локализацию
                _fpsLockSlider.interactable = !value;
                _fpsLockValue.text = value 
                    ? "VSync" 
                    : (_fpsLockSlider.value < _fpsLockSlider.minValue + 1 
                        ? _localizationManager.ToggleOff // Используем локализацию
                        : $"{(int)_fpsLockSlider.value}");
            });

            _brightnessSlider.onValueChanged.AddListener(value =>
            {
                _settingsService.SetBrightness(value);
                _postProcessingController.ApplyBrightness(value);
                _brightnessValue.text = $"{value:0.00}";
            });

            _resolutionDropdown.onValueChanged.AddListener(index =>
            {
                var res = _resolutions[index];
                _settingsService.SetResolution(res.height, res.width);
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            });
        }

        private void OnEnable()
        {
            // Загрузка настроек (Refresh)
            _masterVolumeSlider.value = _settingsService.SavedMasterVolume;
            _soundsVolumeSlider.value = _settingsService.SavedSoundsVolume;
            _musicVolumeSlider.value = _settingsService.SavedMusicVolume;
            _mouseSensitivitySlider.value = _settingsService.SavedMouseSensitivity;
            _fpsLockSlider.value = _settingsService.SavedMaxFpsLock;
            _fpsCounterToggle.isOn = _settingsService.SavedFpsCounter;
            _vsyncToggle.isOn = _settingsService.SavedVsync;
            
            // ... (логика VSync, FPS, Brightness, Resolution без изменений)

            QualitySettings.vSyncCount = _settingsService.SavedVsync ? 1 : 0;
            Application.targetFrameRate = _settingsService.SavedVsync ? 60 : _settingsService.SavedMaxFpsLock;

            _fpsLockSlider.interactable = !_settingsService.SavedVsync;
            
            float clampedBrightness = Mathf.Max(_settingsService.SavedBrightness, 0.5f);
            _brightnessSlider.value = clampedBrightness;
            _brightnessValue.text = $"{clampedBrightness:0.00}";
            _postProcessingController.ApplyBrightness(clampedBrightness);

            // Разрешения
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
            
            // НОВОЕ: Обновляем все тексты, используя локализацию
            UpdateTexts();
        }

        // НОВЫЙ МЕТОД: Обновление всех текстов из LocalizationManager
        private void UpdateTexts()
        {
            if (_localizationManager == null) return;
            
            // Обновление заголовков настроек
            masterVolumeText.text = _localizationManager.MasterVolume;
            soundsVolumeText.text = _localizationManager.SoundVolume;
            musicVolumeText.text = _localizationManager.MusicVolume;
            mouseSensitivityText.text = _localizationManager.MouseSensitivity;
            brightnessText.text = _localizationManager.Brightness;
            frameLimitText.text = _localizationManager.FrameLimit;
            fpsCounterText.text = _localizationManager.FpsCounter;
            vSyncText.text = _localizationManager.VSync;
            backText.text = _localizationManager.Back;

            // Обновление текстов состояний (Toggle/FPS Lock)
            bool isVsyncOn = _settingsService.SavedVsync;
            bool isFpsCounterOn = _settingsService.SavedFpsCounter;
            
            _vsyncValue.text = isVsyncOn ? _localizationManager.ToggleOn : _localizationManager.ToggleOff;
            _fpsCounterValue.text = isFpsCounterOn ? _localizationManager.ToggleOn : _localizationManager.ToggleOff;
            
            // Обновление FPS Lock текста
            _fpsLockValue.text = isVsyncOn 
                ? "VSync" 
                : (_settingsService.SavedMaxFpsLock < _fpsLockSlider.minValue + 1 
                    ? _localizationManager.ToggleOff 
                    : $"{_settingsService.SavedMaxFpsLock}");
        }

        private void OnDestroy()
        {
            // ... (удаление слушателей)
            OnClose -= _settingsService.SaveSettings;

            _back.onClick.RemoveAllListeners();
            _masterVolumeSlider.onValueChanged.RemoveAllListeners();
            _soundsVolumeSlider.onValueChanged.RemoveAllListeners();
            _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            _mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
            _fpsLockSlider.onValueChanged.RemoveAllListeners();
            _fpsCounterToggle.onValueChanged.RemoveAllListeners();
            _vsyncToggle.onValueChanged.RemoveAllListeners();
            _brightnessSlider.onValueChanged.RemoveAllListeners();
            _resolutionDropdown.onValueChanged.RemoveAllListeners();
        }
    }
}