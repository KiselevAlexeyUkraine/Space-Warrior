using System;

namespace Components.Player
{
    public sealed class PlayerScore
    {
        private int _currentScore;
        
        public event Action<int> OnScoreChanged;

        public int CurrentScore => _currentScore;

        /// <summary>
        /// Добавить очки за уничтожение врага.
        /// </summary>
        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            _currentScore += amount;
            
            // Вызываем событие, если на него кто-то подписан
            OnScoreChanged?.Invoke(_currentScore);
        }

        /// <summary>
        /// Сбросить очки (при рестарте уровня).
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            OnScoreChanged?.Invoke(_currentScore);
        }
    }
}