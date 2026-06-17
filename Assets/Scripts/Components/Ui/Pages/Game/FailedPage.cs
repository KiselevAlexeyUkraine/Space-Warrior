using Services;
using UnityEngine;
using UnityEngine.UI;
using Services.Localization;
using Zenject;
using TMPro;

namespace Components.Ui.Pages.Game
{
    public class FailedPage : BasePage
    {
        [SerializeField] private Button _restart;
        [SerializeField] private Button _menu;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _failureText;

        private PauseService _pauseService;
        private PauseSwitcher _pauseSwitcher;
        private LocalizationManager _localizationManager;

        [Inject]
        private void Construct(
            PauseService pauseService,
            PauseSwitcher pauseSwitcher,
            LocalizationManager localizationManager)
        {
            _pauseService = pauseService;
            _pauseSwitcher = pauseSwitcher;
            _localizationManager = localizationManager;

            OnOpen += () =>
            {
                _pauseSwitcher.enabled = false;
                _pauseService.Pause();
            };

            OnOpenWithParam += ShowFailureUI;
        }

        private void Awake()
        {
            TextsUpdate();
            _restart.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.GameRestart).Forget();
            });

            _menu.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.GameExit).Forget();
            });
        }

        private void OnDestroy()
        {
            _restart.onClick.RemoveAllListeners();
            _menu.onClick.RemoveAllListeners();

            OnOpenWithParam -= ShowFailureUI;
        }

        private void ShowFailureUI(int index)
        {
            TextsUpdate();

            if (_failureText != null)
            {
                _failureText.text = _localizationManager.TextFaild;
            }
        }
        

        private void TextsUpdate()
        {
            _failureText.text = _localizationManager.TextFaild;
            
            _restart.GetComponentInChildren<TMP_Text>().text =
                _localizationManager.Restart;

            _menu.GetComponentInChildren<TMP_Text>().text =
                _localizationManager.ToMainMenu;
        }
    }
}