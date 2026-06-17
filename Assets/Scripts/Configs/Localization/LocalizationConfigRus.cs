using UnityEngine;
using SO;
using UnityEngine.Serialization;

namespace Configs.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfigRus", menuName = "Configs/Localization/Russian", order = 0)]
    public class LocalizationConfigRus : ScriptableConfig, ILocalizationConfig
    {
        [Header("Главное меню")]
        [Tooltip("Название игры, отображаемое в главном меню")]
        [SerializeField] private string gameTitle = "Крылья Эхо";

        [Tooltip("Кнопка выбора уровня (если предусмотрена)")]
        [SerializeField] private string choseLevel = "Выбор уровня";

        [Tooltip("Кнопка начала новой игры")]
        [SerializeField] private string startGame = "Начать игру";

        [Tooltip("Кнопка для перехода к экрану авторов")]
        [SerializeField] private string authors = "Авторы";

        [Tooltip("Кнопка открытия настроек")]
        [SerializeField] private string settings = "Настройки";

        [Tooltip("Кнопка выхода из игры")]
        [SerializeField] private string exitGame = "Выход из игры";


        [Header("Меню настроек — категории и элементы")]
        [SerializeField] private string masterVolume = "Общий";
        [SerializeField] private string soundVolume = "Звуки";
        [SerializeField] private string musicVolume = "Музыка";
        [SerializeField] private string mouseSensitivity = "Чувствительность мыши";
        [SerializeField] private string brightness = "Яркость экрана";
        [SerializeField] private string frameLimit = "Ограничение кадров";
        [SerializeField] private string fpsCounter = "Счётчик кадров";
        [SerializeField] private string vSync = "Вертикальная синхронизация";
        [SerializeField] private string resolution = "Разрешение";
        [SerializeField] private string language = "Язык";


        [Header("Меню настроек — кнопки и состояния")]
        [Tooltip("Кнопка 'Назад' для возвращения из настроек")]
        [SerializeField] private string back = "Назад";

        [Tooltip("Надпись для состояния переключателя 'Включено'")]
        [SerializeField] private string toggleOn = "Вкл";

        [Tooltip("Надпись для состояния переключателя 'Выключено'")]
        [SerializeField] private string toggleOff = "Выкл";


        [Header("Меню паузы")]
        [Tooltip("Название меню паузы")]
        [SerializeField] private string titlePause = "Пауза";
        
        [Tooltip("Кнопка 'Продолжить игру' в меню паузы")]
        [SerializeField] private string resume = "Продолжить";

        [Tooltip("Кнопка 'Начать заново' в меню паузы")]
        [SerializeField] private string restart = "Начать заново";

        [Tooltip("Кнопка выхода в главное меню")]
        [SerializeField] private string toMainMenu = "В главное меню";


        [Header("Подтверждения и диалоги")]
        [SerializeField] private string correct = "Вы уверены?";
        [SerializeField] private string yes = "Да";
        [SerializeField] private string no = "Нет";


        [Header("Авторы — Роли")]
        [Tooltip("Роль: Unity-разработчик")]
        [SerializeField] private string unityDeveloper = "Unity-разработчик";

        [Tooltip("Роль: Художник")]
        [SerializeField] private string artist = "Художник";
        
        [Tooltip("Роль: Аниматор")]
        [SerializeField] private string animator = "Аниматор";

        [Tooltip("Роль: Саунд-дизайнер")]
        [SerializeField] private string soundDesigner = "Саунд-дизайнер";

        [Tooltip("Роль: Геймдизайнер")]
        [SerializeField] private string gameDesigner = "Геймдизайнер";
        
        [Header("Причина смерти (для страницы поражения)")]
        [Tooltip("Причина 2: Столкновение с препятствием")]
        [SerializeField] private string deathByObstacle = "Разбился о препятствие";
        
        [Tooltip("Причина 1: Урон от врага")] 
        [SerializeField] private string deathByEnemy = "Убит врагом";
        
        [Header("HUD")]
        [Tooltip("Отображение ключей в интерфейсе")]
        [SerializeField] private string keys = "Ключей";
        
        [Header("Поражение")]
        [Tooltip("Проигрыш игры")]
        [SerializeField] private string textFaild = "Твоя миссия провалена";
        
        [Header("UI Победы")]
        [Tooltip("Победа уровня")]
        [SerializeField] private string textWinLevel = "Уровень пройден!";
        
        [Tooltip("Победа игры")]
        [SerializeField] private string textWinGame = "Молодец, ты прошёл игру!";
        
        [Tooltip("Кнопка 'Следующий уровень' в меню победы")]
        [SerializeField] private string nextWinGame = "Следующий уровень";
        
        // Свойства для интерфейса
        public string GameTitle => gameTitle;
        public string ChoseLevel => choseLevel;
        public string StartGame => startGame;
        public string Authors => authors;
        public string Settings => settings;
        public string ExitGame => exitGame;

        public string MasterVolume => masterVolume;
        public string SoundVolume => soundVolume;
        public string MusicVolume => musicVolume;
        public string MouseSensitivity => mouseSensitivity;
        public string Brightness => brightness;
        public string FrameLimit => frameLimit;
        public string FpsCounter => fpsCounter;
        public string VSync => vSync;
        public string Resolution => resolution;
        public string Languages => language;

        public string Back => back;
        public string ToggleOn => toggleOn;
        public string ToggleOff => toggleOff;

        public string TitlePause => titlePause;
        public string Resume => resume;
        public string Restart => restart;
        public string ToMainMenu => toMainMenu;

        public string Correct => correct;
        public string Yes => yes;
        public string No => no;

        public string UnityDeveloper => unityDeveloper;
        public string Artist => artist;
        public string Animator => animator;
        public string SoundDesigner => soundDesigner;
        public string GameDesigner => gameDesigner;
        
        public  string DeathTitle1 => deathByEnemy;
        public  string DeathTitle2 => deathByObstacle;
        
        public string Keys => keys;
        
        public string TextFaild => textFaild;
        
        public string TextWin => textWinLevel;
        public string TextWinGame => textWinGame;
        public string NextLevel => nextWinGame;
    }
}
