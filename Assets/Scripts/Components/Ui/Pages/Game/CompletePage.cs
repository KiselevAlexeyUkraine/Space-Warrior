using Components.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Components.Player.Localization;
using Servise; 
using System;

namespace Components.Ui.Pages.Game
{
    public class CompletePage : BasePage
    {
        [SerializeField] private TMP_Text _tile;
        [SerializeField] private Button _nextLevel;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _menu;

        [Header("Player Fly Up Animation")]
        [SerializeField] [Tooltip("На сколько единиц вверх улетит игрок.")] 
        private float _flyUpDistance = 20f;
        
        [SerializeField] [Tooltip("Длительность анимации взлета.")] 
        private float _flyDuration = 1.5f;

        [SerializeField] [Tooltip("Кривая анимации взлета (например, резкий старт в конце).")] 
        private AnimationCurve _flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private SceneService _sceneService;
        private LocalizationManager _localizationManager;
        private PlayerComponent _playerComponent;
        private AnimatorService _animatorService;
        private DiContainer _container; 

        [Inject]
        private void Construct(
            PauseService pauseService, 
            PauseSwitcher pauseSwitcher, 
            SceneService sceneService, 
            LocalizationManager localizationManager,
            AnimatorService animatorService,
            DiContainer container) 
        {
            _sceneService = sceneService;
            _localizationManager = localizationManager;
            _animatorService = animatorService;
            _container = container; 
            
            OnOpen += () =>
            {
                pauseSwitcher.enabled = false;
                pauseService.Pause();
                TextsUpdate();

                // ИСПРАВЛЕНИЕ: Извлекаем игрока ПРЯМО ЗДЕСЬ, так как Start() не выполнится при Time.timeScale = 0
                if (_playerComponent == null && _container != null)
                {
                    try
                    {
                        _playerComponent = _container.Resolve<PlayerComponent>();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CompletePage] Не удалось извлечь PlayerComponent при открытии страницы: {e.Message}");
                    }
                }

                // Запуск анимации взлета при открытии страницы победы
                if (_playerComponent != null && _animatorService != null)
                {
                    Debug.Log("<color=cyan>🚀 [CompletePage] Инициация взлета. Отправляем запрос в AnimatorService... 🚀</color>");
                    
                    _animatorService.AnimatePlayerFlyUp(
                        _playerComponent.transform, 
                        _flyUpDistance, 
                        _flyDuration, 
                        _flyCurve,
                        () => Debug.Log("<color=green>🏁 [CompletePage] Финальная анимация взлета успешно ЗАВЕРШЕНА! Игрок на орбите. 🏁</color>")
                    );
                }
                else
                {
                    Debug.LogWarning("[CompletePage] Не удалось запустить анимацию взлета: _playerComponent или _animatorService равен null.");
                }
            };
        }

        private void Awake()
        {
            _nextLevel.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.NextLevel).Forget();
            });

            _restart.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.GameRestart).Forget();
            });

            _menu.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.GameExit).Forget();
            });
            

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

        private void Start()
        {
            // Резервное извлечение, если страница просто инициализируется заранее
            if (_playerComponent == null && _container != null)
            {
                try
                {
                    _playerComponent = _container.Resolve<PlayerComponent>();
                }
                catch { }
            }
        }

        private void OnDestroy()
        {
            _nextLevel.onClick.RemoveAllListeners();
            _restart.onClick.RemoveAllListeners();
            _menu.onClick.RemoveAllListeners();
        }
        
        private void TextsUpdate()
        {
            _nextLevel.GetComponentInChildren<TMP_Text>().text = _localizationManager.NextLevel;
            _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;
            _menu.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;
        }
    }
}