using System;
using UnityEngine;
using Zenject;

namespace Components.Player
{
    public sealed class PlayerScore
    {
        private int _currentScore;
        private int _currentLevel = 1;
        
        private readonly int[] _levelThresholds = { 1000, 4000, 8000, 12000, 20000, 28000, 40000 };
        
        public event Action<int> OnScoreChanged;
        public event Action<int> OnLevelChanged;
        public event Action<float> OnProgressChanged;

        public int CurrentScore => _currentScore;
        public int CurrentLevel => _currentLevel;

        private AudioService _audioService;

        // Внедряем аудио-сервис через Zenject
        [Inject]
        public void Construct(AudioService audioService)
        {
            _audioService = audioService;
        }

        /// <summary>
        /// Добавить очки за уничтожение врага.
        /// </summary>
        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            _currentScore += amount;
            OnScoreChanged?.Invoke(_currentScore);

            CheckLevelUp();
            UpdateProgress();
        }

        /// <summary>
        /// Сбросить очки и уровень (при рестарте уровня).
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            _currentLevel = 1;
            
            OnScoreChanged?.Invoke(_currentScore);
            OnLevelChanged?.Invoke(_currentLevel);
            UpdateProgress();
        }

        private void CheckLevelUp()
        {
            int maxConfiguredLevel = _levelThresholds.Length + 1;
            
            while (_currentLevel < maxConfiguredLevel)
            {
                int nextLevelTarget = _levelThresholds[_currentLevel - 1];
                
                if (_currentScore >= nextLevelTarget)
                {
                    _currentLevel++;
                    OnLevelChanged?.Invoke(_currentLevel);
                    
                    // НОВОЕ: Проигрываем звук повышения уровня при каждом апе!
                    if (_audioService != null)
                    {
                        _audioService.PlayLevelUp();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void UpdateProgress()
        {
            if (_currentLevel > _levelThresholds.Length)
            {
                OnProgressChanged?.Invoke(1f);
                return;
            }

            int currentLevelIndex = _currentLevel - 1;
            int targetForNextLevel = _levelThresholds[currentLevelIndex];
            int baseScoreForCurrentLevel = currentLevelIndex > 0 ? _levelThresholds[currentLevelIndex - 1] : 0;

            int pointsRequiredForThisLevel = targetForNextLevel - baseScoreForCurrentLevel;
            int pointsEarnedInThisLevel = _currentScore - baseScoreForCurrentLevel;

            float progressFactor = Mathf.Clamp01((float)pointsEarnedInThisLevel / pointsRequiredForThisLevel);
            
            OnProgressChanged?.Invoke(progressFactor);
        }
    }
}