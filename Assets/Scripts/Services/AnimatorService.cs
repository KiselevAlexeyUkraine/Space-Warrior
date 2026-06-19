using UnityEngine;
using PrimeTween;
using System.Collections;

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
        private bool _spawningActive = true; // Флаг, указывающий, что идет старт уровня / спавн

        /// <summary>
        /// Метод инициализации анимации. Вызывается из Spawner-а через Zenject
        /// </summary>
        public void StartAnimation(Transform root)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            if (_formationRoot != null)
            {
                Tween.StopAll(_formationRoot);
            }

            _formationRoot = root;
            _startPosition = _formationRoot.position;
            _isMovingRight = false;
            _spawningActive = true; // В начале игры разрешаем движение без проверки количества детей

            _animationCoroutine = StartCoroutine(FormationMovementLoop());
        }

        private IEnumerator FormationMovementLoop()
        {
            if (_formationRoot == null) yield break;

            // Небольшая задержка перед стартом (по желанию), чтобы первый враг успел заспавниться
            yield return new WaitForSeconds(0.2f);

            while (true)
            {
                // ИСПРАВЛЕНО: Если спавн завершен и детей не осталось — только тогда сбрасываем сетку
                if (!_spawningActive && _formationRoot.childCount == 0)
                {
                    ResetFormation();
                    yield break;
                }

                // Переключаем флаг спавна, как только появился хотя бы один враг 
                // (или если спавнер заполнил сетку полностью)
                if (_spawningActive && _formationRoot.childCount > 0)
                {
                    _spawningActive = false;
                }

                // 1. Рассчитываем движение по оси X
                float targetX = _isMovingRight ? maxX : minX;
                Vector3 targetPositionX = new Vector3(targetX, _formationRoot.position.y, _formationRoot.position.z);
                
                float distanceX = Mathf.Abs(_formationRoot.position.x - targetX);
                float durationX = distanceX / speed;

                // Используем Linear, чтобы движение не замедлялось у краев экрана
                Tween moveXTween = Tween.Position(_formationRoot, targetPositionX, durationX, Ease.Linear);
                yield return moveXTween.ToYieldInstruction();

                if (!_spawningActive && _formationRoot.childCount == 0)
                {
                    ResetFormation();
                    yield break;
                }

                // 2. Шаг по оси Y вниз
                Vector3 targetPositionY = _formationRoot.position + new Vector3(0, stepY, 0);
                float durationY = Mathf.Abs(stepY) / speed;

                Tween moveYTween = Tween.Position(_formationRoot, targetPositionY, durationY, Ease.Linear);
                yield return moveYTween.ToYieldInstruction();

                // 3. Меняем направление
                _isMovingRight = !_isMovingRight;
            }
        }

        private void ResetFormation()
        {
            if (_formationRoot == null) return;

            Debug.Log("Все враги уничтожены! Возвращаем переданный трансформ в исходную позицию.");
            Tween.StopAll(_formationRoot);
            Tween.Position(_formationRoot, _startPosition, 1f, Ease.InOutQuad);
        }

        private void OnDestroy()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            if (_formationRoot != null)
            {
                Tween.StopAll(_formationRoot);
            }
        }
    }
}