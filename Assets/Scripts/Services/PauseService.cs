using System;
using UnityEngine;

namespace Services
{
    public class PauseService : MonoBehaviour
    {
        public event Action OnPlay; 
        public event Action OnPause; 
        
        public bool IsPaused { get; private set; }

        private void OnDestroy()
        {
            OnPlay = null;
            OnPause = null;
            
            IsPaused = false;
            Time.timeScale = 1f;
        }

        public void Toggle()
        {
            if (IsPaused)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }
        
        public void Play()
        {
            OnPlay?.Invoke();
            IsPaused = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = 1f;
        }

        public void Pause()
        {
            OnPause?.Invoke();
            IsPaused = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
        }
    }
}
