using System;
using UnityEngine;

namespace Components.Player
{
    public class PauseService : MonoBehaviour
    {
        public event Action OnPlay; 
        public event Action OnPause; 
        public event Action OnLevelComplete; 
        
        public bool IsPaused { get; private set; }

        private void OnDestroy()
        {
            OnPlay = null;
            OnPause = null;
            OnLevelComplete = null;
            
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

        /// <summary>
        /// Вызывается при прохождении уровня. Освобождает курсор для UI, но НЕ останавливает время.
        /// </summary>
        public void LevelComplete()
        {
            OnLevelComplete?.Invoke();
            IsPaused = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f; // Время продолжает идти
        }
    }
}