using UnityEngine;
using UnityEngine.UI;
using Components.Player.Localization;
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
        
        [Inject]
        private void Construct(LocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        private void Awake()
        {
            _continue.onClick.AddListener(() => { PageSwitcher.Open(PageName.Stats).Forget(); });
            _restart.onClick.AddListener(() => { PageSwitcher.Open(PageName.GameRestart).Forget(); });
            _settings.onClick.AddListener(() => { PageSwitcher.Open(PageName.GameSettings).Forget(); });
            _exit.onClick.AddListener(() =>
            {
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
            _pauseTitle.text = _localizationManager.TitlePause;
            _continue.GetComponentInChildren<TMP_Text>().text = _localizationManager.Resume;
            _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;
            _settings.GetComponentInChildren<TMP_Text>().text = _localizationManager.Settings;
            _exit.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;
        }
    }
}