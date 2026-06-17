namespace Configs.Localization
{
    public interface ILocalizationConfig
    {
        // Главное меню
        string GameTitle { get; }
        string ChoseLevel { get; }
        string StartGame { get; }
        string Authors { get; }
        string Settings { get; }
        string ExitGame { get; }

        // Меню настроек — категории
        string MasterVolume { get; }
        string SoundVolume { get; }
        string MusicVolume { get; }
        string MouseSensitivity { get; }
        string Brightness { get; }
        string FrameLimit { get; }
        string FpsCounter { get; }
        string VSync { get; }
        string Resolution { get; }
        string Languages { get; }

        // Меню настроек — кнопки и состояния
        string Back { get; }
        string ToggleOn { get; }
        string ToggleOff { get; }

        // Меню паузы
        string TitlePause { get; }
        string Resume { get; }
        string Restart { get; }
        string ToMainMenu { get; }

        // Подтверждения и диалоги
        string Correct { get; }
        string Yes { get; }
        string No { get; }
        
        // Авторы
        string UnityDeveloper { get; }
        string Artist { get; }
        string Animator { get; }
        string SoundDesigner { get; }
        string GameDesigner { get; }
        
        // Страница чекпоинта
        string DeathTitle1 { get; }
        string DeathTitle2 { get; }
        
        // HUD
        string Keys{ get;  }
        
        // UI для станицы завершения уровня поражение
        string TextFaild { get; }
        
        // Победа
        string TextWin { get; }
        string TextWinGame { get; }
        string NextLevel { get; }
        
    }
}