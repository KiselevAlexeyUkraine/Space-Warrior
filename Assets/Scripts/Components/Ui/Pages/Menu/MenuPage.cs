using System;
using UnityEngine;
using UnityEngine.UI;
using Components.Player.Localization;
using Zenject;
using TMPro;

namespace Components.Ui.Pages.Menu
{
    public class MenuPage : BasePage
    {
        [SerializeField] private TMP_Text _gameTitle;
        [SerializeField] private TMP_Text _versionText;

        [SerializeField] private Button _start;
        [SerializeField] private Button _levels;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _authors;
        [SerializeField] private Button _diary;
        [SerializeField] private Button _exit;

        private LocalizationManager _localizationManager;

        [Inject]
        private void Construct(LocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        private void Awake()
        {
            _start.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Start).Forget();
            });

            _levels.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Levels).Forget();
            });

            _settings.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Settings).Forget();
            });

            _authors.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Authors).Forget();
            });

            _exit.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.Exit).Forget();
            });

            _diary.onClick.AddListener(() =>
            {
                PageSwitcher.Open(PageName.DiaryMainMenu).Forget();
            });
        }

        private void OnEnable()
        {
            ButtonTextsUpdate();
        }

        private void Start()
        {
            ButtonTextsUpdate();
        }

        private void OnDestroy()
        {
            _start.onClick.RemoveAllListeners();
            _levels.onClick.RemoveAllListeners();
            _settings.onClick.RemoveAllListeners();
            _authors.onClick.RemoveAllListeners();
            _exit.onClick.RemoveAllListeners();
            _diary.onClick.RemoveAllListeners();
        }

        private void ButtonTextsUpdate()
        {
            _gameTitle.text = _localizationManager.GameTitle;

            _start.GetComponentInChildren<TMP_Text>().text = _localizationManager.StartGame;
            _levels.GetComponentInChildren<TMP_Text>().text = _localizationManager.ChoseLevel;
            _settings.GetComponentInChildren<TMP_Text>().text = _localizationManager.Settings;
            _authors.GetComponentInChildren<TMP_Text>().text = _localizationManager.Authors;
            _exit.GetComponentInChildren<TMP_Text>().text = _localizationManager.ExitGame;

            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }
    }
}