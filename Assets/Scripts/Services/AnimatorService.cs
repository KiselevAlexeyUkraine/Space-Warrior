using UnityEngine;
using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using System;

namespace Servise
{
    public sealed class AnimatorService : MonoBehaviour
    {
        [Header("Movement Settings (Grid)")]
        [SerializeField] private float minX = -5.5f;
        [SerializeField] private float maxX = 5.5f;
        [SerializeField] private float stepY = -1.0f;
        [SerializeField] private float speed = 2f;

        [Header("Projectiles Management")]
        [SerializeField] private float bulletDestroyY = -40f;
        [Tooltip("Границы экрана для удаления ракет (опционально, если они улетают слишком далеко по X/Y).")]
        [SerializeField] private float screenBoundaryRadius = 100f;

        private class TrackedProjectile
        {
            public Transform transform;
            public float speed;
            public Vector3 direction; 
            public bool isHomingDirection; 
        }

        private Transform _formationRoot;
        private Vector3 _startPosition;
        private Coroutine _animationCoroutine;
        private bool _isMovingRight = false;
        private readonly List<TrackedProjectile> _activeProjectiles = new List<TrackedProjectile>();

        public void RegisterProjectile(GameObject projectileGo, float projectileSpeed)
        {
            if (projectileGo == null) return;

            _activeProjectiles.Add(new TrackedProjectile
            {
                transform = projectileGo.transform,
                speed = projectileSpeed,
                direction = Vector3.down,
                isHomingDirection = false
            });
        }

        public void RegisterDirectedProjectile(GameObject projectileGo, float projectileSpeed, Vector3 moveDirection)
        {
            if (projectileGo == null) return;

            _activeProjectiles.Add(new TrackedProjectile
            {
                transform = projectileGo.transform,
                speed = projectileSpeed,
                direction = moveDirection.normalized, 
                isHomingDirection = true
            });
        }

        private void Update()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                TrackedProjectile projectile = _activeProjectiles[i];

                if (projectile.transform == null || !projectile.transform.gameObject.activeInHierarchy)
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                projectile.transform.position += projectile.direction * projectile.speed * Time.deltaTime;

                bool shouldDestroy = false;

                if (projectile.isHomingDirection)
                {
                    if (projectile.transform.position.magnitude > screenBoundaryRadius || projectile.transform.position.y < bulletDestroyY)
                        shouldDestroy = true;
                }
                else
                {
                    if (projectile.transform.position.y <= bulletDestroyY)
                        shouldDestroy = true;
                }

                if (shouldDestroy)
                {
                    SafeDespawn(projectile.transform.gameObject);
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        private void SafeDespawn(GameObject go)
        {
            if (go == null) return;
            if (LeanPool.Links.ContainsKey(go)) LeanPool.Despawn(go);
            else Destroy(go);
        }

        public void StartAnimation(Transform root)
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            if (_formationRoot != null)
                Tween.StopAll(_formationRoot);

            _formationRoot = root;
            _startPosition = _formationRoot.position;
            _isMovingRight = false;
            _animationCoroutine = StartCoroutine(FormationMovementLoop());
        }

        private IEnumerator FormationMovementLoop()
        {
            if (_formationRoot == null) yield break;

            yield return new WaitForSeconds(0.2f);

            while (true)
            {
                if (_formationRoot == null) yield break;

                float targetX = _isMovingRight ? maxX : minX;
                Vector3 targetPositionX = new Vector3(targetX, _formationRoot.position.y, _formationRoot.position.z);
                float distanceX = Mathf.Abs(_formationRoot.position.x - targetX);
                float durationX = distanceX / speed;

                // ИСПРАВЛЕНО: Убран неверный параметр из метода Position
                Tween moveXTween = Tween.Position(_formationRoot, targetPositionX, durationX, Ease.Linear);
                yield return moveXTween.ToYieldInstruction();

                if (_formationRoot == null) yield break;

                Vector3 targetPositionY = _formationRoot.position + new Vector3(0, stepY, 0);
                float durationY = Mathf.Abs(stepY) / speed;

                // ИСПРАВЛЕНО: Убран неверный параметр из метода Position
                Tween moveYTween = Tween.Position(_formationRoot, targetPositionY, durationY, Ease.Linear);
                yield return moveYTween.ToYieldInstruction();

                _isMovingRight = !_isMovingRight;
            }
        }

        public void ResetFormationAndCall(Action onResetComplete)
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);

            if (_formationRoot == null)
            {
                onResetComplete?.Invoke();
                return;
            }

            Debug.Log("Сбрасываем сетку на стартовую позицию перед новой волной...");
            Tween.StopAll(_formationRoot);
            
            // ИСПРАВЛЕНО: Теперь параметр warnIfTargetDestroyed передается строго туда, где PrimeTween его ждет — в метод OnComplete!
            Tween.Position(_formationRoot, _startPosition, 1.2f, Ease.InOutQuad)
                .OnComplete(onResetComplete, warnIfTargetDestroyed: false);
        }

        private void OnDestroy()
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            if (_formationRoot != null)
                Tween.StopAll(_formationRoot);
            _activeProjectiles.Clear();
        }
    }
}