public interface IMenuView
{
    Menus.MenuDisplayMode DisplayMode { get; }
    void SetVisible(bool visible);
    void Destroy();
    void OnEnter();
    void OnExit(System.Action onComplete);
}