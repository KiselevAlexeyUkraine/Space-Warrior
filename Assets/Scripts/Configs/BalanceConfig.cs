using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public class BalanceConfig : MonoBehaviour
{
    [Header("Google Sheets CSV URL")]
    [SerializeField] private string googleSheetCsvUrl;

    [Header("Load")]
    [SerializeField] private bool loadOnAwake = true;

    public bool IsLoaded { get; private set; }

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

    private void Awake()
    {
        if (loadOnAwake)
            StartCoroutine(Load());
    }

    public IEnumerator Load()
    {
        IsLoaded = false;

        using UnityWebRequest request = UnityWebRequest.Get(googleSheetCsvUrl);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[BalanceConfig] Ошибка скачивания таблицы: {request.error}");
            yield break;
        }

        Parse(request.downloadHandler.text);

        IsLoaded = true;

        // Выводим в консоль для проверки ячейку B9
        Debug.Log($"⚙️ Balance loaded успешно! PlayerMoveSpeed (ячейка B9): {PlayerMoveSpeed}");
    }

    private void Parse(string csv)
    {
        // Нормализуем переносы строк для корректного разделения
        string[] lines = csv.Replace("\r", "").Split('\n');
        TableSection currentSection = TableSection.None;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cells = line.Split(',');

            if (cells.Length < 1)
                continue;

            // Очищаем имя от пробелов и кавычек CSV
            string key = cells[0].Trim().Trim('"');

            // Переключаем секции при чтении заголовков таблицы
            if (key.Contains("Параметры игрока"))
            {
                currentSection = TableSection.Player;
                continue;
            }
            if (key.Contains("Параметры врагов"))
            {
                currentSection = TableSection.Enemy;
                continue;
            }

            // Если в строке нет значения во второй колонке, идти дальше нет смысла
            if (cells.Length < 2)
                continue;

            // Заменяем запятую на точку, чтобы InvariantCulture смог распарсить "30,0" как float 30.0
            string valueRaw = cells[1].Trim().Replace(',', '.').Trim('"');

            if (!float.TryParse(valueRaw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float value))
                continue;

            // Парсинг параметров на основе текущей секции таблицы
            if (currentSection == TableSection.Player)
            {
                switch (key)
                {
                    case "Здоровье (единицы)":
                        PlayerHealth = (int)value;
                        break;

                    case "Щит (единицы)":
                        PlayerShield = (int)value;
                        break;

                    case "Скорость стрельбы (секунды между выстрелами)":
                        PlayerFireRate = value;
                        break;

                    case "Скорость полета снаряда (юниты в секунду)":
                        PlayerBulletSpeed = value;
                        break;

                    case "Скорость движения корабля (юниты в секунду)":
                        PlayerMoveSpeed = value; // Твоя ячейка B9!
                        break;
                }
            }
            else if (currentSection == TableSection.Enemy)
            {
                switch (key)
                {
                    // Общие параметры врагов
                    case "Скорость движения (юниты в секунды)":
                        // Если у тебя будет переменная для базовой скорости врага, запиши сюда
                        break;

                    case "Здоровье (единицы)":
                        EnemyHealth = (int)value;
                        break;

                    case "Урон от столкновения (единицы)":
                        EnemyCollisionDamage = value;
                        break;

                    case "Щит (единицы)":
                        EnemyShield = (int)value;
                        break;

                    // Обычные стрелки
                    case "Скорость стрельбы (секунды между выстрелами)":
                        EnemyBulletFireRate = value;
                        break;

                    case "Скорость полета выстрела (юниты в секунду)":
                        EnemyBulletSpeed = value;
                        break;

                    case "Урон от выстрелов (единицы)":
                        EnemyBulletDamage = value;
                        break;

                    // Ракетницы / Лазеры (добавь по аналогии, если точные строки появятся ниже в таблице)
                    case "Скорость запуска ракеты (секунды между запуском)":
                        EnemyRocketFireRate = value;
                        break;

                    case "Скорость полета ракеты (юниты в секунду)":
                        EnemyRocketSpeed = value;
                        break;
                }
            }
        }
    }
}