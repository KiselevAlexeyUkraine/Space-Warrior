using Components.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using Components.Effects;
using Components.Player.Localization;


namespace Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private SettingsService _settingsService;
        [SerializeField] private MusicService _musicService;
        [SerializeField] private AudioService _audioService;
        [SerializeField] private PostProcessingController _postProcessingController;
        [SerializeField] private LocalizationManager _localizationManager;
        [SerializeField] private BalanceConfig balanceConfig;

        
        public override void InstallBindings()
        {
            Container.Bind<SceneService>().FromNew().AsSingle().NonLazy();
            Container.Bind<EventSystem>().FromComponentInNewPrefab(_eventSystem).AsSingle().NonLazy();
            Container.Bind<SettingsService>().FromComponentInNewPrefab(_settingsService).AsSingle().NonLazy();
            Container.Bind<MusicService>().FromComponentInNewPrefab(_musicService).AsSingle().NonLazy();
            Container.Bind<AudioService>().FromComponentInNewPrefab(_audioService).AsSingle().NonLazy();
            Container.Bind<PostProcessingController>().FromComponentInNewPrefab(_postProcessingController).AsSingle().NonLazy();
            Container.Bind<LocalizationManager>().FromComponentInNewPrefab(_localizationManager).AsSingle().NonLazy();
            Container.Bind<BalanceConfig>().FromComponentInNewPrefab(balanceConfig).AsSingle().NonLazy();
        }
    }
}