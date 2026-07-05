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
            Debug.LogError(request.error);
            yield break;
        }

        Parse(request.downloadHandler.text);

        IsLoaded = true;

        Debug.Log("Balance loaded.");
    }

    private void Parse(string csv)
    {
        string[] lines = csv.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cells = line.Split(',');

            if (cells.Length < 2)
                continue;

            string key = cells[0].Trim();

            if (!float.TryParse(cells[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float value))
                continue;

            switch (key)
            {
                case "PlayerHealth":
                    PlayerHealth = (int)value;
                    break;

                case "PlayerShield":
                    PlayerShield = (int)value;
                    break;

                case "PlayerFireRate":
                    PlayerFireRate = value;
                    break;

                case "PlayerBulletSpeed":
                    PlayerBulletSpeed = value;
                    break;

                case "PlayerMoveSpeed":
                    PlayerMoveSpeed = value;
                    break;

                case "EnemyHealth":
                    EnemyHealth = (int)value;
                    break;

                case "EnemyShield":
                    EnemyShield = (int)value;
                    break;

                case "EnemyCollisionDamage":
                    EnemyCollisionDamage = value;
                    break;

                case "EnemyBulletFireRate":
                    EnemyBulletFireRate = value;
                    break;

                case "EnemyBulletSpeed":
                    EnemyBulletSpeed = value;
                    break;

                case "EnemyBulletDamage":
                    EnemyBulletDamage = value;
                    break;

                case "EnemyRocketFireRate":
                    EnemyRocketFireRate = value;
                    break;

                case "EnemyRocketSpeed":
                    EnemyRocketSpeed = value;
                    break;

                case "EnemyRocketRadius":
                    EnemyRocketRadius = value;
                    break;

                case "EnemyRocketDamage":
                    EnemyRocketDamage = value;
                    break;

                case "EnemyLaserFireRate":
                    EnemyLaserFireRate = value;
                    break;

                case "EnemyLaserDelay":
                    EnemyLaserDelay = value;
                    break;

                case "EnemyLaserDuration":
                    EnemyLaserDuration = value;
                    break;

                case "EnemyLaserDamage":
                    EnemyLaserDamage = value;
                    break;
            }
        }
    }
}