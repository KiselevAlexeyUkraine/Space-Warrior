using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Services.Localization;

namespace Components.Ui.Pages.Game
{
    public class CompletePage : BasePage
    {
        [SerializeField] private TMP_Text _tile;
        [SerializeField] private Button _nextLevel;
        [SerializeField] private Button _restart;
        [SerializeField] private Button _menu;
        
        private SceneService _sceneService;
        private LocalizationManager _localizationManager;

        [Inject]
        private void Construct(PauseService pauseService, PauseSwitcher pauseSwitcher, SceneService sceneService, LocalizationManager localizationManager)
        {
            _sceneService = sceneService;
            _localizationManager = localizationManager;
            
            OnOpen += () =>
            {
                pauseSwitcher.enabled = false;
                pauseService.Pause();
                TextsUpdate();
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
