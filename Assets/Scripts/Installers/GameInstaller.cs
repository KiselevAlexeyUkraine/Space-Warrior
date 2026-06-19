using Components.Ui.Pages;
using Components.Player;
using Components.Player.Helpers;
using Servise;
using UnityEngine;
using Zenject;
using Level;

namespace Installers
{
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Scene References")]
        [SerializeField] private Main _main;

        [Header("Prefabs")]
        [SerializeField] private PageSwitcher _gameCanvas;
        [SerializeField] private PauseService _pauseService;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerSpawn _playerSpawn;
        [SerializeField] private PauseSwitcher _pauseSwitcher;
        [SerializeField] private AnimatorService _animatorService;

        public override void InstallBindings()
        {

            Container.Bind<Main>().FromInstance(_main).AsSingle().NonLazy();

            Container.Bind<DesktopInput>().FromNew().AsSingle().NonLazy();
            Container.Bind<PlayerScore>().FromNew().AsSingle().NonLazy();
            
            Container.Bind<Utils>().FromNew().AsSingle().NonLazy();
            
            Container.Bind<PageSwitcher>().FromComponentInNewPrefab(_gameCanvas).AsSingle().NonLazy();
            Container.Bind<PauseService>().FromComponentInNewPrefab(_pauseService).AsSingle().NonLazy();
            Container.Bind<PauseSwitcher>().FromComponentInNewPrefab(_pauseSwitcher).AsSingle().NonLazy();
            Container.Bind<AnimatorService>().FromComponentInNewPrefab(_animatorService).AsSingle().NonLazy();
            
            BindPlayer();
        }

        private void BindPlayer()
        {
            var player = Container.InstantiatePrefab(
                _player, _playerSpawn.transform.position, _playerSpawn.transform.rotation, null
            );
          
            Container.Bind<PlayerComponent>().FromComponentOn(player).AsSingle().NonLazy();
        }
    }
}