using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Components.Enemy;
using Configs;

namespace Components.Player
{
    public sealed class PlayerScore
    {
        public const string TOTAL_SCORE_KEY = "Player_Total_Saved_Score";
        public const string PLAYER_UPGRADE_LEVEL_KEY = "Player_Permanent_Upgrade_Level";

        private int _currentScore;
        private int _totalSavedScore;
        private int _shipLevel;

        private readonly Dictionary<ResourceType, int> _levelResources = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, int> _totalResources = new Dictionary<ResourceType, int>();

        public event Action<int> OnScoreChanged;
        public event Action<int> OnTotalScoreChanged;
        public event Action<int> OnLevelChanged;
        public event Action OnResourcesChanged;

        public int CurrentScore => _currentScore;
        public int TotalSavedScore => _totalSavedScore;
        public int CurrentLevel => _shipLevel;

        private AudioService _audioService;

        public PlayerScore()
        {
            _totalSavedScore = PlayerPrefs.GetInt(TOTAL_SCORE_KEY, 0);
            _shipLevel = PlayerPrefs.GetInt(PLAYER_UPGRADE_LEVEL_KEY, 1);
            LoadResources();
        }

        [Inject]
        public void Construct(AudioService audioService)
        {
            _audioService = audioService;
        }

        private void LoadResources()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                int amount = PlayerPrefs.GetInt($"Resource_{type}", 0);
                _totalResources[type] = amount;
                _levelResources[type] = 0;
            }
        }

        private void SaveResources()
        {
            foreach (var kvp in _totalResources)
            {
                PlayerPrefs.SetInt($"Resource_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        public int GetLevelResourceAmount(ResourceType type)
        {
            return _levelResources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public int GetTotalResourceAmount(ResourceType type)
        {
            return _totalResources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;

            if (!_levelResources.ContainsKey(type)) _levelResources[type] = 0;

            _levelResources[type] += amount;
            OnResourcesChanged?.Invoke();
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            _currentScore += amount;
            OnScoreChanged?.Invoke(_currentScore);
        }

        public void SaveLevelScore()
        {
            _totalSavedScore += _currentScore;
            PlayerPrefs.SetInt(TOTAL_SCORE_KEY, _totalSavedScore);

            foreach (var kvp in _levelResources)
            {
                if (!_totalResources.ContainsKey(kvp.Key)) _totalResources[kvp.Key] = 0;
                _totalResources[kvp.Key] += kvp.Value;
            }

            SaveResources();
            _currentScore = 0;
            _levelResources.Clear();

            OnTotalScoreChanged?.Invoke(_totalSavedScore);
            OnResourcesChanged?.Invoke();
        }

        public bool CanAffordRequirement(LevelUpgradeRequirement req)
        {
            if (_totalSavedScore < req.scoreCost) return false;

            if (req.requiredResources != null)
            {
                foreach (var res in req.requiredResources)
                {
                    if (GetTotalResourceAmount(res.resourceType) < res.amount)
                        return false;
                }
            }
            return true;
        }

        public bool TrySpendRequirement(LevelUpgradeRequirement req)
        {
            if (!CanAffordRequirement(req)) return false;

            _totalSavedScore -= req.scoreCost;
            PlayerPrefs.SetInt(TOTAL_SCORE_KEY, _totalSavedScore);

            if (req.requiredResources != null)
            {
                foreach (var res in req.requiredResources)
                {
                    _totalResources[res.resourceType] -= res.amount;
                }
            }

            _shipLevel++;
            PlayerPrefs.SetInt(PLAYER_UPGRADE_LEVEL_KEY, _shipLevel);

            SaveResources();

            OnTotalScoreChanged?.Invoke(_totalSavedScore);
            OnLevelChanged?.Invoke(_shipLevel);
            OnResourcesChanged?.Invoke();

            if (_audioService != null)
            {
                _audioService.PlayLevelUp();
            }

            return true;
        }

        public void ResetScore()
        {
            _currentScore = 0;
            _levelResources.Clear();

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _levelResources[type] = 0;
            }

            OnScoreChanged?.Invoke(_currentScore);
            OnResourcesChanged?.Invoke();
        }

        public void ResetAllProgress()
        {
            _currentScore = 0;
            _totalSavedScore = 0;
            _shipLevel = 1;

            PlayerPrefs.DeleteKey(TOTAL_SCORE_KEY);
            PlayerPrefs.DeleteKey(PLAYER_UPGRADE_LEVEL_KEY);

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _totalResources[type] = 0;
                _levelResources[type] = 0;
                PlayerPrefs.DeleteKey($"Resource_{type}");
            }

            PlayerPrefs.Save();

            OnScoreChanged?.Invoke(_currentScore);
            OnLevelChanged?.Invoke(_shipLevel);
            OnTotalScoreChanged?.Invoke(_totalSavedScore);
            OnResourcesChanged?.Invoke();
        }
    }
}