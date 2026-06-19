using UnityEngine;
using PrimeTween;
using System.Collections;
using System;

namespace Servise
{
    public sealed class AnimatorService : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Крайняя левая точка по X, до которой идет движение.")]
        [SerializeField] private float minX = -5.5f;

        [Tooltip("Крайняя правая точка по X, до которой идет движение.")]
        [SerializeField] private float maxX = 5.5f;

        [Tooltip("На сколько смещаться по Y при достижении края.")]
        [SerializeField] private float stepY = -1.0f;

        [Tooltip("Скорость движения сетки.")]
        [SerializeField] private float speed = 2f;

        private Transform _formationRoot;
        private Vector3 _startPosition;
        private Coroutine _animationCoroutine;
        private bool _isMovingRight = false;

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
                // 1. Рассчитываем движение по оси X
                float targetX = _isMovingRight ? maxX : minX;
                Vector3 targetPositionX = new Vector3(targetX, _formationRoot.position.y, _formationRoot.position.z);
                
                float distanceX = Mathf.Abs(_formationRoot.position.x - targetX);
                float durationX = distanceX / speed;

                Tween moveXTween = Tween.Position(_formationRoot, targetPositionX, durationX, Ease.Linear);
                yield return moveXTween.ToYieldInstruction();

                // 2. Шаг по оси Y вниз
                Vector3 targetPositionY = _formationRoot.position + new Vector3(0, stepY, 0);
                float durationY = Mathf.Abs(stepY) / speed;

                Tween moveYTween = Tween.Position(_formationRoot, targetPositionY, durationY, Ease.Linear);
                yield return moveYTween.ToYieldInstruction();

                // 3. Меняем направление
                _isMovingRight = !_isMovingRight;
            }
        }

        /// <summary>
        /// Сбрасывает сетку на стартовую позицию и выполняет действие по завершению возврата.
        /// </summary>
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
            
            // Плавно возвращаем в начальную точку за 1.2 секунды
            Tween.Position(_formationRoot, _startPosition, 1.2f, Ease.InOutQuad)
                .OnComplete(() =>
                {
                    // Когда трансформ вернулся в центр — запускаем колбэк (спавн следующей волны)
                    onResetComplete?.Invoke();
                });
        }

        private void OnDestroy()
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            if (_formationRoot != null) Tween.StopAll(_formationRoot);
        }
    }
}