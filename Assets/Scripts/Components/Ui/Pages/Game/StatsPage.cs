using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Components.Player;
using Components.Enemy;
using Zenject;
using System;
using Servise;
using Level;
using Cysharp.Threading.Tasks;

namespace Components.Ui.Pages.Game
{
    public sealed class StatsPage : BasePage
    {
        [Header("Status UI Elements")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI scoreText;  
        [SerializeField] private TextMeshProUGUI shieldText; 

        [Header("Wave Progress Elements")]
        [SerializeField] private Slider waveSlider;
        [SerializeField] private TextMeshProUGUI waveText;

        [Header("Resource UI Elements")]
        [SerializeField] private TextMeshProUGUI ironText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI crystalsText;
        [SerializeField] private TextMeshProUGUI titaniumText;

        [Header("Animation Scale Settings")]
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float animationDuration = 0.4f;

        [Header("Wave Animation Settings")]
        [SerializeField] private float waveAnimationDuration = 0.8f;
        [SerializeField] private float waveTextPunchScale = 1.5f;

        private PauseService _pauseService;
        private PageSwitcher _pageSwitcher;
        private PlayerScore _playerScore;
        private PlayerCollect _playerCollect;
        private AnimatorService _animatorService;
        private DiContainer _container;
        private GridFormationSpawner _spawner;

        // Кэшированные значения
        private int _lastScore = 0;
        private int _lastIron = 0;
        private int _lastGold = 0;
        private int _lastCrystals = 0;
        private int _lastTitanium = 0;
        private int _currentDisplayedWaveIndex = -1;

        [Inject]
        private void Construct(
            PauseService pauseService, 
            PageSwitcher pageSwitcher, 
            PlayerScore playerScore,
            AnimatorService animatorService,
            DiContainer container,
            [InjectOptional] PlayerCollect playerCollect,
            [InjectOptional] GridFormationSpawner spawner)
        {
            _pauseService = pauseService;
            _pageSwitcher = pageSwitcher;
            _playerScore = playerScore;
            _animatorService = animatorService;
            _container = container;
            _playerCollect = playerCollect;
            _spawner = spawner;

            OnOpen += () => 
            { 
                _pauseService.Play(); 

                if (_playerCollect == null && _container != null)
                {
                    try { _playerCollect = _container.Resolve<PlayerCollect>(); }
                    catch (Exception e) { Debug.LogWarning($"[StatsPage] Ошибка Resolve PlayerCollect: {e.Message}"); }
                }

                if (_spawner == null && _container != null)
                {
                    try { _spawner = _container.Resolve<GridFormationSpawner>(); }
                    catch (Exception e) { Debug.LogWarning($"[StatsPage] Ошибка Resolve GridFormationSpawner: {e.Message}"); }
                }

                SubscribeShieldEvents();
                SubscribeWaveEvents();

                UpdateShieldText(_playerCollect != null ? Mathf.Max(0, (int)_playerCollect.ShieldLevel) : 0);
                RefreshUIValues();

                if (_spawner != null)
                {
                    _currentDisplayedWaveIndex = _spawner.CurrentWaveIndex;
                    InitWaveSliderInstant(_currentDisplayedWaveIndex);
                }
            };

            OnClose += () => 
            {
                UnsubscribeShieldEvents();
                UnsubscribeWaveEvents();
                _pauseService.Pause();
            };
        }

        private void Start()
        {
            pauseButton?.onClick.AddListener(OnPauseClicked);

            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged += OnScoreUpdated;
                _playerScore.OnResourcesChanged += OnResourcesUpdated;
            }
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);

            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged -= OnScoreUpdated;
                _playerScore.OnResourcesChanged -= OnResourcesUpdated;
            }

            UnsubscribeShieldEvents();
            UnsubscribeWaveEvents();
        }

        private void SubscribeWaveEvents()
        {
            UnsubscribeWaveEvents();
            if (_spawner != null)
            {
                _spawner.OnWaveStarted += UpdateWaveProgress;
            }
        }

        private void UnsubscribeWaveEvents()
        {
            if (_spawner != null)
            {
                _spawner.OnWaveStarted -= UpdateWaveProgress;
            }
        }

        private void InitWaveSliderInstant(int currentWaveIndex)
        {
            if (_spawner == null) return;

            int totalWaves = _spawner.TotalWaves;
            int currentDisplayWave = currentWaveIndex + 1;

            if (waveSlider != null)
            {
                waveSlider.minValue = 0;
                waveSlider.maxValue = totalWaves;
                waveSlider.value = currentDisplayWave;
            }

            if (waveText != null)
            {
                waveText.text = currentDisplayWave.ToString();
            }
        }

        private void UpdateWaveProgress(int currentWaveIndex)
        {
            if (_spawner == null) return;

            // Запускаем асинхронную анимацию при смене волны
            AnimateWaveTransitionAsync(currentWaveIndex).Forget();
        }

        private async UniTaskVoid AnimateWaveTransitionAsync(int newWaveIndex)
        {
            if (_spawner == null) return;

            int totalWaves = _spawner.TotalWaves;
            int targetDisplayWave = newWaveIndex + 1;

            float startValue = waveSlider != null ? waveSlider.value : _currentDisplayedWaveIndex + 1;
            float targetValue = targetDisplayWave;

            if (waveSlider != null)
            {
                waveSlider.minValue = 0;
                waveSlider.maxValue = totalWaves;
            }

            // Анимация текста волны (Punch scale)
            if (waveText != null)
            {
                ResetElementTransform(waveText);
                waveText.text = targetDisplayWave.ToString();
                RebuildParentLayout(waveText);

                if (_animatorService != null)
                {
                    _animatorService.AnimatePunchScaleUI(waveText, waveAnimationDuration, waveTextPunchScale);
                }
            }

            // Плавно заполняем слайдер волны
            if (waveSlider != null)
            {
                float elapsedTime = 0f;
                while (elapsedTime < waveAnimationDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsedTime / waveAnimationDuration);

                    waveSlider.value = Mathf.Lerp(startValue, targetValue, t);

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                waveSlider.value = targetValue;
            }

            _currentDisplayedWaveIndex = newWaveIndex;
        }

        private void RefreshUIValues()
        {
            if (_playerScore != null)
            {
                _lastScore = _playerScore.CurrentScore;
                UpdateTextAndRebuild(scoreText, $"$ {_lastScore}");

                _lastIron = _playerScore.GetLevelResourceAmount(ResourceType.Iron);
                UpdateTextAndRebuild(ironText, $"I x {_lastIron}");

                _lastGold = _playerScore.GetLevelResourceAmount(ResourceType.Gold);
                UpdateTextAndRebuild(goldText, $"G x {_lastGold}");

                _lastCrystals = _playerScore.GetLevelResourceAmount(ResourceType.Crystals);
                UpdateTextAndRebuild(crystalsText, $"C x {_lastCrystals}");

                _lastTitanium = _playerScore.GetLevelResourceAmount(ResourceType.Titanium);
                UpdateTextAndRebuild(titaniumText, $"T x {_lastTitanium}");
            }
        }

        private void SubscribeShieldEvents()
        {
            UnsubscribeShieldEvents();
            if (_playerCollect != null)
            {
                _playerCollect.OnShieldLevelChanged += UpdateShieldText;
            }
        }

        private void UnsubscribeShieldEvents()
        {
            if (_playerCollect != null)
            {
                _playerCollect.OnShieldLevelChanged -= UpdateShieldText;
            }
        }

        private void OnScoreUpdated(int newScore)
        {
            if (scoreText == null) return;

            if (newScore > _lastScore && _animatorService != null)
            {
                _animatorService.AnimateCounterUI(scoreText, _lastScore, newScore, animationDuration, "$ ", punchScale);
            }
            else
            {
                UpdateTextAndRebuild(scoreText, $"$ {newScore}");
            }

            _lastScore = newScore;
        }

        private void OnResourcesUpdated()
        {
            if (_playerScore == null) return;

            CheckAndAnimateResource(ironText, "I", ResourceType.Iron, ref _lastIron);
            CheckAndAnimateResource(goldText, "G", ResourceType.Gold, ref _lastGold);
            CheckAndAnimateResource(crystalsText, "C", ResourceType.Crystals, ref _lastCrystals);
            CheckAndAnimateResource(titaniumText, "T", ResourceType.Titanium, ref _lastTitanium);
        }

        private void CheckAndAnimateResource(TextMeshProUGUI textElement, string prefix, ResourceType type, ref int lastAmount)
        {
            if (textElement == null) return;

            int currentAmount = _playerScore.GetLevelResourceAmount(type);
            
            ResetElementTransform(textElement);
            textElement.text = $"{prefix} x {currentAmount}";
            RebuildParentLayout(textElement);

            if (currentAmount > lastAmount && _animatorService != null)
            {
                _animatorService.AnimatePunchScaleUI(textElement, animationDuration, punchScale);
            }

            lastAmount = currentAmount;
        }

        private void UpdateShieldText(int currentShield)
        {
            if (shieldText != null)
            {
                ResetElementTransform(shieldText);
                shieldText.text = $"S x {currentShield}";
                RebuildParentLayout(shieldText);
            }
        }

        private void UpdateTextAndRebuild(TextMeshProUGUI textElement, string text)
        {
            if (textElement == null) return;
            ResetElementTransform(textElement);
            textElement.text = text;
            RebuildParentLayout(textElement);
        }

        private void ResetElementTransform(TextMeshProUGUI textElement)
        {
            textElement.rectTransform.localScale = Vector3.one;
        }

        private void RebuildParentLayout(TextMeshProUGUI textElement)
        {
            if (textElement.transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();
    }
}