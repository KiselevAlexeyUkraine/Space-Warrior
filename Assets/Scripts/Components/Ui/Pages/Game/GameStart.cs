using Components.Player;
using Zenject;
using Components.Effects;
using Cysharp.Threading.Tasks;

namespace Components.Ui.Pages.Game
{
    public class GameStart : BasePage
    {
        [Inject]
        private void Construct(
            PauseService pauseService, 
            PauseSwitcher pauseSwitcher, 
            PostProcessingController postProcessingController,
            BalanceConfig balanceConfig)
        {
            OnOpen += () =>
            {
                pauseService.Pause();
            };

            Opened += () => 
            {
                WaitBalanceAndOpenStatsAsync(pauseSwitcher, balanceConfig).Forget();
            };
        }

        private async UniTaskVoid WaitBalanceAndOpenStatsAsync(
            PauseSwitcher pauseSwitcher, 
            BalanceConfig balanceConfig)
        {
            // Ждем, пока подтянется BalanceConfig
            if (balanceConfig != null)
            {
                while (!balanceConfig.IsLoaded)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    if (this == null || gameObject == null) return;
                }
            }
            
            pauseSwitcher.enabled = true;

            // Переходим дальше только после полной загрузки конфига
            PageSwitcher.Open(PageName.Stats).Forget(); 
        }
    }
}