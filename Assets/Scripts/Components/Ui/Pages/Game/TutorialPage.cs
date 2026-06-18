using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using System.Collections.Generic;
using Components.Player;

namespace Components.Ui.Pages.Game
{
    public class TutorialPage : BasePage
    {
        [SerializeField] private Button _continue;
        [SerializeField] private TextMeshProUGUI _tutorialText;
        [SerializeField, TextArea] private List<string> _textContents;

        private PauseService _pauseService;
        private SceneService _sceneService;
        
        private int _currentSceneIndex = 2;

        [Inject]
        private void Construct(PauseService pauseService, SceneService sceneService, PauseSwitcher pauseSwitcher)
        {
            _pauseService = pauseService;
            _sceneService = sceneService;

            OnOpen += () =>
            {
                Debug.Log("TutorialPage opened.");
                pauseSwitcher.enabled = true;
                pauseService.Pause();
                ShowTutorialForCurrentScene();
            };

            if (_sceneService == null)
            {
                Debug.LogError("SceneService is not injected into TutorialPage.");
            }
        }

        private void Awake()
        {
            _continue.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Stats).Forget();
                _pauseService.Play();
            });
        }

        private void OnDestroy()
        {
            _continue.onClick.RemoveAllListeners();
        }

        private void ShowTutorialForCurrentScene()
        {
            int index = _sceneService.GetCurrentScene() - _currentSceneIndex; // формула так как у нас две сцены вначале есть

            if (index >= 0 && index < _textContents.Count)
            {
                _tutorialText.text = _textContents[index];
            }
            else
            {
                Debug.LogWarning($"No tutorial text found for scene index {index}");
            }
        }
    }
}
