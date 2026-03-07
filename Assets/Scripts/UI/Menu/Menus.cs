public static class Menus
{
    public enum Type
    {
        None = 0,
        Main = 1,
        HUD = 2,
        Settings = 3,
        Inventory = 4,
        PlayerInfo = 5,
        Game = 6,
        ConfirmationPopup = 7,
        Debug = 8,
        Pause = 9,
        Reward = 10,
        MatchResult = 11,
        Loading = 12,
        GenericPopup = 13,

        DailyReward = 14,

        SpinWheel = 15,
        LevelSelect = 16,
        MatchhLose = 17,

    }
    public enum MenuDisplayMode
    {
        ScreenReplace, // Default: Hides/Pauses previous
        Popup,         // Overlays: Previous stays visible/active
        Additive,       // Parallel: Previous stays visible but might pause

        Overlay
    }
}