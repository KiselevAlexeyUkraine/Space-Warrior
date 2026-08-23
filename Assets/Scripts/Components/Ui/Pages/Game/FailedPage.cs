using Components.Player;
using UnityEngine;
using UnityEngine.UI;
using Components.Player.Localization;
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
        private PlayerScore _playerScore;

        [Inject]
        private void Construct(
            PauseService pauseService,
            PauseSwitcher pauseSwitcher,
            LocalizationManager localizationManager,
            [InjectOptional] PlayerScore playerScore)
        {
            _pauseService = pauseService;
            _pauseSwitcher = pauseSwitcher;
            _localizationManager = localizationManager;
            _playerScore = playerScore;

            OnOpen += () =>
            {
                _pauseSwitcher.enabled = false;
                _pauseService.Pause();

                // Сбрасываем ресурсы при смерти игрока
                _playerScore?.ResetScore();
            };

            OnOpenWithParam += ShowFailureUI;
        }

        private void Awake()
        {
            TextsUpdate();
            _restart.onClick.AddListener(() =>
            {
                _playerScore?.ResetScore();
                PageSwitcher.Open(PageName.GameRestart).Forget();
            });

            _menu.onClick.AddListener(() =>
            {
                _playerScore?.ResetScore();
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
            if (_failureText != null)
                _failureText.text = _localizationManager.TextFaild;
            
            if (_restart != null && _restart.GetComponentInChildren<TMP_Text>() != null)
                _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;

            if (_menu != null && _menu.GetComponentInChildren<TMP_Text>() != null)
                _menu.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;
        }
    }
}