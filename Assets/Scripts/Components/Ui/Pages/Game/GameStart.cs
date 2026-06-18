using Components.Player;
using Zenject;
using Components.Effects;

namespace Components.Ui.Pages.Game
{
    public class GameStart : BasePage
    {
        [Inject]
        private void Construct(PauseService pauseService, PauseSwitcher pauseSwitcher, PostProcessingController postProcessingController)
        {
            OnOpen += () =>
            {
                pauseService.Pause();
            };
            Opened += () => 
            {
                postProcessingController.CheckDiraction();
                PageSwitcher.Open(PageName.Stats).Forget(); 

                // На всякий случай гарантируем, что PauseSwitcher включен после открытия стартовой страницы
                pauseSwitcher.enabled = true;
            };
        }
    }
}