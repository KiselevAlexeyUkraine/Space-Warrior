using Services;
using Zenject;

namespace Components.Ui.Pages.Game
{
    public class GameRestart : BasePage
    {
        [Inject]
        private void Construct(SceneService sceneService)
        {
			Opened += () => { sceneService.Load(sceneService.GetCurrentScene()); };
		}
    }
}