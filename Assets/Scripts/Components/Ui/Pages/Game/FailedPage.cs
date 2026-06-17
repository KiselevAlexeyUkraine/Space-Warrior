using Services;
using UnityEngine;
using UnityEngine.UI;
using Services.Localization;
using Zenject;
using TMPro;

namespace Components.Ui.Pages.Game
{
    public class FailedPage : BasePage
    {
        [SerializeField] private Button _restart;
        [SerializeField] private Button _menu;

        [Header("UI Elements")]
        [SerializeField] private Image _background;           
        [SerializeField] private TextMeshProUGUI _failureText;
        [SerializeField] private Sprite[] _backgroundSprites; 
        private string[] _texts = new string[2]; 


        private PauseService _pauseService;
        private PauseSwitcher _pauseSwitcher;
        private LocalizationManager _localizationManager;


        [Inject]
        private void Construct(PauseService pauseService, PauseSwitcher pauseSwitcher, LocalizationManager localizationManager)
        {
            _pauseService = pauseService;
            _pauseSwitcher = pauseSwitcher;
            _localizationManager = localizationManager;

            OnOpen += () =>
            {
                _pauseSwitcher.enabled = false;
                _pauseService.Pause();
            };
            OnOpenWithParam += ShowFailureUI;
            
        }

        private void Awake()
        {
            _restart.onClick.AddListener(() => { PageSwitcher.Open(PageName.GameRestart).Forget(); });
            _menu.onClick.AddListener(() => { PageSwitcher.Open(PageName.GameExit).Forget(); });
        }

        private void OnDestroy()
        {
            _restart.onClick.RemoveAllListeners();
            _menu.onClick.RemoveAllListeners();
            OnOpenWithParam -= ShowFailureUI;
        }
        
        private void ShowFailureUI(int index)
        {
            TextsUpdate();
            if (_background != null && _backgroundSprites != null && index < _backgroundSprites.Length)
                _background.sprite = _backgroundSprites[index];
            if (_failureText != null && _texts != null && index < _texts.Length)
                _failureText.text = _texts[index];


            TriggerAlarm();
        }

        private void TriggerAlarm()
        {
            Debug.Log("Alarm Triggered!");
            // Тут можно добавить анимацию, звук или красный экран
        }
        private void TextsUpdate()
        {
            _restart.GetComponentInChildren<TMP_Text>().text = _localizationManager.Restart;
            _menu.GetComponentInChildren<TMP_Text>().text = _localizationManager.ToMainMenu;
            _texts[0] = _localizationManager.TextFaild;
        }
    }
}
