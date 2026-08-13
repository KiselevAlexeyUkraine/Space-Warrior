using System;
using Components.Player;
using Components.Player.Localization;
using Servise;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

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
        private float _flyUpDistance = 30f;
        
        [SerializeField] [Tooltip("Скорость полета вверх (юнитов в секунду).")] 
        private float _flySpeed = 15f;

        [SerializeField] [Tooltip("Кривая ускорения взлета.")] 
        private AnimationCurve _flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Player Continuous Random Rotation (Triggers After Arrival)")]
        [SerializeField] [Tooltip("Ось вращения корабля (например, (0, 1, 0) - ось Y, (0, 0, 1) - Z).")]
        private Vector3 _rotationAxis = Vector3.up;

        [SerializeField] [Tooltip("Минимальная скорость вращения (градусов в секунду).")]
        private float _minRotationSpeed = 180f;

        [SerializeField] [Tooltip("Максимальная скорость вращения (градусов в секунду).")]
        private float _maxRotationSpeed = 720f;

        [SerializeField] [Tooltip("Частота плавного изменения скорости со временем.")]
        private float _speedChangeFrequency = 1.5f;

        [Header("Camera FOV Zoom")]
        [SerializeField] [Tooltip("Целевой FOV камеры в конце анимации взлета.")]
        private float _targetFov = 85f;

        [SerializeField] [Tooltip("Кривая изменения FOV камеры по мере взлета.")]
        private AnimationCurve _fovCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private SceneService _sceneService;
        private LocalizationManager _localizationManager;
        private PlayerComponent _playerComponent;
        private AnimatorService _animatorService;
        private DiContainer _container; 

        private bool _isSpinning = false;
        private float _randomSeed;

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

                if (_playerComponent != null)
                {
                    // 1. Выключаем ограничения границ экрана
                    if (_playerComponent.BoundsCheck != null)
                    {
                        _playerComponent.BoundsCheck.enabled = false;
                        Debug.Log("<color=yellow>[CompletePage] BoundsCheck выключен.</color>");
                    }

                    // 2. Включаем победную Cinemachine камеру и запоминаем стартовый FOV
                    float initialFov = 60f;
                    var winCam = _playerComponent.WinCinemachineCamera;

                    if (winCam != null)
                    {
                        winCam.gameObject.SetActive(true);
                        initialFov = winCam.Lens.FieldOfView;
                        Debug.Log("<color=cyan>[CompletePage] Победная Cinemachine камера активирована!</color>");
                    }

                    // Сбрасываем флаг вращения перед стартом взлета
                    _isSpinning = false;

                    // 3. Запуск взлета и изменения FOV
                    if (_animatorService != null)
                    {
                        float safeSpeed = Mathf.Max(_flySpeed, 0.1f);
                        float duration = _flyUpDistance / safeSpeed;

                        Debug.Log($"<color=cyan>🚀 [CompletePage] Старт полёта на расстояние {_flyUpDistance} со скоростью {_flySpeed} (длительность: {duration:F2}с) 🚀</color>");

                        _animatorService.AnimatePlayerFlyUpCustom(
                            _playerComponent.transform, 
                            _flyUpDistance, 
                            duration, 
                            _flyCurve,
                            onUpdateProgress: (progress) =>
                            {
                                // Плавное изменение FOV камеры по мере прямолинейного взлета
                                if (winCam != null)
                                {
                                    float fovProgress = _fovCurve != null ? _fovCurve.Evaluate(progress) : progress;
                                    float currentFov = Mathf.Lerp(initialFov, _targetFov, fovProgress);
                                    
                                    var lens = winCam.Lens;
                                    lens.FieldOfView = currentFov;
                                    winCam.Lens = lens;
                                }
                            },
                            onComplete: () =>
                            {
                                Debug.Log("<color=green>🏁 [CompletePage] Корабль прилетел в конечную точку! Запускаем вращение... 🏁</color>");
                                
                                // ТОЛЬКО ПОСЛЕ ПРИЛЕТА ЗАПУСКАЕМ ВРАЩЕНИЕ
                                _randomSeed = UnityEngine.Random.Range(0f, 100f);
                                _isSpinning = true;
                            }
                        );
                    }
                }
                else
                {
                    Debug.LogWarning("[CompletePage] _playerComponent равен null.");
                }
            };
        }

        private void Update()
        {
            // Начинает вращаться строго после прилета (когда _isSpinning равен true)
            if (_isSpinning && _playerComponent != null)
            {
                float noise = Mathf.PerlinNoise(_randomSeed, Time.unscaledTime * _speedChangeFrequency);
                float currentSpeed = Mathf.Lerp(_minRotationSpeed, _maxRotationSpeed, noise);

                _playerComponent.transform.Rotate(_rotationAxis.normalized, currentSpeed * Time.unscaledDeltaTime, Space.Self);
            }
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
            _isSpinning = false;
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