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

        [Inject]
        private void Construct(SceneService sceneService, DiContainer container, LocalizationManager localizationManager)
        {
            _sceneService = sceneService;
            _container = container;
            _localizationManager = localizationManager;

            CreateLevelButtons();
        }

        private void Awake()
        {
            _back.onClick.AddListener(() => { PageSwitcher.Open(PageName.Menu).Forget(); });
        }

        private void OnEnable()
        {
            _back.GetComponentInChildren<TMP_Text>().text = _localizationManager.Back;
        }

        private void OnDestroy()
        {
            _back.onClick.RemoveAllListeners();
        }

        private void CreateLevelButtons()
        {
            for (var i = 2; i < _sceneService.GetScenesCount(); i++)
            {
                var levelIndex = i;
                var newLevelGO = _container.InstantiatePrefab(_levelButtonPrefab, _levelsContainer);
                var newLevel = newLevelGO.GetComponent<Button>();
                newLevel.GetComponentInChildren<TMP_Text>().text = $"{i - 1}";
                newLevel.onClick.AddListener(() =>
                {
                    _sceneService.SceneToLoad = levelIndex;
                    PageSwitcher.Open(PageName.Start).Forget();
                });
            }
        }
    }
}
