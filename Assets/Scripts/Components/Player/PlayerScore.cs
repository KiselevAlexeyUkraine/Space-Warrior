using System;
using UnityEngine;
using Zenject;

namespace Components.Player
{
    public sealed class PlayerScore
    {
        public const string TOTAL_SCORE_KEY = "Player_Total_Saved_Score";
        public const string PLAYER_UPGRADE_LEVEL_KEY = "Player_Permanent_Upgrade_Level";

        private int _currentScore;     // Счет текущего уровня (накапливается за заезд)
        private int _totalSavedScore; // Общий сохраненный счет (валюта)
        private int _shipLevel;       // Уровень прокачки корабля

        public event Action<int> OnScoreChanged;
        public event Action<int> OnTotalScoreChanged;
        public event Action<int> OnLevelChanged;

        public int CurrentScore => _currentScore;
        public int TotalSavedScore => _totalSavedScore;
        public int CurrentLevel => _shipLevel;

        private AudioService _audioService;

        public PlayerScore()
        {
            _totalSavedScore = PlayerPrefs.GetInt(TOTAL_SCORE_KEY, 0);
            _shipLevel = PlayerPrefs.GetInt(PLAYER_UPGRADE_LEVEL_KEY, 1);
        }

        [Inject]
        public void Construct(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            _currentScore += amount;
            OnScoreChanged?.Invoke(_currentScore);
        }

        public void SaveLevelScore()
        {
            if (_currentScore <= 0) return;

            _totalSavedScore += _currentScore;
            PlayerPrefs.SetInt(TOTAL_SCORE_KEY, _totalSavedScore);
            PlayerPrefs.Save();

            OnTotalScoreChanged?.Invoke(_totalSavedScore);
            Debug.Log($"<color=yellow>💾 [PlayerScore] Очки за уровень ({_currentScore}) сохранены! Общий счет: {_totalSavedScore}</color>");
        }

        /// <summary>
        /// Покупка улучшения: списывает валюту, повышает уровень и сохраняет состояние.
        /// </summary>
        public bool TrySpendTotalScore(int cost)
        {
            if (cost <= 0) return true;
            if (_totalSavedScore < cost) return false;

            _totalSavedScore -= cost;
            _shipLevel++;

            PlayerPrefs.SetInt(TOTAL_SCORE_KEY, _totalSavedScore);
            PlayerPrefs.SetInt(PLAYER_UPGRADE_LEVEL_KEY, _shipLevel);
            PlayerPrefs.Save();

            OnTotalScoreChanged?.Invoke(_totalSavedScore);
            OnLevelChanged?.Invoke(_shipLevel);

            if (_audioService != null)
            {
                _audioService.PlayLevelUp();
            }

            Debug.Log($"<color=green>🎉 [PlayerScore] Корабль успешно улучшен до уровня {_shipLevel}!</color>");
            return true;
        }

        /// <summary>
        /// Сбросить текущие очки за заезд и гарантировать актуальность уровня.
        /// </summary>
        public void ResetScore()
        {
            _currentScore = 0;
            _shipLevel = PlayerPrefs.GetInt(PLAYER_UPGRADE_LEVEL_KEY, 1);

            OnScoreChanged?.Invoke(_currentScore);
            OnTotalScoreChanged?.Invoke(_totalSavedScore);
        }

        /// <summary>
        /// Полный сброс всех данных игрока.
        /// </summary>
        public void ResetAllProgress()
        {
            _currentScore = 0;
            _totalSavedScore = 0;
            _shipLevel = 1;

            PlayerPrefs.DeleteKey(TOTAL_SCORE_KEY);
            PlayerPrefs.DeleteKey(PLAYER_UPGRADE_LEVEL_KEY);
            PlayerPrefs.Save();

            OnScoreChanged?.Invoke(_currentScore);
            OnLevelChanged?.Invoke(_shipLevel);
            OnTotalScoreChanged?.Invoke(_totalSavedScore);

            Debug.Log("<color=red>🗑️ [PlayerScore] Весь прогресс и улучшения сброшены!</color>");
        }
    }
}