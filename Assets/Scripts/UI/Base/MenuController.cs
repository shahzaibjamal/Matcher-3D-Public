public abstract class MenuController<TView, TData> : IMenuController
    where TView : IMenuView
    where TData : MenuData
{
    public TView View { get; private set; }
    public TData Data { get; private set; }

    private IMenuState _currentState;

    public void Bind(TView view, TData data)
    {
        View = view;
        Data = data;
    }

    public void SetState(IMenuState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    public abstract void OnEnter();

    public virtual void OnExit()
    {
        _currentState?.Exit();
        _currentState = null;
    }

    public abstract void OnPause();

    public abstract void OnResume();
}