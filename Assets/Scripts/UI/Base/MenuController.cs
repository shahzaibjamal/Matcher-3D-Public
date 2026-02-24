public abstract class MenuController<TView, TData> : IMenuController
    where TView : IMenuView
    where TData : MenuData
{
    public TView View { get; private set; }
    public TData Data { get; private set; }

    public IMenuState CurrentState { get; private set; }

    public void Bind(TView view, TData data)
    {
        View = view;
        Data = data;
    }

    public void SetState(IMenuState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public abstract void OnEnter();

    public virtual void OnExit()
    {
        CurrentState?.Exit();
        CurrentState = null;
    }

    public abstract void OnPause();

    public abstract void OnResume();
}