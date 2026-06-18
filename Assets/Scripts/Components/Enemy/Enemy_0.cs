using UnityEngine;

namespace Components.Enemy
{
    // Обычный враг, летящий просто вниз
    public sealed class Enemy_0 : EnemyBase
    {
        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }
    }
}