using Components.Ui.Pages;
using Components.Player;
using UnityEngine;
using Zenject;

public class PauseSwitcher : MonoBehaviour
{
    private DesktopInput _desktopInput;
    private PageSwitcher _pageSwitcher;
    private PauseService _pause;

    private float _escapeCooldown = 0.2f;
    private float _lastEscapeTime;
    private bool _isLevel = false;

    [Inject]
    private void Construct(DesktopInput desktopInput, PageSwitcher pageSwitcher, PauseService pauseService)
    {
        _desktopInput = desktopInput;
        _pageSwitcher = pageSwitcher;
        _pause = pauseService;
    }

    private void Update()
    {
        if (_isLevel)
            return;

        if (_desktopInput.Escape && Time.unscaledTime - _lastEscapeTime >= _escapeCooldown)
        {
            _lastEscapeTime = Time.unscaledTime;

            if (_pause.IsPaused)
            {
                _pageSwitcher.Open(PageName.Stats).Forget();
            }
            else
            {
                _pageSwitcher.Open(PageName.Pause).Forget();
            }
        }
    }
    
}
