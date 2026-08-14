using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Components.Player;
using Zenject;
using System;

namespace Components.Ui.Pages.Game
{
    public sealed class StatsPage : BasePage
    {
        [Header("Status UI Elements")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI scoreText;  // Текст текущего счета
        [SerializeField] private TextMeshProUGUI shieldText; // Текст для отображения уровня щита (S x N)

        private PauseService _pauseService;
        private PageSwitcher _pageSwitcher;
        private PlayerScore _playerScore;
        private PlayerCollect _playerCollect;
        private DiContainer _container;

        [Inject]
        private void Construct(
            PauseService pauseService, 
            PageSwitcher pageSwitcher, 
            PlayerScore playerScore,
            DiContainer container,
            [InjectOptional] PlayerCollect playerCollect)
        {
            _pauseService = pauseService;
            _pageSwitcher = pageSwitcher;
            _playerScore = playerScore;
            _container = container;
            _playerCollect = playerCollect;

            OnOpen += () => 
            { 
                _pauseService.Play(); 
                _playerScore?.ResetScore();

                // Если PlayerCollect не был передан при создании, пытаемся разрешить его через DiContainer
                if (_playerCollect == null && _container != null)
                {
                    try
                    {
                        _playerCollect = _container.Resolve<PlayerCollect>();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[StatsPage] Не удалось извлечь PlayerCollect через DiContainer: {e.Message}");
                    }
                }

                // Подписываемся на события и обновляем интерфейс при каждом открытии
                SubscribeShieldEvents();
                UpdateShieldText(_playerCollect != null ? Mathf.Max(0, (int)_playerCollect.ShieldLevel) : 0);
            };

            OnClose += () => 
            {
                UnsubscribeShieldEvents();
                _pauseService.Pause();
            };
        }

        private void Start()
        {
            pauseButton?.onClick.AddListener(OnPauseClicked);

            if (_playerScore != null)
            {
                _playerScore.OnScoreChanged += UpdateScoreText;
                UpdateScoreText(_playerScore.CurrentScore);
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
            }

            UnsubscribeShieldEvents();
        }

        private void SubscribeShieldEvents()
        {
            UnsubscribeShieldEvents(); // Предотвращаем дублирование подписок

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

        private void UpdateScoreText(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{newScore}";
            }
        }

        private void UpdateShieldText(int currentShield)
        {
            if (shieldText != null)
            {
                shieldText.text = $"S x {currentShield}";
            }
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();
    }
}