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
        
        [Header("Level System UI (New)")]
        [SerializeField] private TextMeshProUGUI levelText;      
        [SerializeField] private Image levelProgressImage;      

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
                _playerScore.OnLevelChanged += UpdateLevelText;
                _playerScore.OnProgressChanged += UpdateProgressFill;
                
                UpdateScoreText(_playerScore.CurrentScore);
                UpdateLevelText(_playerScore.CurrentLevel);
                
                _playerScore.AddScore(0); 
            }
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
            
            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged -= UpdateScoreText;
                _playerScore.OnLevelChanged -= UpdateLevelText;
                _playerScore.OnProgressChanged -= UpdateProgressFill;
            }
        }

        private void UpdateScoreText(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{newScore}";
            }
        }

        private void UpdateLevelText(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"{newLevel}";
            }
        }

        private void UpdateProgressFill(float fillAmount)
        {
            if (levelProgressImage != null)
            {
                levelProgressImage.fillAmount = fillAmount;
            }
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();
    }
}