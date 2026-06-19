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
        [Tooltip("Крайняя левая точка по X, до которой идет движение.")]
        [SerializeField] private float minX = -5.5f;
        [Tooltip("Крайняя правая точка по X, до которой идет движение.")]
        [SerializeField] private float maxX = 5.5f;
        [Tooltip("На сколько смещаться по Y при достижении края.")]
        [SerializeField] private float stepY = -1.0f;
        [Tooltip("Скорость движения всей сетки врагов.")]
        [SerializeField] private float speed = 2f;

        // ДОБАВЛЕНО: Настройки для движения "глупых" пуль
        [Header("Projectiles Management")]
        [Tooltip("Нижняя граница экрана по Y, при пересечении которой пуля возвращается в пул.")]
        [SerializeField] private float bulletDestroyY = -40f;

        // Структура для отслеживания пули и её индивидуальной скорости
        private class TrackedProjectile
        {
            public Transform transform;
            public float speed;
        }

        private Transform _formationRoot;
        private Vector3 _startPosition;
        private Coroutine _animationCoroutine;
        private bool _isMovingRight = false;

        // Список всех летящих снарядов на сцене
        private readonly List<TrackedProjectile> _activeProjectiles = new List<TrackedProjectile>();

        /// <summary>
        /// ДОБАВЛЕНО: Регистрирует префаб пули без скриптов в сервисе для его постоянного движения вниз.
        /// </summary>
        public void RegisterProjectile(GameObject projectileGo, float projectileSpeed)
        {
            if (projectileGo == null) return;

            _activeProjectiles.Add(new TrackedProjectile
            {
                transform = projectileGo.transform,
                speed = projectileSpeed
            });
        }

        private void Update()
        {
            // Каждое обновление кадра двигаем все зарегистрированные пули вниз
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                TrackedProjectile projectile = _activeProjectiles[i];

                // Если пуля была уничтожена где-то снаружи (например, при попадании)
                if (projectile.transform == null || !projectile.transform.gameObject.activeInHierarchy)
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }

                // Смещаем пулю строго вниз по Y
                Vector3 currentPos = projectile.transform.position;
                currentPos.y -= projectile.speed * Time.deltaTime;
                projectile.transform.position = currentPos;

                // Если пуля улетела ниже нижней границы экрана — деспавним её
                if (currentPos.y <= bulletDestroyY)
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

        // --- Существующая логика движения сетки врагов ---

        public void StartAnimation(Transform root)
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            if (_formationRoot != null) Tween.StopAll(_formationRoot);

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
                float targetX = _isMovingRight ? maxX : minX;
                Vector3 targetPositionX = new Vector3(targetX, _formationRoot.position.y, _formationRoot.position.z);
                
                float distanceX = Mathf.Abs(_formationRoot.position.x - targetX);
                float durationX = distanceX / speed;

                Tween moveXTween = Tween.Position(_formationRoot, targetPositionX, durationX, Ease.Linear);
                yield return moveXTween.ToYieldInstruction();

                Vector3 targetPositionY = _formationRoot.position + new Vector3(0, stepY, 0);
                float durationY = Mathf.Abs(stepY) / speed;

                Tween moveYTween = Tween.Position(_formationRoot, targetPositionY, durationY, Ease.Linear);
                yield return moveYTween.ToYieldInstruction();

                _isMovingRight = !_isMovingRight;
            }
        }

        public void ResetFormationAndCall(Action onResetComplete)
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);

            if (_formationRoot == null)
            {
                onResetComplete?.Invoke();
                return;
            }

            Debug.Log("Сбрасываем сетку на стартовую позицию перед новой волной...");
            Tween.StopAll(_formationRoot);
            
            Tween.Position(_formationRoot, _startPosition, 1.2f, Ease.InOutQuad)
                .OnComplete(() =>
                {
                    onResetComplete?.Invoke();
                });
        }

        private void OnDestroy()
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            if (_formationRoot != null) Tween.StopAll(_formationRoot);
            _activeProjectiles.Clear();
        }
    }
}