using Components.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Components.Player.Localization;

namespace Components.Ui.Pages.Menu
{
    public class LevelsPage : BasePage
    {
        [SerializeField] private Button _levelButtonPrefab;
        [SerializeField] private RectTransform _levelsContainer;
        [SerializeField] private Button _back;

        private SceneService _sceneService;
        private DiContainer _container;
        private LocalizationManager _localizationManager;
        private PlayerScore _playerScore;

        [Inject]
        private void Construct(SceneService sceneService, DiContainer container, LocalizationManager localizationManager, PlayerScore playerScore)
        {
            _sceneService = sceneService;
            _container = container;
            _localizationManager = localizationManager;
            _playerScore = playerScore;

            CreateLevelButtons();
        }

        private void Awake()
        {
            _back.onClick.AddListener(() => { PageSwitcher.Open(PageName.Menu).Forget(); });
        }

        private void OnEnable()
        {
            _back.GetComponentInChildren<TMP_Text>().text = _localizationManager.Back;
            UpdateLevelButtonsState();
        }

        private void OnDestroy()
        {
            _back.onClick.RemoveAllListeners();
        }

        private void CreateLevelButtons()
        {
            // Удаляем возможные дубликаты кнопок
            foreach (Transform child in _levelsContainer)
            {
                Destroy(child.gameObject);
            }

            for (var i = 2; i < _sceneService.GetScenesCount(); i++)
            {
                var levelIndex = i;
                var displayLevelNumber = i - 1; // Отображаемый порядковый номер уровня

                var newLevelGO = _container.InstantiatePrefab(_levelButtonPrefab, _levelsContainer);
                var newLevel = newLevelGO.GetComponent<Button>();
                
                newLevel.GetComponentInChildren<TMP_Text>().text = $"{displayLevelNumber}";
                
                // Делаем кнопку активной, если уровень открыт
                newLevel.interactable = displayLevelNumber <= _playerScore.UnlockedLevel;

                newLevel.onClick.AddListener(() =>
                {
                    _sceneService.SceneToLoad = levelIndex;
                    PageSwitcher.Open(PageName.Start).Forget();
                });
            }
        }

        private void UpdateLevelButtonsState()
        {
            int childCount = _levelsContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var button = _levelsContainer.GetChild(i).GetComponent<Button>();
                if (button != null)
                {
                    int displayLevelNumber = i + 1;
                    button.interactable = displayLevelNumber <= _playerScore.UnlockedLevel;
                }
            }
        }
    }
}