using UnityEngine;
using UnityEngine.UI;
using Services;
using Zenject;

namespace Components.Ui.Pages.Game
{
    public sealed class StatsPage : BasePage
    {
        [Header("Status UI Elements")]
        [SerializeField] private Button pauseButton;

        private PauseService _pauseService;
        private PageSwitcher _pageSwitcher;

        [Inject]
        private void Construct(PauseService pauseService, PageSwitcher pageSwitcher)
        {
            _pauseService = pauseService;
            _pageSwitcher = pageSwitcher;

            OnOpen += () => { _pauseService.Play(); };
            OnClose += () => _pauseService.Pause();
        }

        private void Start()
        {
            pauseButton?.onClick.AddListener(OnPauseClicked);
        }

        private void OnDestroy()
        {
            pauseButton?.onClick.RemoveListener(OnPauseClicked);
        }

        private void OnPauseClicked() => _pageSwitcher.Open(PageName.Pause).Forget();
    }
}