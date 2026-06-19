using UnityEngine;
using Components.Weapon;
using Lean.Pool;

namespace Components.Enemy
{
    public sealed class Enemy_DontMove : EnemyBase
    {
        private Vector3 _targetGridPosition;
        private bool _hasTarget = false;
        private float _maxHealthCached;
        private bool _reachedTarget = false; 

        protected override void Awake()
        {
            base.Awake();
            _maxHealthCached = health;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _hasTarget = false;
            _reachedTarget = false;
            health = _maxHealthCached; 

            if (bndCheck != null)
            {
                bndCheck.enabled = false;
            }

            // На всякий случай принудительно ставим Z = 0 при выходе из пула
            Vector3 currentPos = pos;
            currentPos.z = 0f;
            pos = currentPos;
        }

        public override void Move()
        {
            if (!_hasTarget) return;

            if (Vector3.Distance(pos, _targetGridPosition) > 0.05f)
            {
                Vector3 nextPos = Vector3.MoveTowards(pos, _targetGridPosition, speed * Time.deltaTime);
                nextPos.z = 0f; // ЖЕСТКИЙ КОНТРОЛЬ: удерживаем врага на Z = 0 во время полета
                pos = nextPos;
            }
            else
            {
                pos = _targetGridPosition;
                _reachedTarget = true;
            }
        }

        protected override void Update()
        {
            Move();

            if (showingDamage && Time.time > damageDoneTime)
            {
                UnShowDamage();
            }
        }

        protected override void OnCollisionEnter(Collision collision)
        {
            GameObject otherGo = collision.gameObject;
            
            if (!otherGo.CompareTag("ProjectileHero")) return;

            Projectile p = otherGo.GetComponent<Projectile>();

            ProcessDamage(p.type);
            
            if (LeanPool.Links.ContainsKey(otherGo))
            {
                LeanPool.Despawn(otherGo);
            }
            else
            {
                Destroy(otherGo);
            }
        }

        public void SetTargetPosition(Vector3 targetPos)
        {
            // ИСПРАВЛЕНО: Игнорируем входящий Z и принудительно ставим его в 0
            _targetGridPosition = new Vector3(targetPos.x, targetPos.y, 0f);
            _hasTarget = true;
        }
    }
}