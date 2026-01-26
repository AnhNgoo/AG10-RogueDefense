/// <summary>
/// Định nghĩa tập trung các Event Names trong game
/// Sử dụng const string để tránh typo và dễ bảo trì
/// </summary>
public static class GameEvents
{
    // ===== AUDIO EVENTS =====
    public const string AUDIO_VOLUME_CHANGED = "Audio_VolumeChanged";
    public const string AUDIO_PLAY_SFX = "Audio_PlaySFX";
    public const string AUDIO_PLAY_MUSIC = "Audio_PlayMusic";
    public const string AUDIO_STOP_MUSIC = "Audio_StopMusic";

    // ===== UI EVENTS =====
    public const string UI_MENU_OPENED = "UI_MenuOpened";
    public const string UI_MENU_CLOSED = "UI_MenuClosed";
    public const string UI_PAUSE_GAME = "UI_PauseGame";
    public const string UI_RESUME_GAME = "UI_ResumeGame";

    // ===== SCENE EVENTS =====
    public const string SCENE_LOAD_STARTED = "Scene_LoadStarted";
    public const string SCENE_LOAD_COMPLETED = "Scene_LoadCompleted";
    
    // ===== GAME EVENTS =====
    public const string GAME_STARTED = "Game_Started";
    public const string GAME_PAUSED = "Game_Paused";
    public const string GAME_RESUMED = "Game_Resumed";
}
