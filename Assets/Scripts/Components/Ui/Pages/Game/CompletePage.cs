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
        private PlayerMove _playerMove;
        private PlayerCollect _playerCollect;
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
            DiContainer container,
            [InjectOptional] PlayerMove playerMove,
            [InjectOptional] PlayerCollect playerCollect) 
        {
            _sceneService = sceneService;
            _localizationManager = localizationManager;
            _animatorService = animatorService;
            _container = container; 
            _playerMove = playerMove;
            _playerCollect = playerCollect;

            OnOpen += () =>
            {
                pauseSwitcher.enabled = false;
                
                // Вызываем прохождение уровня (без остановки Time.timeScale)
                pauseService.LevelComplete();
                
                TextsUpdate();

                // Извлекаем PlayerComponent при необходимости
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

                // Пытаемся дополучить Move/Collect, если они не заинжектились сразу
                if (_playerMove == null && _container != null)
                {
                    try { _playerMove = _container.Resolve<PlayerMove>(); } catch { }
                }

                if (_playerCollect == null && _container != null)
                {
                    try { _playerCollect = _container.Resolve<PlayerCollect>(); } catch { }
                }

                // -------------------------------------------------------------
                // 1. ОТКЛЮЧЕНИЕ УПРАВЛЕНИЯ, СБОРА И ОРУЖИЯ
                // -------------------------------------------------------------
                if (_playerMove != null)
                {
                    _playerMove.enabled = false;
                    Debug.Log("<color=orange>[CompletePage] PlayerMove выключен.</color>");
                }

                if (_playerCollect != null)
                {
                    _playerCollect.enabled = false;
                    Debug.Log("<color=orange>[CompletePage] PlayerCollect выключен.</color>");
                }

                if (_playerComponent != null)
                {
                    // Отключаем BoundsCheck
                    if (_playerComponent.BoundsCheck != null)
                    {
                        _playerComponent.BoundsCheck.enabled = false;
                        Debug.Log("<color=yellow>[CompletePage] BoundsCheck выключен.</color>");
                    }

                    // Отключаем все компоненты стрельбы/оружия, прикрепленные к игроку
                    DisablePlayerWeapons(_playerComponent.gameObject);

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

        /// <summary>
        /// Находит и деактивирует все объекты или скрипты оружия у игрока.
        /// </summary>
        private void DisablePlayerWeapons(GameObject playerGo)
        {
            if (playerGo == null) return;

            // Отключаем все скрипты, содержащие "Weapon", "Gun", "Shoot" или "Attack" в имени класса
            var allMonoBehaviours = playerGo.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in allMonoBehaviours)
            {
                if (script == null) continue;

                string typeName = script.GetType().Name.ToLower();
                if (typeName.Contains("weapon") || typeName.Contains("gun") || typeName.Contains("shoot") || typeName.Contains("attack"))
                {
                    script.enabled = false;
                    Debug.Log($"<color=red>[CompletePage] Скрипт оружия {script.GetType().Name} деактивирован.</color>");
                }
            }

            // Дополнительно ищем дочерние объекты-контейнеры оружия и выключаем SetActive(false)
            foreach (Transform child in playerGo.GetComponentsInChildren<Transform>(true))
            {
                string name = child.name.ToLower();
                if (name.Contains("weapon") || name.Contains("gun") || name.Contains("armament"))
                {
                    child.gameObject.SetActive(false);
                    Debug.Log($"<color=red>[CompletePage] Объект оружия {child.name} деактивирован.</color>");
                }
            }
        }

        private void Update()
        {
            // Начинает вращаться строго после прилета (когда _isSpinning равен true)
            if (_isSpinning && _playerComponent != null)
            {
                float noise = Mathf.PerlinNoise(_randomSeed, Time.time * _speedChangeFrequency);
                float currentSpeed = Mathf.Lerp(_minRotationSpeed, _maxRotationSpeed, noise);

                _playerComponent.transform.Rotate(_rotationAxis.normalized, currentSpeed * Time.deltaTime, Space.Self);
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