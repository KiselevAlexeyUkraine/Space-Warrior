using Components.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Components.Player.Localization;
using Servise; 
using System;
using Cysharp.Threading.Tasks;
using Components.Enemy;

namespace Components.Ui.Pages.Game
{
    public class CompletePage : BasePage
    {
        [SerializeField] private TMP_Text _tile;
        [SerializeField] private TMP_Text _totalScoreText; 
        [SerializeField] private Button _nextLevel;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _menu;

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

        [Header("===== Reset System Elements =====")]
        [SerializeField] private Button _resetProgressButton;

        [Header("===== Score Animation Settings =====")]
        [SerializeField] private float _scoreCountDuration = 1.0f;

        [Header("===== Player Fly Up Animation =====")]
        [SerializeField] private float _flyUpDistance = 20f;
        [SerializeField] private float _flyDuration = 1.5f;
        [SerializeField] private AnimationCurve _flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private SceneService _sceneService;
        private LocalizationManager _localizationManager;
        private PlayerComponent _playerComponent;
        private AnimatorService _animatorService;
        private PlayerScore _playerScore;
        private BalanceConfig _balanceConfig;
        private DiContainer _container; 

        [Inject]
        private void Construct(
            PauseService pauseService, 
            PauseSwitcher pauseSwitcher, 
            SceneService sceneService, 
            LocalizationManager localizationManager,
            AnimatorService animatorService,
            PlayerScore playerScore,
            BalanceConfig balanceConfig,
            DiContainer container) 
        {
            _sceneService = sceneService;
            _localizationManager = localizationManager;
            _animatorService = animatorService;
            _playerScore = playerScore;
            _balanceConfig = balanceConfig;
            _container = container; 
            
            OnOpen += () =>
            {
                pauseSwitcher.enabled = false;
                pauseService.Pause();
                TextsUpdate();

                // Сохраняем заработанные ресурсы/очки и запускаем анимацию подсчета
                if (_playerScore != null)
                {
                    AnimateAndSaveScoreAsync(_playerScore).Forget();
                }

                if (_playerComponent == null && _container != null)
                {
                    try
                    {
                        _playerComponent = _container.Resolve<PlayerComponent>();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CompletePage] Не удалось извлечь PlayerComponent: {e.Message}");
                    }
                }

                if (_playerComponent != null && _animatorService != null)
                {
                    _animatorService.AnimatePlayerFlyUp(
                        _playerComponent.transform, 
                        _flyUpDistance, 
                        _flyDuration, 
                        _flyCurve,
                        () => {}
                    );
                }
            };
        }

        private void Awake()
        {
            _nextLevel.onClick.AddListener(() => PageSwitcher.Open(PageName.NextLevel).Forget());
            _restart.onClick.AddListener(() => PageSwitcher.Open(PageName.GameRestart).Forget());
            _menu.onClick.AddListener(() => PageSwitcher.Open(PageName.GameExit).Forget());
            
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (_resetProgressButton != null)
            {
                _resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            }

            int currentSceneIndex = _sceneService.GetCurrentScene();
            int totalScenes = _sceneService.GetScenesCount();

            bool isLastLevel = currentSceneIndex >= totalScenes - 1;

            if (isLastLevel)
            {
                _tile.text = _localizationManager.TextWinGame;
                _nextLevel.interactable = false;
                _nextLevel.gameObject.SetActive(false);
            }
            else
            {
                _tile.text = _localizationManager.TextWinLevel;
                _nextLevel.interactable = true;
                _nextLevel.gameObject.SetActive(true);
            }
        }

        private async UniTaskVoid AnimateAndSaveScoreAsync(PlayerScore playerScore)
        {
            int oldTotal = playerScore.TotalSavedScore;
            int levelScore = playerScore.CurrentScore;
            int targetTotal = oldTotal + levelScore;

            // Переносит текущие полученные ресурсы и очки за уровень в общий сохранённый банк
            playerScore.SaveLevelScore();

            if (_totalScoreText != null)
            {
                if (levelScore <= 0)
                {
                    _totalScoreText.text = $"$ {targetTotal}";
                }
                else
                {
                    float elapsedTime = 0f;
                    while (elapsedTime < _scoreCountDuration)
                    {
                        elapsedTime += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(elapsedTime / _scoreCountDuration);
                        
                        int currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(oldTotal, targetTotal, t));
                        _totalScoreText.text = $"$ {currentDisplayedScore}";

                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }
                    _totalScoreText.text = $"$ {targetTotal}";
                }
            }

            UpdateUpgradeUI();
        }

        private void OnUpgradeClicked()
        {
            if (_balanceConfig == null || _playerScore == null) return;

            int nextLevel = _playerScore.CurrentLevel + 1;

            // Получаем полные требования следующего уровня (очки + ресурсы)
            if (_balanceConfig.TryGetUpgradeRequirement(nextLevel, out var req))
            {
                if (_playerScore.TrySpendRequirement(req))
                {
                    if (_totalScoreText != null)
                    {
                        _totalScoreText.text = $"$ {_playerScore.TotalSavedScore}";
                    }

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

            if (_totalScoreText != null)
            {
                _totalScoreText.text = "$ 0";
            }

            UpdateUpgradeUI();
        }

        private void UpdateUpgradeUI()
        {
            if (_balanceConfig == null || _playerScore == null) return;

            int currentLvl = _playerScore.CurrentLevel;
            int nextLvl = currentLvl + 1;

            // Обновляем счетчики всех ресурсов в UI
            if (_ironText != null) _ironText.text = $"I x {_playerScore.GetTotalResourceAmount(ResourceType.Iron)}";
            if (_goldText != null) _goldText.text = $"G x {_playerScore.GetTotalResourceAmount(ResourceType.Gold)}";
            if (_crystalsText != null) _crystalsText.text = $"C x {_playerScore.GetTotalResourceAmount(ResourceType.Crystals)}";
            if (_titaniumText != null) _titaniumText.text = $"T x {_playerScore.GetTotalResourceAmount(ResourceType.Titanium)}";

            if (_currentStatsText != null)
            {
                _currentStatsText.text = GetStatsDescription(currentLvl);
            }

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
                    // Проверяем возможность покупки
                    bool canAfford = _playerScore.CanAffordRequirement(req);
                    _upgradeButton.interactable = canAfford;

                    if (_upgradeButtonText != null)
                    {
                        string actionText = _localizationManager != null ? _localizationManager.UpgradeAction : "Upgrade";

                        // Формируем список ресурсов с проверкой > 0
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

            bool isEnglish = _localizationManager != null && _localizationManager.CurrentLanguage == LocalizationManager.Language.English;
            return isEnglish ? $"Level {level}" : $"Уровень {level}";
        }

        private void OnDestroy()
        {
            _nextLevel.onClick.RemoveAllListeners();
            _restart.onClick.RemoveAllListeners();
            _menu.onClick.RemoveAllListeners();
            if (_upgradeButton != null) _upgradeButton.onClick.RemoveAllListeners();
            if (_resetProgressButton != null) _resetProgressButton.onClick.RemoveAllListeners();
        }
        
        private void TextsUpdate()
        {
            _nextLevel.GetComponentInChildren<TMP_Text>().text = _localizationManager.NextLevel;
            _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;
            _menu.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;

            UpdateUpgradeUI();
        }
    }
}