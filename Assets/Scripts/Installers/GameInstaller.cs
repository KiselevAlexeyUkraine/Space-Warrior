using Components.Ui.Pages;
using Components.Player;
using Services;
using UnityEngine;
using Zenject;
using MechanicalWorld;

namespace Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Scene Objects (Existing)")]
        [SerializeField] private PageSwitcher _gameCanvas;

        [Header("Prefabs")]
        [SerializeField] private PauseService _pauseService;
        [SerializeField] private PlayerComponent _player;
        [SerializeField] private PlayerSpawn _playerSpawn;
        [SerializeField] private PauseSwitcher _pauseSwitcher;
        [SerializeField] private AnimatorService _animatorService;
        [SerializeField] private QuestManager questManager;

        public override void InstallBindings()
        {
            // Чистый C# класс
            Container.Bind<DesktopInput>().FromNew().AsSingle().NonLazy();

            // ПРИВЯЗКА СУЩЕСТВУЮЩЕГО ОБЪЕКТА (Твой запрос)
            // Используем FromInstance, чтобы не создавать клон
            Container.Bind<PageSwitcher>().FromInstance(_gameCanvas).AsSingle().NonLazy();

            // Остальные сервисы (если это префабы, оставляем FromComponentInNewPrefab)
            Container.Bind<PauseService>().FromComponentInNewPrefab(_pauseService).AsSingle().NonLazy();
            Container.Bind<PauseSwitcher>().FromComponentInNewPrefab(_pauseSwitcher).AsSingle().NonLazy();
            Container.Bind<AnimatorService>().FromComponentInNewPrefab(_animatorService).AsSingle().NonLazy();
            Container.Bind<QuestManager>().FromComponentInNewPrefab(questManager).AsSingle().NonLazy();
            
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