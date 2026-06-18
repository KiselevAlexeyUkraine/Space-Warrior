using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Components.Player.Localization;
using Zenject;

namespace Components.Ui.Pages.Menu
{
    public class ExitPage : BasePage
    {
        [SerializeField] private Button _accept;
        [SerializeField] private Button _cancel;
        
        [SerializeField] private TMP_Text correctText;
        [SerializeField] private TMP_Text yesText;
        [SerializeField] private TMP_Text noText;
        
        private LocalizationManager _localizationManager;
        
        [Inject]
        private void Construct(LocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        private void Awake()
        {
            _accept.onClick.AddListener(Application.Quit);
            _cancel.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Menu).Forget();
            });
        }

        private void OnEnable()
        {
            //_localizationManager.OnLanguageChanged += ButtonTextsUpdate;
            ButtonTextsUpdate();
        }

        private void OnDestroy()
        {
            _accept.onClick.RemoveAllListeners();
            _cancel.onClick.RemoveAllListeners();
            //_localizationManager.OnLanguageChanged -= ButtonTextsUpdate;
        }
        
        private void ButtonTextsUpdate()
        {
            correctText.text = _localizationManager.Correct;
            yesText.text = _localizationManager.Yes;
            noText.text = _localizationManager.No;
        }
    }
}