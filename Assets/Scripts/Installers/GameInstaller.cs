using Components.Ui.Pages;
using Components.Player;
using Components.Player.Helpers;
using UnityEngine;
using Zenject;

namespace Installers
{
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Prefabs")]
        [SerializeField] private PageSwitcher _gameCanvas;
        [SerializeField] private PauseService _pauseService;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerSpawn _playerSpawn;
        [SerializeField] private PauseSwitcher _pauseSwitcher;

        public override void InstallBindings()
        {
            Container.Bind<DesktopInput>().FromNew().AsSingle().NonLazy();
            Container.Bind<PlayerScore>().FromNew().AsSingle().NonLazy();
            
            Container.Bind<Utils>().AsSingle().NonLazy();
            
            Container.Bind<PageSwitcher>().FromComponentInNewPrefab(_gameCanvas).AsSingle().NonLazy();
            Container.Bind<PauseService>().FromComponentInNewPrefab(_pauseService).AsSingle().NonLazy();
            Container.Bind<PauseSwitcher>().FromComponentInNewPrefab(_pauseSwitcher).AsSingle().NonLazy();
            
            BindPlayer();
        }

        private void BindPlayer()
        {
            // Создаем игрока в точке спавна
            var player = Container.InstantiatePrefab(
                _player, _playerSpawn.transform.position, _playerSpawn.transform.rotation, null
            );
          
            Container.Bind<PlayerComponent>().FromComponentOn(player).AsSingle().NonLazy();
        }
    }
}