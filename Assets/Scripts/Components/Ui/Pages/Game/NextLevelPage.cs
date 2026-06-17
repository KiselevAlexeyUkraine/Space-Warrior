using Services;
using Zenject;
using Components.Ui.Pages;

namespace Assets.Scripts.Components.Ui.Pages.Game
{
	public class NextLevelPage : BasePage
	{
		[Inject]
		private void Construct(SceneService sceneService)
		{
			Opened += () => { sceneService.LoadNextScene(); };
		}
	}
}