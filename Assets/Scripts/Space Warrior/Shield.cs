using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Set in Inspector")]
    [SerializeField]
    private float rotationsPerSecond = 0.1f;

    [Header("Runtime")]
    [SerializeField]
    private float levelShown;

    private Material mat;

    private void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        if (PlayerCollect.S == null)
            return;

        int currentLevel =
            Mathf.FloorToInt(PlayerCollect.S.ShieldLevel);

        if (levelShown != currentLevel)
        {
            levelShown = currentLevel;

            mat.mainTextureOffset =
                new Vector2(0.2f * levelShown, 0);
        }

        float rZ =
            -(rotationsPerSecond * Time.time * 360f) % 360f;

        transform.rotation =
            Quaternion.Euler(0, 0, rZ);
    }
}