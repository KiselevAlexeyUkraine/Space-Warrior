using UnityEngine;
using PrimeTween;

namespace Core
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

        private Tween _skyboxTween;

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
            // В PrimeTween важно останавливать твины при выключении объекта
            _skyboxTween.Stop();
        }

        private void StartSkyboxAnimation()
        {
            // Останавливаем старый твин перед запуском нового
            _skyboxTween.Stop();

            _skyboxTween = Tween.Custom(
                startValue: 0f, 
                endValue: 360f, 
                duration: rotationDuration, 
                onValueChange: ApplyEnvironment,
                ease: Ease.Linear, // Передаем сглаживание как аргумент
                cycles: -1, 
                cycleMode: CycleMode.Restart
            );
        }

        private void ApplyEnvironment(float angle)
        {
            // Рассчитываем положение в градиенте (от 0 до 1)
            float t = angle / 360f;
            Color color = colorGradient.Evaluate(t);

            // Обновляем параметры материала
            skyboxMaterial.SetFloat(RotationProperty, angle);
            skyboxMaterial.SetColor(TintProperty, color);

            // Обновляем освещение сцены (чтобы скайбокс влиял на объекты)
            // Внимание: вызов DynamicGI.UpdateEnvironment() каждый кадр может быть тяжелым.
            // Если FPS падает, лучше вызывать его реже (например, через Coroutine или интервал).
            DynamicGI.UpdateEnvironment();
        }
    }
}