using UnityEngine;

namespace Components.Enemy
{
    // Обычный враг, летящий просто вниз
    public sealed class Simple_Enemy_1 : EnemyBase
    {
        public override void Move()
        {
            Vector3 tempPos = pos;
            tempPos.y -= speed * Time.deltaTime;
            pos = tempPos;
        }
    }
}