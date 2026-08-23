using Components.Ui.Pages;
using Components.Player;
using Components.Player.Helpers;
using UnityEngine;
using Zenject;
using Level;

namespace Installers
{
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Scene References")]
        [SerializeField] private Main _main;
        [SerializeField] private GridFormationSpawner _spawner; // Ссылка на спавнер на сцене

        [Header("Prefabs")]
        [SerializeField] private PageSwitcher _gameCanvas;
        [SerializeField] private PauseService _pauseService;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerSpawn _playerSpawn;
        [SerializeField] private PauseSwitcher _pauseSwitcher;

        public override void InstallBindings()
        {
            Container.Bind<Main>().FromInstance(_main).AsSingle().NonLazy();
            Container.Bind<DesktopInput>().FromNew().AsSingle().NonLazy();
            Container.Bind<Utils>().FromNew().AsSingle().NonLazy();

            // Регистрируем Spawner в Zenject
            if (_spawner != null)
            {
                Container.Bind<GridFormationSpawner>().FromInstance(_spawner).AsSingle().NonLazy();
            }
            else
            {
                Container.Bind<GridFormationSpawner>().FromComponentInHierarchy().AsSingle().NonLazy();
            }

            Container.Bind<PageSwitcher>().FromComponentInNewPrefab(_gameCanvas).AsSingle().NonLazy();
            Container.Bind<PauseService>().FromComponentInNewPrefab(_pauseService).AsSingle().NonLazy();
            Container.Bind<PauseSwitcher>().FromComponentInNewPrefab(_pauseSwitcher).AsSingle().NonLazy();

            BindPlayer();
        }

        private void BindPlayer()
        {
            var player = Container.InstantiatePrefab(
                _player, _playerSpawn.transform.position, _playerSpawn.transform.rotation, null
            );

            Container.Bind<PlayerComponent>().FromComponentOn(player).AsSingle().NonLazy();

            var playerMove = player.GetComponentInChildren<PlayerMove>();
            if (playerMove != null)
            {
                Container.Bind<PlayerMove>().FromInstance(playerMove).AsSingle().NonLazy();
            }
            else
            {
                Debug.LogError("[GameInstaller] Компонент PlayerMove не найден в дочерних объектах префаба игрока!", this);
            }

            var playerCollect = player.GetComponentInChildren<PlayerCollect>();
            if (playerCollect != null)
            {
                Container.Bind<PlayerCollect>().FromInstance(playerCollect).AsSingle().NonLazy();
            }
            else
            {
                Debug.LogError("[GameInstaller] Компонент PlayerCollect не найден в дочерних объектах префаба игрока!", this);
            }
        }
    }
}