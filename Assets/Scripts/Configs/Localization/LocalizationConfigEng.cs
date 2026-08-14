using UnityEngine;
using SO;

namespace Configs.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfigEng", menuName = "Configs/Localization/English", order = 1)]
    public class LocalizationConfigEng : ScriptableConfig, ILocalizationConfig
    {
        [Header("Main Menu")]
        [Tooltip("Game title displayed in the main menu")]
        [SerializeField] private string gameTitle = "Echo Wings";

        [Tooltip("Button for level selection (if applicable)")]
        [SerializeField] private string choseLevel = "Level Select";

        [Tooltip("Button to start a new game")]
        [SerializeField] private string startGame = "Start Game";

        [Tooltip("Button for upgrades menu")]
        [SerializeField] private string upgrade = "Upgrades";

        [Tooltip("Button for upgrading action")]
        [SerializeField] private string upgradeAction = "Upgrade";

        [Tooltip("Button to open the credits screen")]
        [SerializeField] private string authors = "Credits";

        [Tooltip("Button to open the settings menu")]
        [SerializeField] private string settings = "Settings";

        [Tooltip("Button to exit the game")]
        [SerializeField] private string exitGame = "Exit Game";


        [Header("Settings Menu — Categories and Elements")]
        [SerializeField] private string masterVolume = "Master Volume";
        [SerializeField] private string soundVolume = "Sound Effects";
        [SerializeField] private string musicVolume = "Music Volume";
        [SerializeField] private string mouseSensitivity = "Mouse Sensitivity";
        [SerializeField] private string brightness = "Screen Brightness";
        [SerializeField] private string frameLimit = "Frame Limit";
        [SerializeField] private string fpsCounter = "FPS Counter";
        [SerializeField] private string vSync = "Vertical Sync";
        [SerializeField] private string resolution = "Resolution";
        [SerializeField] private string language = "Language";


        [Header("Settings Menu — Buttons and States")]
        [Tooltip("Button to go back from the settings menu")]
        [SerializeField] private string back = "Back";

        [Tooltip("Label for the toggle state 'On'")]
        [SerializeField] private string toggleOn = "On";

        [Tooltip("Label for the toggle state 'Off'")]
        [SerializeField] private string toggleOff = "Off";


        [Header("Pause Menu")]
        [Tooltip("Tutle text from pause menu")]
        [SerializeField] private string titlePause = "Pause";
        
        [Tooltip("Button to resume the game from pause menu")]
        [SerializeField] private string resume = "Resume";

        [Tooltip("Button to restart the game from pause menu")]
        [SerializeField] private string restart = "Restart";

        [Tooltip("Button to return to the main menu")]
        [SerializeField] private string toMainMenu = "Main Menu";


        [Header("Confirmations and Dialogs")]
        [SerializeField] private string correct = "Are you sure?";
        [SerializeField] private string yes = "Yes";
        [SerializeField] private string no = "No";


        [Header("Authors — Roles")]
        [Tooltip("Role: Unity Developer")]
        [SerializeField] private string unityDeveloper = "Unity Developer";

        [Tooltip("Role: Artist")]
        [SerializeField] private string artist = "Artist";

        [Tooltip("Role: Sound Designer")]
        [SerializeField] private string soundDesigner = "Sound Designer";

        [Tooltip("Role: Game Designer")]
        [SerializeField] private string gameDesigner = "Game Designer";
        
        [Tooltip("Role: Animator")]
        [SerializeField] private string animator = "Animator";
        
        [Header("Cause of Death (for the lesion page))")]
        [Tooltip("Reason 1: Damage from the enemy")]
        [SerializeField] private string deathByEnemy = "Killed by the enemy";

        [Tooltip("Reason 2: Damage from the enemy")] 
        [SerializeField] private string deathByObstacle = "Crashed into an obstacle";
        
        [Header("HUD")]
        [Tooltip("Reason 1: Damage from the enemy")]
        [SerializeField] private string keys = "Keys";
        
        [Header("Defeat")]
        [Tooltip("Losing the game")]
        [SerializeField] private string textFaild = "Your mission is failed";
        
        [Header("UI Victory")]
        [Tooltip("Victory level")]
        [SerializeField] private string textWinLevel = "Level completed!";
        
        [Tooltip("Victory game")]
        [SerializeField] private string textWinGame = "Well done, you completed the game!";
        
        [Tooltip("The 'Next Level' button in the victory menu")]
        [SerializeField] private string nextWinGame = "Next level";
        
        // Interface property implementations
        public string GameTitle => gameTitle;
        public string ChoseLevel => choseLevel;
        public string StartGame => startGame;
        public string Upgrade => upgrade;
        public string UpgradeAction => upgradeAction;
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

        public string DeathTitle1 => deathByEnemy;
        public string DeathTitle2 => deathByObstacle;
        
        public string Keys => keys;
        
        public string TextFaild => textFaild;
        
        public string TextWin => textWinLevel;
        public string TextWinGame => textWinGame;
        public string NextLevel => nextWinGame;
    }
}