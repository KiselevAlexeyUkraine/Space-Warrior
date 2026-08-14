using Components.Player;
using Components.Player.Localization;
using Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Components.Ui.Pages.Menu
{
    public sealed class UpgradePage : BasePage
    {
        [Header("===== Score & Level Display =====")]
        [SerializeField] private TMP_Text _totalScoreText;

        [Header("===== Upgrade System Elements =====")]
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _upgradeButtonText;
        [SerializeField] private TMP_Text _currentStatsText;
        [SerializeField] private TMP_Text _nextStatsText;

        [Header("===== Navigation & Reset =====")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _backButtonText;
        [SerializeField] private Button _resetProgressButton;

        private PlayerScore _playerScore;
        private BalanceConfig _balanceConfig;
        private LocalizationManager _localizationManager;

        [Inject]
        private void Construct(
            PlayerScore playerScore,
            BalanceConfig balanceConfig,
            LocalizationManager localizationManager)
        {
            _playerScore = playerScore;
            _balanceConfig = balanceConfig;
            _localizationManager = localizationManager;
        }

        private void Awake()
        {
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }

            if (_resetProgressButton != null)
            {
                _resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            }
        }

        private void OnEnable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnTotalScoreChanged += OnTotalScoreChanged;
                _playerScore.OnLevelChanged += OnLevelChanged;
            }

            UpdateUpgradeUI();
        }

        private void OnDisable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnTotalScoreChanged -= OnTotalScoreChanged;
                _playerScore.OnLevelChanged -= OnLevelChanged;
            }
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null) _upgradeButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
            if (_resetProgressButton != null) _resetProgressButton.onClick.RemoveAllListeners();
        }

        private void OnUpgradeClicked()
        {
            if (_balanceConfig == null || _playerScore == null) return;

            int nextLevel = _playerScore.CurrentLevel + 1;
            int upgradeCost = GetUpgradeCost(nextLevel);

            if (_playerScore.TrySpendTotalScore(upgradeCost))
            {
                UpdateUpgradeUI();
            }
        }

        private void OnResetProgressClicked()
        {
            if (_playerScore != null)
            {
                _playerScore.ResetAllProgress();
            }

            UpdateUpgradeUI();
        }

        private void OnBackClicked()
        {
            PageSwitcher.Open(PageName.Menu).Forget();
        }

        private void OnTotalScoreChanged(int newTotalScore)
        {
            UpdateUpgradeUI();
        }

        private void OnLevelChanged(int newLevel)
        {
            UpdateUpgradeUI();
        }

        private void UpdateUpgradeUI()
        {
            if (_balanceConfig == null || _playerScore == null) return;

            // Локализация кнопки "Назад"
            if (_backButtonText != null && _localizationManager != null)
            {
                _backButtonText.text = _localizationManager.Back;
            }

            int currentLvl = _playerScore.CurrentLevel;
            int nextLvl = currentLvl + 1;

            if (_totalScoreText != null)
            {
                _totalScoreText.text = _playerScore.TotalSavedScore.ToString();
            }

            if (_currentStatsText != null)
            {
                _currentStatsText.text = GetStatsDescription(currentLvl);
            }

            int cost = GetUpgradeCost(nextLvl);
            bool isMaxLevel = cost <= 0;

            if (_nextStatsText != null)
            {
                _nextStatsText.text = isMaxLevel ? "MAX LEVEL" : GetStatsDescription(nextLvl);
            }

            if (_upgradeButton != null)
            {
                if (isMaxLevel)
                {
                    _upgradeButton.interactable = false;
                    if (_upgradeButtonText != null) _upgradeButtonText.text = "MAX";
                }
                else
                {
                    bool canAfford = _playerScore.TotalSavedScore >= cost;
                    _upgradeButton.interactable = canAfford;

                    if (_upgradeButtonText != null)
                    {
                        string actionText = _localizationManager != null ? _localizationManager.UpgradeAction : "Upgrade";
                        _upgradeButtonText.text = $"{actionText} ({cost})";
                    }
                }
            }
        }

        private int GetUpgradeCost(int targetLevel)
        {
            if (_balanceConfig != null && _balanceConfig.TryGetUpgradeCost(targetLevel, out int cost))
            {
                return cost;
            }

            return targetLevel <= 10 ? (targetLevel - 1) * 2000 : 0;
        }

        private string GetStatsDescription(int level)
        {
            if (_balanceConfig != null && _balanceConfig.TryGetPlayerStatsForLevel(level, out float hp, out float shield, out float speed, out float fireRate))
            {
                bool isEng = _localizationManager != null && _localizationManager.CurrentLanguage == LocalizationManager.Language.English;
                
                string lvlText = isEng ? $"Lvl. {level}" : $"Ур. {level}";
                string shieldText = isEng ? "Shield" : "Щит";
                string speedText = isEng ? "Speed" : "Скорость";
                string fireRateText = isEng ? "Fire Rate" : "Темп стрельбы";

                return $"{lvlText}\nHP: {hp} | {shieldText}: {shield}\n{speedText}: {speed}\n{fireRateText}: {fireRate}";
            }

            return $"Level {level}";
        }
    }
}