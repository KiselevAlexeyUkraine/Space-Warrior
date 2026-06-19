using UnityEngine;
using PrimeTween;

namespace Servise
{
    public sealed class AnimationEnvironmentService : MonoBehaviour
    {
        [Header("Skybox Settings")]
        [Tooltip("Материал скайбокса, который будет анимироваться (Rotation и Tint).")]
        [SerializeField] private Material skyboxMaterial;

        [Tooltip("Время полного оборота скайбокса (360 градусов) в секундах.")]
        [SerializeField, Min(0.1f)] private float rotationDuration = 120f;

        [Tooltip("Градиент изменения цвета скайбокса по времени (0–1 = 0–360 градусов).")]
        [SerializeField] private Gradient colorGradient;

        [Header("Optimization")]
        [Tooltip("Минимальный шаг изменения угла (в градусах), при котором обновится скайбокс и освещение. Чем больше число, тем выше FPS.")]
        [SerializeField, Range(0.1f, 5f)] private float angleUpdateStep = 1.0f;

        private Tween _skyboxTween;
        private float _currentAngle;
        private float _lastUpdatedAngle = -100f; // Заведомо пройденное значение для первого кадра

        // Кэшируем ID свойств шейдера для оптимизации
        private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");
        private static readonly int TintProperty = Shader.PropertyToID("_Tint");

        private void OnEnable()
        {
            if (skyboxMaterial != null)
                StartSkyboxAnimation();
        }

        private void OnDisable()
        {
            _skyboxTween.Stop();
        }

        private void StartSkyboxAnimation()
        {
            _skyboxTween.Stop();

            // Твин теперь просто меняет приватную переменную _currentAngle в фоне без коллбеков
            _skyboxTween = Tween.Custom(
                startValue: 0f, 
                endValue: 360f, 
                duration: rotationDuration, 
                onValueChange: angle => _currentAngle = angle,
                ease: Ease.Linear, 
                cycles: -1, 
                cycleMode: CycleMode.Restart
            );
        }

        private void Update()
        {
            if (skyboxMaterial == null) return;

            // Проверяем, изменился ли угол достаточно сильно с момента последнего обновления
            if (Mathf.Abs(_currentAngle - _lastUpdatedAngle) >= angleUpdateStep)
            {
                ApplyEnvironment(_currentAngle);
                _lastUpdatedAngle = _currentAngle;
            }
        }

        private void ApplyEnvironment(float angle)
        {
            float t = angle / 360f;
            Color color = colorGradient.Evaluate(t);

            // Обновляем параметры материала (теперь редко, а не каждый кадр!)
            skyboxMaterial.SetFloat(RotationProperty, angle);
            skyboxMaterial.SetColor(TintProperty, color);

            // Обновляем освещение сцены
            DynamicGI.UpdateEnvironment();
        }
    }
}