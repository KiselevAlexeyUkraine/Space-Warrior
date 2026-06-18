using UnityEngine;

namespace Components.Enemy
{
    public sealed class Enemy_2 : EnemyBase
    {
        [Header("Set in Inspector: Enemy_2")]
        [SerializeField] private float sinEccentricity = 0.6f;
        [SerializeField] private float lifeTime = 10f;

        [Header("Set Dynamically: Enemy_2")]
        [SerializeField] private Vector3 p0;
        [SerializeField] private Vector3 p1;
        [SerializeField] private float birthTime;

        private void Start()
        {
            p0 = Vector3.zero;
            p0.x = -bndCheck.camWidth - bndCheck.radius;
            p0.y = Random.Range(-bndCheck.camHeight, bndCheck.camHeight);

            p1 = Vector3.zero;
            p1.x = bndCheck.camWidth + bndCheck.radius;
            p1.y = Random.Range(-bndCheck.camHeight, bndCheck.camHeight);

            if (Random.value > 0.5f)
            {
                p0.x *= -1;
                p1.x *= -1;
            }

            birthTime = Time.time;
        }

        public override void Move()
        {
            float u = (Time.time - birthTime) / lifeTime;

            if (u > 1)
            {
                Destroy(gameObject);
                return;
            }

            u = u + sinEccentricity * Mathf.Sin(u * Mathf.PI * 2);
            pos = (1 - u) * p0 + u * p1;
        }
    }
}