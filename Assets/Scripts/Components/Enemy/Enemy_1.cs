using UnityEngine;

namespace Components.Enemy
{
    public sealed class Enemy_1 : EnemyBase
    {
        [Header("Set in Inspector: Enemy_1")]
        [SerializeField] private float waveFrequency = 2f;
        [SerializeField] private float waveWidth = 4f;
        [SerializeField] private float waveRotY = 45f;

        private float x0; 
        private float birthTime;

        private void Start()
        {
            x0 = pos.x;
            birthTime = Time.time;
        }

        public override void Move()
        {
            Vector3 tempPos = pos;
            float age = Time.time - birthTime;
            float theta = Mathf.PI * 2 * age / waveFrequency;
            float sin = Mathf.Sin(theta);
            
            // Движение по оси X (синусоида)
            tempPos.x = x0 + waveWidth * sin;
            
            // Движение по оси Y (базовое линейное вниз)
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;

            // Поворот по оси Y
            Vector3 rot = new Vector3(0, sin * waveRotY, 0);
            transform.rotation = Quaternion.Euler(rot);
        }
    }
}