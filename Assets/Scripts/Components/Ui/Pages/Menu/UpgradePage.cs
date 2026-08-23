using Components.Enemy;
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

        [Header("===== Resource UI Elements =====")]
        [SerializeField] private TMP_Text _ironText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _crystalsText;
        [SerializeField] private TMP_Text _titaniumText;

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
            if (_upgradeButton != null) _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
            if (_resetProgressButton != null) _resetProgressButton.onClick.AddListener(OnResetProgressClicked);
        }

        private void OnEnable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnTotalScoreChanged += OnTotalScoreChanged;
                _playerScore.OnLevelChanged += OnLevelChanged;
                _playerScore.OnResourcesChanged += UpdateUpgradeUI;
            }

            UpdateUpgradeUI();
        }

        private void OnDisable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnTotalScoreChanged -= OnTotalScoreChanged;
                _playerScore.OnLevelChanged -= OnLevelChanged;
                _playerScore.OnResourcesChanged -= UpdateUpgradeUI;
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
            
            if (_balanceConfig.TryGetUpgradeRequirement(nextLevel, out var req))
            {
                if (_playerScore.TrySpendRequirement(req))
                {
                    UpdateUpgradeUI();
                }
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

        private void OnTotalScoreChanged(int newTotalScore) => UpdateUpgradeUI();
        private void OnLevelChanged(int newLevel) => UpdateUpgradeUI();

        private void UpdateUpgradeUI()
        {
            if (_balanceConfig == null || _playerScore == null) return;

            if (_backButtonText != null && _localizationManager != null)
            {
                _backButtonText.text = _localizationManager.Back;
            }

            int currentLvl = _playerScore.CurrentLevel;
            int nextLvl = currentLvl + 1;

            if (_totalScoreText != null) _totalScoreText.text = $"$ {_playerScore.TotalSavedScore}";

            if (_ironText != null) _ironText.text = $"I x {_playerScore.GetTotalResourceAmount(ResourceType.Iron)}";
            if (_goldText != null) _goldText.text = $"G x {_playerScore.GetTotalResourceAmount(ResourceType.Gold)}";
            if (_crystalsText != null) _crystalsText.text = $"C x {_playerScore.GetTotalResourceAmount(ResourceType.Crystals)}";
            if (_titaniumText != null) _titaniumText.text = $"T x {_playerScore.GetTotalResourceAmount(ResourceType.Titanium)}";

            if (_currentStatsText != null) _currentStatsText.text = GetStatsDescription(currentLvl);

            bool hasRequirement = _balanceConfig.TryGetUpgradeRequirement(nextLvl, out var req);

            if (_nextStatsText != null)
            {
                _nextStatsText.text = !hasRequirement ? "MAX LEVEL" : GetStatsDescription(nextLvl);
            }

            if (_upgradeButton != null)
            {
                if (!hasRequirement)
                {
                    _upgradeButton.interactable = false;
                    if (_upgradeButtonText != null) _upgradeButtonText.text = "MAX";
                }
                else
                {
                    bool canAfford = _playerScore.CanAffordRequirement(req);
                    _upgradeButton.interactable = canAfford;

                    if (_upgradeButtonText != null)
                    {
                        string actionText = _localizationManager != null ? _localizationManager.UpgradeAction : "Upgrade";

                        string reqResStr = "";
                        if (req.requiredResources != null)
                        {
                            foreach (var res in req.requiredResources)
                            {
                                if (res.amount > 0)
                                {
                                    char resourceLetter = res.resourceType.ToString()[0];
                                    reqResStr += $" | {resourceLetter}:{res.amount}";
                                }
                            }
                        }

                        _upgradeButtonText.text = $"{actionText} (${req.scoreCost}{reqResStr})";
                    }
                }
            }
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