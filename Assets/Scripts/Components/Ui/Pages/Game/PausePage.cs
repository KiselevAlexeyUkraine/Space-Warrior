using UnityEngine;
using UnityEngine.UI;
using Components.Player.Localization;
using Components.Player;
using Zenject;
using TMPro;

namespace Components.Ui.Pages.Game
{
    public class PausePage : BasePage
    {
        [SerializeField] private TMP_Text _pauseTitle;
        [SerializeField] private Button _continue;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _exit;
        
        private LocalizationManager _localizationManager;
        private PlayerScore _playerScore;
        
        [Inject]
        private void Construct(
            LocalizationManager localizationManager,
            [InjectOptional] PlayerScore playerScore)
        {
            _localizationManager = localizationManager;
            _playerScore = playerScore;
        }

        private void Awake()
        {
            // Продолжить игру — ресурсы НЕ сбрасываем
            _continue.onClick.AddListener(() => 
            { 
                PageSwitcher.Open(PageName.Stats).Forget(); 
            });

            // Начать заново — сбрасываем ресурсы
            _restart.onClick.AddListener(() => 
            { 
                _playerScore?.ResetScore();
                PageSwitcher.Open(PageName.GameRestart).Forget(); 
            });

            // Настройки — ресурсы НЕ сбрасываем
            _settings.onClick.AddListener(() => 
            { 
                PageSwitcher.Open(PageName.GameSettings).Forget(); 
            });

            // Выход в главное меню — сбрасываем ресурсы
            _exit.onClick.AddListener(() =>
            {
                _playerScore?.ResetScore();
                PageSwitcher.Open(PageName.GameExit).Forget();
            });

            ButtonTextsUpdate();
        }

        private void OnDestroy()
        {
            _continue.onClick.RemoveAllListeners();
            _restart.onClick.RemoveAllListeners();
            _settings.onClick.RemoveAllListeners();
            _exit.onClick.RemoveAllListeners();
        }
        
        private void ButtonTextsUpdate()
        {
            if (_pauseTitle != null) 
                _pauseTitle.text = _localizationManager.TitlePause;

            if (_continue != null && _continue.GetComponentInChildren<TMP_Text>() != null)
                _continue.GetComponentInChildren<TMP_Text>().text = _localizationManager.Resume;

            if (_restart != null && _restart.GetComponentInChildren<TMP_Text>() != null)
                _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;

            if (_settings != null && _settings.GetComponentInChildren<TMP_Text>() != null)
                _settings.GetComponentInChildren<TMP_Text>().text = _localizationManager.Settings;

            if (_exit != null && _exit.GetComponentInChildren<TMP_Text>() != null)
                _exit.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;
        }
    }
}