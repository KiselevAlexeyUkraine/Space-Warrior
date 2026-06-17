using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Services;
using Components.Ui.Pages;

namespace MechanicalWorld
{
    public enum QuestActionType { CameraTransition, OpenCompletePage }
    // Перечисление для выбора оси в инспекторе
    public enum RotationAxis { X, Y, Z }

    [Serializable]
    public class QuestCameraTransition
    {
        public int questIndex;
        public QuestActionType actionType;
        public int targetCameraIndex = -1;
        public RotationAxis rotationAxis = RotationAxis.Z; // Новое поле: ось вращения
    }

    public class QuestManager : MonoBehaviour
    {
        [Header("Tags")]
        [SerializeField] private string questTriggerTag = "Quest";
        [SerializeField] private string itemTag = "PickUp";

        [Header("Animation States")]
        [SerializeField] private string pickUpAnimName = "PickUpQuest";
        [SerializeField] private string completeAnimName = "CompliteQuest";

        private AnimatorService _animatorService;
        private AudioService _audioService;
        private PageSwitcher _pageSwitcher;

        [Header("Quest → Action Transitions")]
        [SerializeField] private QuestCameraTransition[] questCameraTransitions;

        public event Action<int, int, int> OnQuestProgressChanged;
        public event Action<int, RotationAxis> OnItemCollected; // Теперь передаем ось
        public event Action<int> OncomplitQuest;
        public event Action<int, int> OnQuestCameraTransition;
        public event Action OnStateChanged;

        private readonly HashSet<int> _viewedCompletedQuests = new();
        private readonly Dictionary<int, List<Button>> _questItems = new();
        private readonly Dictionary<int, HashSet<int>> _questProgress = new();
        private readonly Dictionary<int, Button> _questButtons = new();
        private readonly Dictionary<int, QuestCameraTransition> _questActionMap = new();
        private readonly Dictionary<int, QuestState> _questStates = new();

        [Inject]
        private void Construct(AnimatorService animatorService, AudioService audioService, PageSwitcher pageSwitcher)
        {
            _animatorService = animatorService;
            _audioService = audioService;
            _pageSwitcher = pageSwitcher;
        }

        private void Start()
        {
            InitializeActionMap();
            InitializeQuests();
        }

        private void InitializeActionMap()
        {
            _questActionMap.Clear();
            foreach (var t in questCameraTransitions)
                if (!_questActionMap.ContainsKey(t.questIndex)) _questActionMap.Add(t.questIndex, t);
        }

        private void InitializeQuests()
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (!obj.scene.IsValid()) continue;
                int qIdx = ExtractIndex(obj.name);
                if (qIdx < 0) continue;
                Button btn = obj.GetComponentInChildren<Button>(true);
                if (!btn) continue;

                if (obj.CompareTag(itemTag))
                {
                    if (!_questItems.ContainsKey(qIdx))
                    {
                        _questItems[qIdx] = new List<Button>();
                        _questProgress[qIdx] = new HashSet<int>();
                        _questStates[qIdx] = QuestState.Inactive;
                    }
                    _questItems[qIdx].Add(btn);
                    btn.interactable = false;
                    btn.onClick.AddListener(() => CollectItem(qIdx, btn));
                }
                else if (obj.CompareTag(questTriggerTag))
                {
                    _questButtons[qIdx] = btn;
                    btn.onClick.AddListener(() => ActivateQuest(qIdx));
                    if (!_questStates.ContainsKey(qIdx)) _questStates[qIdx] = QuestState.Inactive;
                }
            }
        }

        public void ActivateQuest(int questIndex)
        {
            if (!_questItems.ContainsKey(questIndex) || _questStates[questIndex] == QuestState.Completed) return;
            _questStates[questIndex] = QuestState.Active;
            
            if (_questButtons.TryGetValue(questIndex, out var qb))
            {
                var anim = qb.GetComponent<Animator>();
                if (anim != null) anim.Play(pickUpAnimName);
                qb.interactable = false;
            }

            _audioService?.PlayQuestActivate();
            foreach (var item in _questItems[questIndex])
            {
                if (!_questProgress[questIndex].Contains(item.gameObject.GetInstanceID()))
                {
                    item.gameObject.SetActive(true);
                    item.interactable = true;
                }
            }
            OnQuestProgressChanged?.Invoke(questIndex, _questProgress[questIndex].Count, _questItems[questIndex].Count);
            NotifyStateChanged();
        }
        
        public bool IsQuestProgressFull(int questId)
        {
            if (_questItems.ContainsKey(questId) && _questProgress.ContainsKey(questId))
            {
                return _questProgress[questId].Count >= _questItems[questId].Count;
            }
            return false;
        }

        private void CollectItem(int questIndex, Button item)
        {
            if (item == null || _animatorService == null || _questStates[questIndex] != QuestState.Active) return;

            int id = item.gameObject.GetInstanceID();
            if (_questProgress[questIndex].Contains(id)) return;

            // --- 1. СМЕНА СЛОЯ НА TopObject ---
            int topObjectLayer = LayerMask.NameToLayer("TopObject");
            if (topObjectLayer != -1) // Проверка, существует ли такой слой в настройках проекта
            {
                SetLayerRecursively(item.gameObject, topObjectLayer);
            }
            else
            {
                Debug.LogWarning("Слой 'TopObject' не найден в настройках Layers!");
            }

            // --- 2. ЛОГИКА ПАРТИКЛОВ (на любой глубине вложенности) ---
            var particles = item.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                ps.Play();
            }

            // --- 3. ОБРАБОТКА СОСТОЯНИЯ ---
            item.interactable = false;
            _questProgress[questIndex].Add(id);

            OnQuestProgressChanged?.Invoke(questIndex, _questProgress[questIndex].Count, _questItems[questIndex].Count);
            NotifyStateChanged();
            _audioService?.PlayItemPickup();

            // --- 4. АНИМАЦИЯ ---
            _animatorService.PlayPickUpAnimation(item.transform.childCount > 0 ? item.transform.GetChild(0) : item.transform, () => {
        
                RotationAxis axis = RotationAxis.Z; 
                if (_questActionMap.TryGetValue(questIndex, out var config))
                {
                    axis = config.rotationAxis;
                }

                OnItemCollected?.Invoke(questIndex, axis);
        
                if (IsQuestProgressFull(questIndex)) CompleteQuest(questIndex);
            });
        }

// Вспомогательный метод для смены слоя у всей иерархии
        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        private void CompleteQuest(int questIndex)
        {
            if (_questStates[questIndex] == QuestState.Completed) return;
            _questStates[questIndex] = QuestState.Completed;
            _audioService?.PlayQuestComplete();

            if (_questButtons.TryGetValue(questIndex, out var qb))
            {
                qb.gameObject.SetActive(true);
                qb.interactable = true;
                qb.onClick.RemoveAllListeners();
                qb.onClick.AddListener(() => StartCoroutine(HandleQuestCompletionRoutine(questIndex, qb)));
            }
            NotifyStateChanged();
        }

        private IEnumerator HandleQuestCompletionRoutine(int questIndex, Button qb)
        {
            qb.interactable = false;
            if (!_viewedCompletedQuests.Contains(questIndex))
            {
                var animator = qb.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(completeAnimName);
                    yield return null; 
                    yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
                }
                _viewedCompletedQuests.Add(questIndex);
            }
            ExecuteQuestCompletionAction(questIndex);
            OncomplitQuest?.Invoke(questIndex);
            qb.interactable = true;
            NotifyStateChanged();
        }

        private void ExecuteQuestCompletionAction(int questIndex)
        {
            if (_questActionMap.TryGetValue(questIndex, out var transition))
            {
                if (transition.actionType == QuestActionType.CameraTransition) OnQuestCameraTransition?.Invoke(questIndex, transition.targetCameraIndex);
                else if (transition.actionType == QuestActionType.OpenCompletePage) _pageSwitcher.Open(PageName.Complete, questIndex).Forget();
            }
        }

        public void NotifyStateChanged() => OnStateChanged?.Invoke();

        public Button GetActiveInteraction()
        {
            foreach (var pair in _questButtons)
            {
                int qIdx = pair.Key;
                Button btn = pair.Value;

                if (btn == null || !btn.gameObject.activeInHierarchy) continue;

                // ЛОГИКА: 
                // 1. Если квест не завершен — он активен для клика.
                // 2. Если квест завершен, но мы ЕЩЕ НЕ КЛИКНУЛИ на кнопку завершения (не просмотрели анимацию) — он активен.
                // 3. Если анимация завершения уже была — игнорируем навсегда.
                
                bool isCompleted = _questStates[qIdx] == QuestState.Completed;
                bool alreadyViewed = _viewedCompletedQuests.Contains(qIdx);

                if (!isCompleted || (isCompleted && !alreadyViewed))
                {
                    if (btn.interactable) return btn;
                }
            }

            // Предметы для сбора (только для активных квестов)
            foreach (var entry in _questItems)
            {
                if (_questStates[entry.Key] == QuestState.Active)
                {
                    var item = entry.Value.FirstOrDefault(b => b != null && b.gameObject.activeInHierarchy && b.interactable);
                    if (item != null) return item;
                }
            }
            return null;
        }

        // Этот метод вызывается при нажатии Help или при завершении анимации
        public void MarkAsViewed(Button btn)
        {
            foreach (var pair in _questButtons)
            {
                if (pair.Value == btn)
                {
                    // Если квест выполнен, помечаем его как "просмотренный окончательно"
                    if (_questStates[pair.Key] == QuestState.Completed)
                    {
                        _viewedCompletedQuests.Add(pair.Key);
                    }
                    NotifyStateChanged(); // Это обновит состояние interactionButton в StatsPage
                    break;
                }
            }
        }

        private int ExtractIndex(string name) { string match = Regex.Match(name, @"\d+").Value; return int.TryParse(match, out int idx) ? idx : -1; }
        private enum QuestState { Inactive, Active, Completed }
    }
}