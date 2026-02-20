public abstract class MenuBaseState<TController, TView, TData> : IMenuState
    where TController : MenuController<TView, TData>
    where TView : MenuView
    where TData : MenuData
{
    protected readonly TController Controller;

    // Quick access helpers
    protected TView View => Controller.View;
    protected TData Data => Controller.Data;

    protected MenuBaseState(TController controller)
    {
        Controller = controller;
    }

    public abstract void Enter();
    public abstract void Exit();
}