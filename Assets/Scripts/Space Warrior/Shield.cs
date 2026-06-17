using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Set in Inspector")]
    public float rotationsPerSecond = 0.1f;

    [Header("Set dynamically")]
    public float levelShown = 0;

    // Скрытые переменные, не появляющиеся в инспкторе
    Material mat;


    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        // Прочитать текущую мощность защитного поля из объекта-одиночки Hero
        int currLevel = Mathf.FloorToInt(Hero.S.shieldLevel);
        // Если она отличается от levelShown...
        if (levelShown != currLevel)
        {
            levelShown = currLevel;
            // Скорректировать смещение в текстуре, чтобы отобразить поле с другой мощногстью
            mat.mainTextureOffset = new Vector2(0.2f * levelShown, 0);
        }
        // Поварачивать поле в каждом кадре с постоянной скоростью
        float rZ = -(rotationsPerSecond * Time.time * 360) % 360f;
        transform.rotation = Quaternion.Euler(0, 0, rZ);
    }
}
