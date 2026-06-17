using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using Services.Localization;


namespace Components.Ui.Pages.Menu
{
    public class AuthorsPage : BasePage
    {
        [SerializeField] private Button _back;
        [SerializeField] private TMP_Text unityDeveloperText;
        [SerializeField] private TMP_Text unityDeveloper1Text;
        [SerializeField] private TMP_Text soundDisText;
        [SerializeField] private TMP_Text artist3DTMPText;
        [SerializeField] private TMP_Text artist2DTMPText;
        [SerializeField] private TMP_Text animatorTMPText;
        [SerializeField] private TMP_Text gameDesingerrText;
        [SerializeField] private TMP_Text backText;
        
        private LocalizationManager _localizationManager;

        private void Awake()
        {
            _back.onClick.AddListener(() => { PageSwitcher.Open(PageName.Menu).Forget(); });
        }
        
        [Inject]
        private void Construct(LocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        private void OnEnable()
        {
            //_localizationManager.OnLanguageChanged += ButtonTextsUpdate;
            ButtonTextsUpdate();
        }

        private void OnDestroy()
        {
            _back.onClick.RemoveAllListeners();
            //_localizationManager.OnLanguageChanged -= ButtonTextsUpdate;
        }
        
        private void ButtonTextsUpdate()
        {
            unityDeveloperText.text = _localizationManager.UnityDeveloper;
            unityDeveloper1Text.text = _localizationManager.UnityDeveloper;
            soundDisText.text = _localizationManager.SoundDesigner;
            artist3DTMPText.text = "3D - " + _localizationManager.Artist;
            artist2DTMPText.text = "2D - " + _localizationManager.Artist;
            animatorTMPText.text = "3D - " + _localizationManager.Animator;
            gameDesingerrText.text = _localizationManager.GameDesigner;
            backText.text = _localizationManager.Back;
        }
    }
}