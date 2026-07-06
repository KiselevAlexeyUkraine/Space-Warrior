using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Components.Enemy;
using Zenject;

public class BalanceConfig : IInitializable
{
    private const int LevelsCount = 5;

    private readonly string googleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/1RBHH4UJFLj880j62VlfpAgyu8xRTAwuM/export?format=csv";

    private enum TableSection { None, Player, Enemy }

    #region Player
    public float PlayerMoveSpeed { get; private set; }
    public float PlayerFireRate { get; private set; }
    public float PlayerBulletSpeed { get; private set; }
    public int PlayerHealth { get; private set; }
    public int PlayerShield { get; private set; }
    #endregion

    #region Enemy Common (по уровням 1-5)
    private readonly float[] _enemyHealthByLevel = new float[LevelsCount];
    private readonly float[] _enemyShieldByLevel = new float[LevelsCount];               // строка 17 (столбцы C..G)
    private readonly float[] _enemyShieldRegenIntervalByLevel = new float[LevelsCount];  // строка 18 (столбцы C..G), "раз в N секунд"
    private readonly float[] _enemyCollisionDamageByLevel = new float[LevelsCount];
    private readonly float[] _enemySpeedByLevel = new float[LevelsCount];

    // Свойства-геттеры прежней формы (значения Уровня 1) — для обратной совместимости со старым кодом
    public int EnemyHealth => (int)GetEnemyHealth(EnemyLevel.Level_1);
    public int EnemyShield => (int)GetEnemyShield(EnemyLevel.Level_1);
    public float EnemyCollisionDamage => GetEnemyCollisionDamage(EnemyLevel.Level_1);

    public float GetEnemySpeed(EnemyLevel level) => GetValue(_enemySpeedByLevel, level, "Скорость движения врага");
    public float GetEnemyHealth(EnemyLevel level) => GetValue(_enemyHealthByLevel, level, "Здоровье врага");
    public float GetEnemyShield(EnemyLevel level) => GetValue(_enemyShieldByLevel, level, "Щит врага");
    public float GetEnemyShieldRegenInterval(EnemyLevel level) => GetValue(_enemyShieldRegenIntervalByLevel, level, "Восстановление щита (сек/ед)");
    public float GetEnemyCollisionDamage(EnemyLevel level) => GetValue(_enemyCollisionDamageByLevel, level, "Урон от столкновения");
    #endregion

    #region Enemy Reflector (по уровням 1-5)
    private readonly float[] _enemyReflectionCountByLevel = new float[LevelsCount];            // строка 19 (столбцы C..G)
    private readonly float[] _enemyReflectedProjectilesSpeedByLevel = new float[LevelsCount];  // строка 20 (столбцы C..G)

    public float GetEnemyReflectionCount(EnemyLevel level) => GetValue(_enemyReflectionCountByLevel, level, "Количество отражений щита");
    public float GetEnemyReflectedProjectileSpeed(EnemyLevel level) => GetValue(_enemyReflectedProjectilesSpeedByLevel, level, "Скорость отраженного снаряда");
    #endregion

    #region Enemy Shooter (по уровням 1-5)
    private readonly float[] _enemyBulletFireRateByLevel = new float[LevelsCount];
    private readonly float[] _enemyBulletSpeedByLevel = new float[LevelsCount];
    private readonly float[] _enemyBulletDamageByLevel = new float[LevelsCount];

    public float GetEnemyBulletFireRate(EnemyLevel level) => GetValue(_enemyBulletFireRateByLevel, level, "Темп стрельбы врага");
    public float GetEnemyBulletSpeed(EnemyLevel level) => GetValue(_enemyBulletSpeedByLevel, level, "Скорость выстрела врага");
    public float GetEnemyBulletDamage(EnemyLevel level) => GetValue(_enemyBulletDamageByLevel, level, "Урон от выстрелов врага");
    #endregion

    #region Enemy Rocket (по уровням 1-5)
    private readonly float[] _enemyRocketFireRateByLevel = new float[LevelsCount];
    private readonly float[] _enemyRocketSpeedByLevel = new float[LevelsCount];
    private readonly float[] _enemyRocketRadiusByLevel = new float[LevelsCount];
    private readonly float[] _enemyRocketDamageByLevel = new float[LevelsCount];

    public float GetEnemyRocketFireRate(EnemyLevel level) => GetValue(_enemyRocketFireRateByLevel, level, "Темп запуска ракеты");
    public float GetEnemyRocketSpeed(EnemyLevel level) => GetValue(_enemyRocketSpeedByLevel, level, "Скорость ракеты");
    public float GetEnemyRocketRadius(EnemyLevel level) => GetValue(_enemyRocketRadiusByLevel, level, "Радиус поражения ракеты");
    public float GetEnemyRocketDamage(EnemyLevel level) => GetValue(_enemyRocketDamageByLevel, level, "Урон от ракеты");
    #endregion

    #region Enemy Laser (по уровням 1-5)
    private readonly float[] _enemyLaserFireRateByLevel = new float[LevelsCount];
    private readonly float[] _enemyLaserDelayByLevel = new float[LevelsCount];
    private readonly float[] _enemyLaserDurationByLevel = new float[LevelsCount];
    private readonly float[] _enemyLaserDamageByLevel = new float[LevelsCount];

    public float GetEnemyLaserFireRate(EnemyLevel level) => GetValue(_enemyLaserFireRateByLevel, level, "Скорострельность лазера");
    public float GetEnemyLaserDelay(EnemyLevel level) => GetValue(_enemyLaserDelayByLevel, level, "Задержка перед уроном лазера");
    public float GetEnemyLaserDuration(EnemyLevel level) => GetValue(_enemyLaserDurationByLevel, level, "Длительность лазера");
    public float GetEnemyLaserDamage(EnemyLevel level) => GetValue(_enemyLaserDamageByLevel, level, "Урон лазера");
    #endregion

    public void Initialize()
    {
        LoadAsync().Forget();
    }

    public async UniTaskVoid LoadAsync()
    {
        string separator = googleSheetCsvUrl.Contains("?") ? "&" : "?";
        string noCacheUrl = $"{googleSheetCsvUrl}{separator}nocache={DateTime.UtcNow.Ticks}";

        using UnityWebRequest request = UnityWebRequest.Get(noCacheUrl);
        request.SetRequestHeader("Cache-Control", "no-cache, no-store");
        request.SetRequestHeader("Pragma", "no-cache");

        try
        {
            await request.SendWebRequest();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BalanceConfig] Ошибка скачивания таблицы: {e.Message}");
            return;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[BalanceConfig] Ошибка скачивания таблицы: {request.error}");
            return;
        }

        Parse(request.downloadHandler.text);

        Debug.Log($"⚙️ Balance loaded успешно!\n" +
                  $"🔹 Скорость движения игрока: {PlayerMoveSpeed}\n" +
                  $"🔹 Скорость снаряда игрока: {PlayerBulletSpeed}\n" +
                  $"🔹 Щит игрока: {PlayerShield}\n" +
                  $"🔹 Темп стрельбы игрока: {PlayerFireRate} сек.");

        LogEnemyBalanceByLevel();
    }

    private void Parse(string csv)
    {
        string[] lines = csv.Replace("\r", "").Split('\n');
        TableSection currentSection = TableSection.None;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            List<string> cells = SplitCsvLine(line);

            if (cells.Count < 2)
                continue;

            string key = cells[0].Trim().Trim('"').ToLower();

            if (key.Contains("параметры игрока"))
            {
                currentSection = TableSection.Player;
                continue;
            }

            if (key.Contains("параметры врагов"))
            {
                currentSection = TableSection.Enemy;
                continue;
            }

            if (currentSection == TableSection.Player)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                float? value = ParseFloat(cells[1]);
                if (value == null)
                    continue;

                if (key.Contains("здоров")) PlayerHealth = (int)value.Value;
                else if (key.Contains("щит")) PlayerShield = (int)value.Value;
                else if (key.Contains("скорость стрельбы")) PlayerFireRate = value.Value;
                else if (key.Contains("скорость полета снаряд")) PlayerBulletSpeed = value.Value;
                else if (key.Contains("скорость движения")) PlayerMoveSpeed = value.Value;
            }
            else if (currentSection == TableSection.Enemy)
            {
                if (cells.Count < 3)
                    continue;

                string paramKey = cells[1].Trim().Trim('"').ToLower();

                if (string.IsNullOrEmpty(paramKey))
                    continue;

                float[] levelValues = ParseLevelValues(cells, 2);

                if (levelValues == null)
                    continue;

                ApplyEnemyLevelValues(paramKey, levelValues);
            }
        }
    }

    private float? ParseFloat(string raw)
    {
        string cleaned = raw.Trim().Trim('"');

        if (string.IsNullOrEmpty(cleaned) || cleaned == "-")
            return null;

        cleaned = cleaned.Replace(',', '.');

        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            return value;
        }

        return null;
    }

    private float[] ParseLevelValues(List<string> cells, int startIndex)
    {
        var values = new float[LevelsCount];
        bool anyParsed = false;

        for (int level = 0; level < LevelsCount; level++)
        {
            int cellIndex = startIndex + level;

            if (cellIndex >= cells.Count)
            {
                values[level] = level > 0 ? values[level - 1] : 0f;
                continue;
            }

            float? parsed = ParseFloat(cells[cellIndex]);

            if (parsed.HasValue)
            {
                values[level] = parsed.Value;
                anyParsed = true;
            }
            else
            {
                values[level] = level > 0 ? values[level - 1] : 0f;
            }
        }

        return anyParsed ? values : null;
    }

    private void ApplyEnemyLevelValues(string key, float[] levelValues)
    {
        if (key.Contains("скорость движения")) CopyTo(_enemySpeedByLevel, levelValues);
        else if (key.Contains("здоров")) CopyTo(_enemyHealthByLevel, levelValues);
        else if (key.Contains("урон от столкновения")) CopyTo(_enemyCollisionDamageByLevel, levelValues);
        else if (key.Contains("восстановление щита")) CopyTo(_enemyShieldRegenIntervalByLevel, levelValues);
        else if (key.Contains("щит")) CopyTo(_enemyShieldByLevel, levelValues);
        
        // --- ПАРСИНГ ПАРАМЕТРОВ РЕФЛЕКТОРА ---
        else if (key.Contains("количество отражений")) CopyTo(_enemyReflectionCountByLevel, levelValues);
        else if (key.Contains("скорость отраженного снаряда")) CopyTo(_enemyReflectedProjectilesSpeedByLevel, levelValues);
        // -------------------------------------

        else if (key.Contains("скорость стрельбы")) CopyTo(_enemyBulletFireRateByLevel, levelValues);
        else if (key.Contains("скорость полета выстрел")) CopyTo(_enemyBulletSpeedByLevel, levelValues);
        else if (key.Contains("урон от выстрелов")) CopyTo(_enemyBulletDamageByLevel, levelValues);
        else if (key.Contains("скорость запуска ракеты")) CopyTo(_enemyRocketFireRateByLevel, levelValues);
        else if (key.Contains("скорость полета ракеты")) CopyTo(_enemyRocketSpeedByLevel, levelValues);
        else if (key.Contains("радиус поражения ракеты")) CopyTo(_enemyRocketRadiusByLevel, levelValues);
        else if (key.Contains("урон от ракеты")) CopyTo(_enemyRocketDamageByLevel, levelValues);
        else if (key.Contains("скорострельность лазера")) CopyTo(_enemyLaserFireRateByLevel, levelValues);
        else if (key.Contains("задержка перед нанесением урона")) CopyTo(_enemyLaserDelayByLevel, levelValues);
        else if (key.Contains("длительность лазера")) CopyTo(_enemyLaserDurationByLevel, levelValues);
    }

    private void CopyTo(float[] target, float[] source)
    {
        for (int i = 0; i < LevelsCount && i < target.Length && i < source.Length; i++)
        {
            target[i] = source[i];
        }
    }

    private float GetValue(float[] array, EnemyLevel level, string paramName)
    {
        int index = Mathf.Clamp((int)level, 0, LevelsCount - 1);
        float value = array[index];

        Debug.Log($"[BalanceConfig] Считано из таблицы: '{paramName}' для {level} = {value}");

        return value;
    }

    private void LogEnemyBalanceByLevel()
    {
        for (int i = 0; i < LevelsCount; i++)
        {
            var level = (EnemyLevel)i;
            Debug.Log($"👾 Enemy balance [{level}] -> " +
                      $"Speed: {_enemySpeedByLevel[i]}, " +
                      $"Health: {_enemyHealthByLevel[i]}, " +
                      $"Shield: {_enemyShieldByLevel[i]}, " +
                      $"ReflectCount: {_enemyReflectionCountByLevel[i]}, " +
                      $"ReflectProjSpeed: {_enemyReflectedProjectilesSpeedByLevel[i]}, " +
                      $"CollisionDamage: {_enemyCollisionDamageByLevel[i]}");
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

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }
}