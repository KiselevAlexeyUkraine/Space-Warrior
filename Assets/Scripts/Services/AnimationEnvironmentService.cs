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
        [Tooltip("Интервал в секундах между обновлениями DynamicGI (перерасчет отражений и освещения). Чем больше, тем выше FPS.")]
        [SerializeField, Min(0.05f)] private float giUpdateInterval = 0.5f;

        private Tween _skyboxTween;
        private float _currentAngle;
        private float _nextGiUpdateTime;

        // Кэшируем ID свойств шейдера
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

            // 1. Каждый кадр плавно обновляем визуал скайбокса (GPU-friendly)
            ApplySkyboxVisuals(_currentAngle);

            // 2. Спектр освещения (GI) обновляем по таймеру, чтобы избежать лагов
            if (Time.time >= _nextGiUpdateTime)
            {
                DynamicGI.UpdateEnvironment();
                _nextGiUpdateTime = Time.time + giUpdateInterval;
            }
        }

        private void ApplySkyboxVisuals(float angle)
        {
            float t = angle / 360f;
            Color color = colorGradient.Evaluate(t);

            skyboxMaterial.SetFloat(RotationProperty, angle);
            skyboxMaterial.SetColor(TintProperty, color);
        }
    }
}