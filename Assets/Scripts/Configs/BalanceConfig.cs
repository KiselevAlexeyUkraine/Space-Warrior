using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class BalanceConfig : IInitializable
{
    private readonly string googleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/1RBHH4UJFLj880j62VlfpAgyu8xRTAwuM/export?format=csv";
    

    // Переменная для отслеживания текущего блока в цикле парсинга
    private enum TableSection { None, Player, Enemy }

    #region Player
    public float PlayerMoveSpeed { get; private set; }
    public float PlayerFireRate { get; private set; }
    public float PlayerBulletSpeed { get; private set; }
    public int PlayerHealth { get; private set; }
    public int PlayerShield { get; private set; }
    #endregion

    #region Enemy Common
    public int EnemyHealth { get; private set; }
    public int EnemyShield { get; private set; }
    public float EnemyCollisionDamage { get; private set; }
    #endregion

    #region Enemy Shooter
    public float EnemyBulletFireRate { get; private set; }
    public float EnemyBulletSpeed { get; private set; }
    public float EnemyBulletDamage { get; private set; }
    #endregion

    #region Enemy Rocket
    public float EnemyRocketFireRate { get; private set; }
    public float EnemyRocketSpeed { get; private set; }
    public float EnemyRocketRadius { get; private set; }
    public float EnemyRocketDamage { get; private set; }
    #endregion

    #region Enemy Laser
    public float EnemyLaserFireRate { get; private set; }
    public float EnemyLaserDelay { get; private set; }
    public float EnemyLaserDuration { get; private set; }
    public float EnemyLaserDamage { get; private set; }
    #endregion

    // Zenject автоматически вызывает Initialize() при старте контейнера,
    // если класс забиндлен через BindInterfacesAndSelfTo
    public void Initialize()
    {
        LoadAsync().Forget();
    }

    /// <summary>
    /// Публичный метод для ручной перезагрузки баланса из таблицы (например, по кнопке в дебаг-меню).
    /// </summary>
    public async UniTaskVoid LoadAsync()
    {

        // Cache-buster: без этого Google CDN может годами отдавать закэшированную
        // версию CSV, и изменения в таблице не будут доходить до игры
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
    }

    private void Parse(string csv)
    {
        string[] lines = csv.Replace("\r", "").Split('\n');
        TableSection currentSection = TableSection.None;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Разбираем CSV по столбцам с учётом кавычек,
            // чтобы значения вида "3,5" не резались на две колонки
            List<string> cells = SplitCsvLine(line);

            if (cells.Count < 2)
                continue;

            string key = cells[0].Trim().Trim('"').ToLower();
            string valueRaw = cells[1].Trim().Trim('"');

            // Если первая колонка пустая — пропускаем
            if (string.IsNullOrEmpty(key))
                continue;

            // Определяем текущий блок таблицы
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

            // Запятая теперь всегда десятичный разделитель
            // (разделение по колонкам уже корректно обработано в SplitCsvLine)
            valueRaw = valueRaw.Replace(',', '.');

            if (!float.TryParse(
                    valueRaw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float value))
            {
                continue;
            }

            if (currentSection == TableSection.Player)
            {
                if (key.Contains("здоров"))
                {
                    PlayerHealth = (int)value;
                }
                else if (key.Contains("щит"))
                {
                    PlayerShield = (int)value;
                }
                else if (key.Contains("скорость стрельбы"))
                {
                    PlayerFireRate = value;
                }
                else if (key.Contains("скорость полета снаряд"))
                {
                    PlayerBulletSpeed = value;
                }
                else if (key.Contains("скорость движения"))
                {
                    PlayerMoveSpeed = value;
                }
            }
            else if (currentSection == TableSection.Enemy)
            {
                if (key.Contains("здоров"))
                {
                    EnemyHealth = (int)value;
                }
                else if (key.Contains("урон от столкновения"))
                {
                    EnemyCollisionDamage = value;
                }
                else if (key.Contains("щит"))
                {
                    EnemyShield = (int)value;
                }
                else if (key.Contains("скорость стрельбы"))
                {
                    EnemyBulletFireRate = value;
                }
                else if (key.Contains("скорость полета выстрел"))
                {
                    EnemyBulletSpeed = value;
                }
                else if (key.Contains("урон от выстрелов"))
                {
                    EnemyBulletDamage = value;
                }
                else if (key.Contains("скорость запуска ракеты"))
                {
                    EnemyRocketFireRate = value;
                }
                else if (key.Contains("скорость полета ракеты"))
                {
                    EnemyRocketSpeed = value;
                }
            }
        }
    }

    // Простой CSV-парсер строки, корректно обрабатывающий поля в кавычках,
    // в том числе с запятыми внутри (например "3,5" — дробное число из Google Sheets)
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // Экранированная кавычка внутри поля в кавычках: ""
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