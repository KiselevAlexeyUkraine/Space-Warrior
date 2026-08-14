using UnityEngine;
using Zenject;
using Configs;
using Cysharp.Threading.Tasks;

namespace Components.Player
{
    /// <summary>
    /// Главный компонент игрока (корабля), который связывает компоненты движения, 
    /// сбора предметов/щитов и оружия, а также применяет характеристики из BalanceConfig при старте/прокачке.
    /// </summary>
    public sealed class PlayerComponent : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Header("===== Subcomponents =====")]
        [SerializeField] private PlayerMove _playerMove;
        [SerializeField] private PlayerCollect _playerCollect;

        private BalanceConfig _balanceConfig;
        private PlayerScore _playerScore;

        public Camera Camera => _camera;
        public PlayerMove PlayerMove => _playerMove;
        public PlayerCollect PlayerCollect => _playerCollect;

        [Inject]
        private void Construct(BalanceConfig balanceConfig, PlayerScore playerScore)
        {
            _balanceConfig = balanceConfig;
            _playerScore = playerScore;
        }

        private void Awake()
        {
            if (_playerMove == null) _playerMove = GetComponent<PlayerMove>();
            if (_playerCollect == null) _playerCollect = GetComponent<PlayerCollect>();
        }

        private void OnEnable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnLevelChanged += OnPlayerLevelChanged;
            }
        }

        private void Start()
        {
            // Ожидаем загрузку BalanceConfig и затем применяем актуальные параметры
            InitializeStatsAsync().Forget();
        }

        private void OnDisable()
        {
            if (_playerScore != null)
            {
                _playerScore.OnLevelChanged -= OnPlayerLevelChanged;
            }
        }

        /// <summary>
        /// Ждет завершения загрузки таблицы баланса, после чего инициализирует характеристики корабля.
        /// </summary>
        private async UniTaskVoid InitializeStatsAsync()
        {
            if (_balanceConfig == null) return;

            // Ждем, пока конфиг полностью загрузится из сети/локально
            while (!_balanceConfig.IsLoaded)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (this == null || gameObject == null) return;
            }

            ApplyStatsFromBalance();
        }

        private void OnPlayerLevelChanged(int newLevel)
        {
            ApplyStatsFromBalance();
        }

        /// <summary>
        /// Применяет актуальные параметры из BalanceConfig ко всем подсистемам игрока.
        /// </summary>
        public void ApplyStatsFromBalance()
        {
            if (_balanceConfig == null || !_balanceConfig.IsLoaded) return;

            // 1. Обновляем уровень щита в PlayerCollect
            if (_playerCollect != null)
            {
                _playerCollect.ShieldLevel = _balanceConfig.PlayerShield;
            }

            Debug.Log($"<color=cyan>🚀 [PlayerComponent] Характеристики корабля обновлены для Уровня {_playerScore?.CurrentLevel}. " +
                      $"HP/Щит: {_balanceConfig.PlayerShield}, Скорость: {_balanceConfig.PlayerMoveSpeed}, Темп стрельбы: {_balanceConfig.PlayerFireRate}</color>");
        }
    }
}