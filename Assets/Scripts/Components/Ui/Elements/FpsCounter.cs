using Services;
using TMPro;
using UnityEngine;
using Zenject;

namespace Components.Ui.Elements
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _fps;

        private SettingsService _settings;
        private float _timer = 1f;
    
        [Inject]
        private void Construct(SettingsService settingsService)
        {
            _settings = settingsService;
        }

        private void Awake()
        {
            SetActive(_settings.SavedFpsCounter);
            _fps.text = string.Empty;
            _settings.OnChanged += SetActive;
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;

            if (_timer >= 1f)
            {
                _fps.text = $"{(int)(1f / Time.unscaledDeltaTime)}";
                _timer = 0f;
            }
        }

        private void OnDestroy()
        {
            _settings.OnChanged -= SetActive;
        }

        private void SetActive(bool value)
        {
            enabled = value;
            _fps.gameObject.SetActive(value);
        }
    }
}
