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
            public Vector3 direction; // Vector3.down для обычных пуль, или кастомный вектор для ракет
            public bool isHomingDirection; // Флаг, что летит по вектору, а не просто вниз
        }

        private Transform _formationRoot;
        private Vector3 _startPosition;
        private Coroutine _animationCoroutine;
        private bool _isMovingRight = false;
        private readonly List<TrackedProjectile> _activeProjectiles = new List<TrackedProjectile>();

        /// <summary>
        /// Существующий метод для обычных пуль (летящих строго вниз)
        /// </summary>
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

        /// <summary>
        /// ДОБАВЛЕНО: Регистрирует ракету, летящую по определенному вектору направления
        /// </summary>
        public void RegisterDirectedProjectile(GameObject projectileGo, float projectileSpeed, Vector3 moveDirection)
        {
            if (projectileGo == null) return;

            _activeProjectiles.Add(new TrackedProjectile
            {
                transform = projectileGo.transform,
                speed = projectileSpeed,
                direction = moveDirection.normalized, // Нормализуем, чтобы скорость была равномерной
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

                // Двигаем снаряд по его индивидуальному направлению
                projectile.transform.position += projectile.direction * projectile.speed * Time.deltaTime;

                // Условия деспавна
                bool shouldDestroy = false;

                if (projectile.isHomingDirection)
                {
                    // Для ракет: удаляем, если улетели слишком далеко от центра сцены
                    if (projectile.transform.position.magnitude > screenBoundaryRadius || projectile.transform.position.y < bulletDestroyY)
                        shouldDestroy = true;
                }
                else
                {
                    // Для обычных пуль: старая проверка по Y
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

        // --- Остальная часть класса AnimatorService остается без изменений ---
        public void StartAnimation(Transform root) { /* ... */ }
        private IEnumerator FormationMovementLoop() { yield break; }
        public void ResetFormationAndCall(Action onResetComplete) { }
    }
}