using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Services;
using Zenject;

namespace Components.Effects
{
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume Reference")]
        [SerializeField] private Volume _volume;

        [Header("Directional Light (Sun)")]
        [SerializeField] private Light _directionalLight;

        private DepthOfField _depthOfField;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;

        private SceneService _sceneService;

        [Inject]
        private void Construct(SceneService sceneService)
        {
            _sceneService = sceneService;
        }

        private void Awake()
        {
            if (_volume == null)
            {
                Debug.LogError("Volume is not assigned to PostProcessingController");
                return;
            }

            _volume.profile.TryGet(out _depthOfField);
            _volume.profile.TryGet(out _vignette);
            _volume.profile.TryGet(out _colorAdjustments);


        }

        public void EnableEffects(bool enable)
        {
            if (_depthOfField != null)
                _depthOfField.active = enable;

            if (_vignette != null)
                _vignette.active = enable;

            if (_colorAdjustments != null)
                _colorAdjustments.active = enable;
        }

        public void DisableAllEffects()
        {
            if (_depthOfField != null) _depthOfField.active = false;
            if (_vignette != null) _vignette.active = false;
            if (_colorAdjustments != null) _colorAdjustments.active = false;
        }
        public void CheckDiraction()
        {
            if (_directionalLight == null)
            {
                Debug.LogWarning("Directional Light is not assigned. Brightness adjustment will affect only post-processing.");
            }

            int currentScene = _sceneService?.GetCurrentScene() ?? -1;
            Debug.Log($"Current Scene Index: {currentScene}");

            if (currentScene >= 13 && _directionalLight != null)
            {
                _directionalLight.enabled = false;
                Debug.Log("Directional Light disabled for Scene 13");
            }
            else
            {
                _directionalLight.enabled = true;
            }
        }

        private void SetDirectionalLightIntensity(float intensity)
        {
            if (_directionalLight != null)
            {
                _directionalLight.intensity = intensity;
            }
            else
            {
                Debug.LogWarning("Directional Light is not assigned.");
            }
        }

        private void SetPostExposure(float exposure)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.value = exposure;
            }
            else
            {
                Debug.LogWarning("ColorAdjustments is not assigned in Volume.");
            }
        }

        public void ApplyBrightness(float brightness)
        {
            SetPostExposure(brightness);
            SetDirectionalLightIntensity(brightness);
        }
    }
}
