using UnityEngine;
using UnityEngine.UI;
using Services;
using Zenject;
using TMPro;
using MechanicalWorld;
using System.Collections;
using System.Collections.Generic;

namespace Components.Ui.Pages.Game
{
    public class StatsPage : BasePage
    {
        [System.Serializable]
        public class QuestVisualSettings
        {
            public int questId;
            public GameObject prefab;
        }
        
        [Header("Status UI Elements")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button helpButton;

        [Header("Interaction System")]
        [SerializeField] private Button interactionButton;
        [SerializeField] private Image cooldownImage; 
        [SerializeField] private float cooldownDuration = 30f;

        [Header("Quest Visuals")]
        [SerializeField] private Transform spawnContainer; 
        [SerializeField] private List<QuestVisualSettings> questVisuals;

        private PauseService _pauseService;
        private AnimatorService _animatorService;
        private PageSwitcher _pageSwitcher;
        private QuestManager _questManager;

        private Dictionary<int, TextMeshProUGUI> _activeQuestTexts = new Dictionary<int, TextMeshProUGUI>();
        private Dictionary<int, GameObject> _spawnedInstances = new Dictionary<int, GameObject>();
        private bool _isCooldown = false;

        [Inject]
        private void Construct(PauseService pauseService, AnimatorService animatorService, PageSwitcher pageSwitcher, QuestManager questManager)
        {
            _pauseService = pauseService;
            _animatorService = animatorService;
            _pageSwitcher = pageSwitcher;
            _questManager = questManager;

            OnOpen += () => { _pauseService.Play(); RefreshInteractionState(); };
            OnClose += () => _pauseService.Pause();
        }

        private void OnEnable()
        {
            pauseButton?.onClick.AddListener(OnPauseClicked);
            helpButton?.onClick.AddListener(OnHelpClicked);
            interactionButton?.onClick.AddListener(OnInteractionClicked);

            if (_questManager != null)
            {
                _questManager.OnQuestProgressChanged += UpdateQuestUI;
                _questManager.OnItemCollected += HandleItemRotation;
                _questManager.OncomplitQuest += RemoveQuestVisual;
                _questManager.OnStateChanged += RefreshInteractionState;
            }
        }

        private void OnDisable()
        {
            pauseButton?.onClick.RemoveListener(OnPauseClicked);
            helpButton?.onClick.RemoveListener(OnHelpClicked);
            interactionButton?.onClick.RemoveListener(OnInteractionClicked);

            if (_questManager != null)
            {
                _questManager.OnQuestProgressChanged -= UpdateQuestUI;
                _questManager.OnItemCollected -= HandleItemRotation;
                _questManager.OncomplitQuest -= RemoveQuestVisual;
                _questManager.OnStateChanged -= RefreshInteractionState;
            }
        }

        private void HandleItemRotation(int questId, RotationAxis axis)
        {
            if (_spawnedInstances.TryGetValue(questId, out var instance))
            {
                bool isLastItem = _questManager.IsQuestProgressFull(questId);
                _animatorService?.PlayMeshRotation(instance, axis, () => {
                    if (isLastItem) RemoveMeshOnly(questId);
                });
            }
        }

        private void RefreshInteractionState()
        {
            if (interactionButton == null || _isCooldown) return;

            // Получаем цель, которая ЕЩЕ НЕ ВЫПОЛНЕНА (или ожидает финального клика)
            Button target = _questManager.GetActiveInteraction();
            
            interactionButton.interactable = target != null;
            if (cooldownImage != null) cooldownImage.fillAmount = (target != null && !_isCooldown) ? 1 : 0;
        }

        private void OnInteractionClicked()
        {
            if (_isCooldown) return;
            Button target = _questManager.GetActiveInteraction();
            if (target != null) 
            { 
                target.onClick.Invoke(); 
                StartCoroutine(CooldownRoutine()); 
            }
        }

        private IEnumerator CooldownRoutine()
        {
            _isCooldown = true;
            interactionButton.interactable = false;
            float timer = 0f;
            while (timer < cooldownDuration)
            {
                timer += Time.deltaTime;
                if (cooldownImage != null) cooldownImage.fillAmount = timer / cooldownDuration;
                yield return null;
            }
            _isCooldown = false;
            RefreshInteractionState();
        }

        private void UpdateQuestUI(int questId, int collected, int total)
        {
            if (!_activeQuestTexts.ContainsKey(questId)) SpawnQuestVisual(questId);
            int rem = Mathf.Max(0, total - collected);
            if (_activeQuestTexts.TryGetValue(questId, out var txt) && txt != null)
                txt.text = rem <= 0 ? " " : rem.ToString();
        }

        private void RemoveMeshOnly(int questId)
        {
            if (_spawnedInstances.TryGetValue(questId, out var instance))
            {
                Transform meshTransform = instance.transform.Find("Mesh");
                if (meshTransform != null) Destroy(meshTransform.gameObject);
            }
        }

        private void RemoveQuestVisual(int questId) 
        { 
            if (_activeQuestTexts.TryGetValue(questId, out var txt)) 
            { 
                if (txt != null) Destroy(txt.transform.parent.gameObject); 
                _activeQuestTexts.Remove(questId); 
            }
            _spawnedInstances.Remove(questId);
        }

        private void SpawnQuestVisual(int questId) {
            var s = questVisuals.Find(x => x.questId == questId);
            if (s != null && s.prefab != null && spawnContainer != null) {
                var v = Instantiate(s.prefab, spawnContainer); 
                v.name = $"{questId}(Clone)";
                _spawnedInstances[questId] = v;
                var t = v.GetComponentInChildren<TextMeshProUGUI>(); 
                if (t != null) _activeQuestTexts.Add(questId, t);
            }
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();

        private void OnHelpClicked()
        {
            // Подсвечиваем всё
            _animatorService?.PlayHintFade();
            
            // Если есть активная цель, помечаем её как просмотренную (логика перехода камеры)
            Button target = _questManager.GetActiveInteraction();
            if (target != null) 
            {
                _questManager.MarkAsViewed(target);
            }
        }
    }
}