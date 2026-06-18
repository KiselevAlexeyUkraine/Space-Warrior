using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Components.Player;
using Zenject;

namespace Components.Ui.Pages.Game
{
    public sealed class StatsPage : BasePage
    {
        [Header("Status UI Elements")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI scoreText;

        private PauseService _pauseService;
        private PageSwitcher _pageSwitcher;
        private PlayerScore _playerScore;

        [Inject]
        private void Construct(PauseService pauseService, PageSwitcher pageSwitcher, PlayerScore playerScore)
        {
            _pauseService = pauseService;
            _pageSwitcher = pageSwitcher;
            _playerScore = playerScore;

            OnOpen += () => { _pauseService.Play(); };
            OnClose += () => _pauseService.Pause();
        }

        private void Start()
        {
            pauseButton?.onClick.AddListener(OnPauseClicked);
            
            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged += UpdateScoreText;
                
                UpdateScoreText(_playerScore.CurrentScore);
            }
        }

        private void OnDestroy()
        {
            pauseButton?.onClick.RemoveListener(OnPauseClicked);
            
            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged -= UpdateScoreText;
            }
        }

        private void UpdateScoreText(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{newScore}";
            }
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();
    }
}