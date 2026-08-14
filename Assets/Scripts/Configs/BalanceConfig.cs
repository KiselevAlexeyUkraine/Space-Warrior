using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Components.Enemy;
using Components.Player;
using Zenject;

public class BalanceConfig : IInitializable
{
    // Поддерживается 10 уровней для характеристик из таблицы
    private const int LevelsCount = 10;

    private readonly string googleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/1RBHH4UJFLj880j62VlfpAgyu8xRTAwuM/export?format=csv";

    private enum TableSection { None, Player, Enemy }

    private PlayerScore _playerScore;

    // Флаг готовности данных
    public bool IsLoaded { get; private set; }

    [Inject]
    public void Construct(PlayerScore playerScore)
    {
        _playerScore = playerScore;
    }

    private int CurrentPlayerLevelIndex => _playerScore != null ? Mathf.Clamp(_playerScore.CurrentLevel - 1, 0, LevelsCount - 1) : 0;

    #region Player
    private readonly float[] _playerMoveSpeedByLevel = new float[LevelsCount];
    private readonly float[] _playerFireRateByLevel = new float[LevelsCount];
    private readonly float[] _playerBulletSpeedByLevel = new float[LevelsCount];
    private readonly int[] _playerHealthByLevel = new int[LevelsCount];
    private readonly int[] _playerShieldByLevel = new int[LevelsCount];

    public float PlayerMoveSpeed => _playerMoveSpeedByLevel[CurrentPlayerLevelIndex];
    public float PlayerFireRate => _playerFireRateByLevel[CurrentPlayerLevelIndex];
    public float PlayerBulletSpeed => _playerBulletSpeedByLevel[CurrentPlayerLevelIndex];
    public int PlayerHealth => _playerHealthByLevel[CurrentPlayerLevelIndex];
    public int PlayerShield => _playerShieldByLevel[CurrentPlayerLevelIndex];

    /// <summary>
    /// Возвращает параметры игрока для конкретного уровня (от 1 до 10).
    /// Используется в CompletePage для отображения характеристик прокачки.
    /// </summary>
    public bool TryGetPlayerStatsForLevel(int level, out float hp, out float shield, out float speed, out float fireRate)
    {
        hp = 0;
        shield = 0;
        speed = 0;
        fireRate = 0;

        if (level < 1 || level > LevelsCount)
            return false;

        int index = level - 1;
        hp = _playerHealthByLevel[index];
        shield = _playerShieldByLevel[index];
        speed = _playerMoveSpeedByLevel[index];
        fireRate = _playerFireRateByLevel[index];

        return true;
    }

    /// <summary>
    /// Возвращает стоимость улучшения корабля до указанного целевого уровня.
    /// </summary>
    public bool TryGetUpgradeCost(int targetLevel, out int cost)
    {
        cost = 0;

        if (targetLevel <= 1 || targetLevel > LevelsCount)
        {
            return false; // Достигнут максимум или невалидный уровень
        }

        // Вы можете заменить эту формулу своей логикой/массивом цен
        cost = (targetLevel - 1) * 2000; 
        return true;
    }
    #endregion

    #region Enemy Common
    private readonly float[] _enemyHealthByLevel = new float[LevelsCount];
    private readonly float[] _enemyShieldByLevel = new float[LevelsCount];               
    private readonly float[] _enemyShieldRegenIntervalByLevel = new float[LevelsCount];  
    private readonly float[] _enemyCollisionDamageByLevel = new float[LevelsCount];
    private readonly float[] _enemySpeedByLevel = new float[LevelsCount];

    public int EnemyHealth => (int)GetEnemyHealth(EnemyLevel.Level_1);
    public int EnemyShield => (int)GetEnemyShield(EnemyLevel.Level_1);
    public float EnemyCollisionDamage => GetEnemyCollisionDamage(EnemyLevel.Level_1);

    public float GetEnemySpeed(EnemyLevel level) => GetValue(_enemySpeedByLevel, level, "Скорость движения врага");
    public float GetEnemyHealth(EnemyLevel level) => GetValue(_enemyHealthByLevel, level, "Здоровье врага");
    public float GetEnemyShield(EnemyLevel level) => GetValue(_enemyShieldByLevel, level, "Щит врага");
    public float GetEnemyShieldRegenInterval(EnemyLevel level) => GetValue(_enemyShieldRegenIntervalByLevel, level, "Восстановление щита (сек/ед)");
    public float GetEnemyCollisionDamage(EnemyLevel level) => GetValue(_enemyCollisionDamageByLevel, level, "Урон от столкновения");
    #endregion

    #region Enemy Reflector
    private readonly float[] _enemyReflectionCountByLevel = new float[LevelsCount];            
    private readonly float[] _enemyReflectedProjectilesSpeedByLevel = new float[LevelsCount];  

    public float GetEnemyReflectionCount(EnemyLevel level) => GetValue(_enemyReflectionCountByLevel, level, "Количество отражений щита");
    public float GetEnemyReflectedProjectileSpeed(EnemyLevel level) => GetValue(_enemyReflectedProjectilesSpeedByLevel, level, "Скорость отраженного снаряда");
    #endregion

    #region Enemy Shooter
    private readonly float[] _enemyBulletFireRateByLevel = new float[LevelsCount]; 
    private readonly float[] _enemyBulletSpeedByLevel = new float[LevelsCount];    

    public float GetEnemyBulletFireRate(EnemyLevel level) => GetValue(_enemyBulletFireRateByLevel, level, "Скорость стрельбы стрелка");
    public float GetEnemyBulletSpeed(EnemyLevel level) => GetValue(_enemyBulletSpeedByLevel, level, "Скорость полета выстрела стрелка");
    #endregion

    #region Enemy Rocket
    private readonly float[] _enemyRocketFireRateByLevel = new float[LevelsCount]; 
    private readonly float[] _enemyRocketSpeedByLevel = new float[LevelsCount];    
    #endregion

    #region Enemy Laser
    private readonly float[] _enemyLaserFireRateByLevel = new float[LevelsCount];
    #endregion

    public void Initialize()
    {
        IsLoaded = false;
        LoadAsync().Forget();
    }

    public async UniTaskVoid LoadAsync()
    {
        string separator = googleSheetCsvUrl.Contains("?") ? "&" : "?";
        string noCacheUrl = $"{googleSheetCsvUrl}{separator}nocache={DateTime.UtcNow.Ticks}";

        using UnityWebRequest request = UnityWebRequest.Get(noCacheUrl);
        request.SetRequestHeader("Cache-Control", "no-cache, no-store");
        request.SetRequestHeader("Pragma", "no-cache");

        try { await request.SendWebRequest(); }
        catch (Exception e) { Debug.LogError($"[BalanceConfig] Ошибка: {e.Message}"); return; }

        if (request.result != UnityWebRequest.Result.Success) return;

        Parse(request.downloadHandler.text);
        LogEnemyBalanceByLevel();
        
        IsLoaded = true;
    }

    private void Parse(string csv)
    {
        string[] lines = csv.Replace("\r", "").Split('\n');
        TableSection currentSection = TableSection.None;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            List<string> cells = SplitCsvLine(line);
            if (cells.Count < 2) continue;

            string key = cells[0].Trim().Trim('"').ToLower();

            if (key.Contains("параметры игрока")) { currentSection = TableSection.Player; continue; }
            if (key.Contains("параметры врагов")) { currentSection = TableSection.Enemy; continue; }

            if (currentSection == TableSection.Player)
            {
                float[] levelValues = ParseLevelValues(cells, 1);
                if (levelValues == null) continue;

                if (key.Contains("здоров")) { for(int i = 0; i < LevelsCount; i++) _playerHealthByLevel[i] = (int)levelValues[i]; }
                else if (key.Contains("щит")) { for(int i = 0; i < LevelsCount; i++) _playerShieldByLevel[i] = (int)levelValues[i]; }
                else if (key.Contains("скорость стрельбы")) CopyTo(_playerFireRateByLevel, levelValues);
                else if (key.Contains("скорость полета снаряд")) CopyTo(_playerBulletSpeedByLevel, levelValues);
                else if (key.Contains("скорость движения")) CopyTo(_playerMoveSpeedByLevel, levelValues);
            }
            else if (currentSection == TableSection.Enemy)
            {
                if (cells.Count < 3) continue;
                string paramKey = cells[1].Trim().Trim('"').ToLower();
                if (string.IsNullOrEmpty(paramKey)) continue;

                float[] levelValues = ParseLevelValues(cells, 2);
                if (levelValues == null) continue;

                ApplyEnemyLevelValues(paramKey, levelValues);
            }
        }
    }

    private float? ParseFloat(string raw)
    {
        string cleaned = raw.Trim().Trim('"');
        if (string.IsNullOrEmpty(cleaned) || cleaned == "-") return null;
        cleaned = cleaned.Replace(',', '.');
        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return value;
        return null;
    }

    private float[] ParseLevelValues(List<string> cells, int startIndex)
    {
        var values = new float[LevelsCount];
        bool anyParsed = false;
        for (int level = 0; level < LevelsCount; level++)
        {
            int cellIndex = startIndex + level;
            if (cellIndex >= cells.Count) { values[level] = level > 0 ? values[level - 1] : 0f; continue; }
            float? parsed = ParseFloat(cells[cellIndex]);
            if (parsed.HasValue) { values[level] = parsed.Value; anyParsed = true; }
            else { values[level] = level > 0 ? values[level - 1] : 0f; }
        }
        return anyParsed ? values : null;
    }

    private void ApplyEnemyLevelValues(string key, float[] levelValues)
    {
        if (key.Contains("скорость движения")) CopyTo(_enemySpeedByLevel, levelValues);
        else if (key.Contains("здоров")) CopyTo(_enemyHealthByLevel, levelValues);
        else if (key.Contains("урон от столкновения")) CopyTo(_enemyCollisionDamageByLevel, levelValues);
        else if (key.Contains("восстановление щита")) CopyTo(_enemyShieldRegenIntervalByLevel, levelValues);
        else if (key.Contains("щит (единицы)")) CopyTo(_enemyShieldByLevel, levelValues); 
        
        else if (key.Contains("количество отражений")) CopyTo(_enemyReflectionCountByLevel, levelValues);
        else if (key.Contains("скорость отраженного снаряда")) CopyTo(_enemyReflectedProjectilesSpeedByLevel, levelValues);
        
        else if (key.Contains("скорость стрельбы") && !key.Contains("раке") && !key.Contains("лазе")) CopyTo(_enemyBulletFireRateByLevel, levelValues);
        else if (key.Contains("скорость полета выстрела")) CopyTo(_enemyBulletSpeedByLevel, levelValues);

        else if (key.Contains("скорость запуска ракеты") || key.Contains("темп запуска ракеты")) CopyTo(_enemyRocketFireRateByLevel, levelValues);
        else if (key.Contains("скорость полета ракеты") || key.Contains("скорость ракеты")) CopyTo(_enemyRocketSpeedByLevel, levelValues);
        
        else if (key.Contains("скорострельность лазера")) CopyTo(_enemyLaserFireRateByLevel, levelValues);
    }

    private void CopyTo(float[] target, float[] source)
    {
        for (int i = 0; i < LevelsCount && i < target.Length && i < source.Length; i++) target[i] = source[i];
    }

    private float GetValue(float[] array, EnemyLevel level, string paramName)
    {
        int index = Mathf.Clamp((int)level, 0, LevelsCount - 1);
        return array[index];
    }

    private void LogEnemyBalanceByLevel()
    {
        for (int i = 0; i < LevelsCount; i++)
        {
            Debug.Log($"👾 [{i}] -> ReflectionCount: {_enemyReflectionCountByLevel[i]}, ReflectedSpeed: {_enemyReflectedProjectilesSpeedByLevel[i]}");
        }
    }

    private List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }
}