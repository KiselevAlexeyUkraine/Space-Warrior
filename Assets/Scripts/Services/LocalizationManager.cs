using UnityEngine;
using Zenject;
using Configs.Localization;

namespace Components.Player.Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        private LocalizationConfigRus _russianConfig;
        private LocalizationConfigEng _englishConfig;
        private ILocalizationConfig _currentConfig;
        
        private SettingsService _settingsService;   

        // Главное меню
        private string gameTitle;
        private string choseLevel;
        private string startGame;
        private string upgrade;
        private string upgradeAction;
        private string authors;
        private string settings;
        private string exitGame;

        // Настройки — категории
        private string masterVolume;
        private string soundVolume;
        private string musicVolume;
        private string mouseSensitivity;
        private string brightness;
        private string frameLimit;
        private string fpsCounter;
        private string vSync;
        private string resolution;
        private string _languages;

        // Настройки — кнопки и состояния
        private string back;
        private string toggleOn;
        private string toggleOff;

        // Меню паузы
        private string titlePause;
        private string resume;
        private string restart;
        private string toMainMenu;

        // Диалоги и подтверждения
        private string correct;
        private string yes;
        private string no;

        // Авторы — роли
        private string unityDeveloper;
        private string artist;
        private string animator;
        private string soundDesigner;
        private string gameDesigner;
        
        // Причины смерти (для страницы поражения)
        private string deathByEnemy;
        private string deathByObstacle;
        private string textFaild;
        
        // Hud
        private string keys;
        
        // Победа
        private string _textWinLevelLevel;
        private string textWinGame;
        private string nextWinGame;
        

        #region Public Getters
        public string GameTitle => gameTitle;
        public string ChoseLevel => choseLevel;
        public string StartGame => startGame;
        public string Upgrade => upgrade;
        public string UpgradeAction => upgradeAction;
        public string Authors => authors;
        public string Settings => settings;
        public string ExitGame => exitGame;

        public string MasterVolume => masterVolume;
        public string SoundVolume => soundVolume;
        public string MusicVolume => musicVolume;
        public string MouseSensitivity => mouseSensitivity;
        public string Brightness => brightness;
        public string FrameLimit => frameLimit;
        public string FpsCounter => fpsCounter;
        public string VSync => vSync;
        public string Resolution => resolution;
        public string Languages => _languages;

        public string Back => back;
        public string ToggleOn => toggleOn;
        public string ToggleOff => toggleOff;

        public string TitlePause => titlePause;
        public string Resume => resume;
        public string Restart => restart;
        public string ToMainMenu => toMainMenu;

        public string Correct => correct;
        public string Yes => yes;
        public string No => no;

        // Авторы
        public string UnityDeveloper => unityDeveloper;
        public string Artist => artist;
        public string Animator => animator;
        public string SoundDesigner => soundDesigner;
        public string GameDesigner => gameDesigner;
        
        // Причины смерти
        public string DeathByEnemy => deathByEnemy;
        public string DeathByObstacle => deathByObstacle;
        
        public string TextFaild => textFaild;
        
        // HUD
        public string Keys => keys;
        
        // Победа
        public string TextWinLevel => _textWinLevelLevel;
        public string TextWinGame => textWinGame;
        public string NextLevel => nextWinGame;
        
        #endregion

        
        public enum Language
        {
            Russian,
            English
        }

        public Language CurrentLanguage { get; private set; } = Language.English;
        

        [Inject]
        public void Construct(LocalizationConfigRus russianConfig, LocalizationConfigEng englishConfig, SettingsService settingsService)
        {
            _russianConfig = russianConfig;
            _englishConfig = englishConfig;
            _settingsService = settingsService;
        }

        private void Start()
        {
            LoadSavedOrDetectSystemLanguage();
        }

        private void LoadSavedOrDetectSystemLanguage()
        {
            int savedLangIndex = _settingsService.SavedLanguageIndex;

            if (savedLangIndex != -1)
            {
                var savedLanguage = (Language)savedLangIndex;
                SetLanguage(savedLanguage);
                Debug.Log($"[Localization] Saved language found: {savedLanguage} → Loaded.");
                return;
            }

            var sysLang = Application.systemLanguage;

            switch (sysLang)
            {
                case SystemLanguage.Russian:
                case SystemLanguage.Ukrainian:
                case SystemLanguage.Belarusian:
                    SetLanguage(Language.Russian);
                    Debug.Log("[Localization] No save found. System language detected: Cyrillic region → Russian.");
                    break;

                default:
                    SetLanguage(Language.English);
                    Debug.Log("[Localization] No save found. System language detected: non-Cyrillic region → English.");
                    break;
            }
        }

        public void SetLanguage(Language language)
        {
            switch (language)
            {
                case Language.Russian:
                    _currentConfig = _russianConfig;
                    CurrentLanguage = Language.Russian;
                    break;

                case Language.English:
                    _currentConfig = _englishConfig;
                    CurrentLanguage = Language.English;
                    break;
            }

            UpdateLocalizedValues();

            Debug.Log($"[Localization] Switched language to: {CurrentLanguage}");
        }

        private void UpdateLocalizedValues()
        {
            if (_currentConfig == null)
            {
                Debug.LogWarning("[Localization] Current config is null — cannot update localized values.");
                return;
            }

            // Главное меню
            gameTitle = _currentConfig.GameTitle;
            choseLevel = _currentConfig.ChoseLevel;
            startGame = _currentConfig.StartGame;
            upgrade = _currentConfig.Upgrade;
            upgradeAction = _currentConfig.UpgradeAction;
            authors = _currentConfig.Authors;
            settings = _currentConfig.Settings;
            exitGame = _currentConfig.ExitGame;

            // Настройки
            masterVolume = _currentConfig.MasterVolume;
            soundVolume = _currentConfig.SoundVolume;
            musicVolume = _currentConfig.MusicVolume;
            mouseSensitivity = _currentConfig.MouseSensitivity;
            brightness = _currentConfig.Brightness;
            frameLimit = _currentConfig.FrameLimit;
            fpsCounter = _currentConfig.FpsCounter;
            vSync = _currentConfig.VSync;
            resolution = _currentConfig.Resolution;
            _languages = _currentConfig.Languages;

            // Кнопки и состояния
            back = _currentConfig.Back;
            toggleOn = _currentConfig.ToggleOn;
            toggleOff = _currentConfig.ToggleOff;

            // Меню паузы
            titlePause = _currentConfig.TitlePause;
            resume = _currentConfig.Resume;
            restart = _currentConfig.Restart;
            toMainMenu = _currentConfig.ToMainMenu;

            // Диалоги
            correct = _currentConfig.Correct;
            yes = _currentConfig.Yes;
            no = _currentConfig.No;

            // Авторы
            unityDeveloper = _currentConfig.UnityDeveloper;
            artist = _currentConfig.Artist;
            animator = _currentConfig.Animator;
            soundDesigner = _currentConfig.SoundDesigner;
            gameDesigner = _currentConfig.GameDesigner;
            
            // Причины смерти
            deathByEnemy = _currentConfig.DeathTitle1;
            deathByObstacle = _currentConfig.DeathTitle2;
            textFaild = _currentConfig.TextFaild;
            
            keys = _currentConfig.Keys;
            
            // Победа
            _textWinLevelLevel = _currentConfig.TextWin;
            textWinGame = _currentConfig.TextWinGame;
            nextWinGame = _currentConfig.NextLevel;
        }

        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == Language.Russian ? Language.English : Language.Russian);
        }

        public ILocalizationConfig Current => _currentConfig;
    }
}