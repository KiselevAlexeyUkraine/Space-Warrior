using Services;
using UnityEngine;
using Zenject;

namespace Components.Ui.Pages.Game
{
    public class GameExit : BasePage
    {
        [Inject]
        private void Construct(SceneService sceneService, PauseService pauseService)
        {
            OnOpen += () =>
            {
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			};
			Opened += () => 
            { 
                sceneService.Load("Menu");
            };
		}
    }
}