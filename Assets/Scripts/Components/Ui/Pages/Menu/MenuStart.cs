using UnityEngine;
using Components.Effects;
using Zenject;
using Services;

namespace Components.Ui.Pages.Menu
{
    public class MenuStart : BasePage
    {
        private void Awake()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
			Time.timeScale = 1f;
        }
        
        [Inject]
        private void Construct(PostProcessingController postProcessingController, MusicService musicService)
        {

            Opened += () => 
            {
                musicService.enabled = true;
                postProcessingController.CheckDiraction();
                PageSwitcher.Open(PageName.Menu).Forget(); 
            };
        }
    }
}