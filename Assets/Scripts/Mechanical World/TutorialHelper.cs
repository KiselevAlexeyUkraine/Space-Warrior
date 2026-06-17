using UnityEngine;
using UnityEngine.UI;
using PrimeTween; // Заменили DG.Tweening
using Zenject;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MechanicalWorld
{
    public class TutorialHelper : MonoBehaviour
    {
        [Serializable]
        public struct ButtonProxy
        {
            public Button sourceButton;
            public Button targetButton;
        }

        [Header("Prefab Settings")]
        [SerializeField] private GameObject pointerPrefab;
        [SerializeField] private Vector3 pointerOffset = Vector3.zero;

        [Header("Button Forwarding")]
        [SerializeField] private List<ButtonProxy> buttonProxies = new List<ButtonProxy>();

        [Header("Interaction Feedback")]
        [SerializeField] private Button observedButton;
        [SerializeField] private GameObject feedbackObject;

        [Header("Settings")]
        [SerializeField] private int gameplayTutorialRepeats = 5;
        [SerializeField] private string locationTag = "Location";

        private QuestManager _questManager;
        private GameObject _activePointer;
        private Button _currentTrackedButton;
        
        private List<Button> _initialButtons = new List<Button>();
        private int _uiButtonIndex = 0;
        private int _gameplayClickCount = 0;
        private bool _isUiTutorialFinished = false;
        
        private bool _lastInteractableState;

        // --- ЛОГИКА "ОДНОГО РАЗА" ---
        private bool _isFeedbackDisabledForever = false;

        // Храним ссылку на твин задержки, чтобы можно было отменить его при выключении
        private Tween _delayTween;

        [Inject]
        private void Construct(QuestManager questManager)
        {
            _questManager = questManager;
        }

        private void Start()
        {
            // Прокси-кнопки
            foreach (var proxy in buttonProxies)
            {
                if (proxy.sourceButton != null && proxy.targetButton != null)
                {
                    proxy.sourceButton.onClick.AddListener(() => proxy.targetButton.onClick.Invoke());
                }
            }

            // Инициализация фидбека
            if (observedButton != null && feedbackObject != null)
            {
                observedButton.onClick.AddListener(() => {
                    _isFeedbackDisabledForever = true;
                    feedbackObject.SetActive(false); 
                });

                _lastInteractableState = observedButton.interactable;
                feedbackObject.SetActive(_lastInteractableState);
            }
        }

        private void Update()
        {
            if (_isFeedbackDisabledForever) return;

            if (observedButton != null && feedbackObject != null)
            {
                if (observedButton.interactable != _lastInteractableState)
                {
                    _lastInteractableState = observedButton.interactable;
                    feedbackObject.SetActive(_lastInteractableState);
                }
            }
        }

        private void OnEnable()
        {
            _uiButtonIndex = 0;
            _gameplayClickCount = 0;
            _isUiTutorialFinished = false;
            _initialButtons.Clear();
            InitializeUiSequence();
            if (_questManager != null) _questManager.OnStateChanged += RefreshTutorial;
            RefreshTutorial();
        }

        private void InitializeUiSequence()
        {
            GameObject uiRoot = GameObject.FindGameObjectWithTag("UI");
            if (uiRoot != null)
            {
                var allButtons = uiRoot.GetComponentsInChildren<Button>(true);
                Button help = allButtons.FirstOrDefault(b => b.name.ToLower().Contains("help"));
                Button interact = allButtons.FirstOrDefault(b => b.name.ToLower().Contains("interaction"));
                if (help != null) _initialButtons.Add(help);
                if (interact != null) _initialButtons.Add(interact);
            }
            if (_initialButtons.Count == 0) _isUiTutorialFinished = true;
        }

        private void RefreshTutorial()
        {
            if (_isUiTutorialFinished && _gameplayClickCount >= gameplayTutorialRepeats)
            {
                FinishTutorialSession();
                return;
            }

            Button nextTarget = null;
            if (!_isUiTutorialFinished)
            {
                if (_uiButtonIndex < _initialButtons.Count) nextTarget = _initialButtons[_uiButtonIndex];
                else { _isUiTutorialFinished = true; nextTarget = FindGameplayTarget(); }
            }
            else nextTarget = FindGameplayTarget();

            if (nextTarget != _currentTrackedButton) UpdatePointer(nextTarget);
        }

        private Button FindGameplayTarget()
        {
            Button target = _questManager.GetActiveInteraction();
            if (target == null)
            {
                GameObject[] locations = GameObject.FindGameObjectsWithTag(locationTag);
                foreach (var loc in locations)
                {
                    Button btn = loc.GetComponentInChildren<Button>(true);
                    if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
                    {
                        target = btn;
                        break;
                    }
                }
            }
            return target;
        }

        private void UpdatePointer(Button target)
        {
            ClearPointer();
            if (target == null || !target.gameObject.activeInHierarchy || !target.interactable) return;
            _currentTrackedButton = target;
            
            _activePointer = Instantiate(pointerPrefab, target.transform.position + pointerOffset, target.transform.rotation);
            target.onClick.AddListener(OnTargetClicked);
        }

        private void OnTargetClicked()
        {
            if (_currentTrackedButton != null)
                _currentTrackedButton.onClick.RemoveListener(OnTargetClicked);

            if (!_isUiTutorialFinished) _uiButtonIndex++;
            else _gameplayClickCount++;

            ClearPointer();

            // Замена DOVirtual.DelayedCall на Tween.Delay
            _delayTween.Stop(); // Останавливаем предыдущую задержку, если она была
            _delayTween = Tween.Delay(0.2f, RefreshTutorial);
        }

        private void FinishTutorialSession() => ClearPointer();

        private void ClearPointer()
        {
            if (_activePointer != null) Destroy(_activePointer);
            _currentTrackedButton = null;
        }

        private void OnDisable()
        {
            if (_questManager != null) _questManager.OnStateChanged -= RefreshTutorial;
            
            foreach (var proxy in buttonProxies)
            {
                if (proxy.sourceButton != null) proxy.sourceButton.onClick.RemoveAllListeners();
            }

            // Останавливаем все твины, связанные с этим объектом, при выключении
            _delayTween.Stop();
            Tween.StopAll(this); 
            
            ClearPointer();
        }
    }
}