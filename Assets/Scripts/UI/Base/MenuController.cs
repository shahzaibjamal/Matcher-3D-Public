using System;
using Unity.VisualScripting;

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
    public virtual void HandleBackInput()
    {
        SoundController.Instance.PlaySoundEffect("popup_close");
        // 1. Generic behavior for temporary UI
        if (View.DisplayMode == Menus.MenuDisplayMode.Overlay ||
            View.DisplayMode == Menus.MenuDisplayMode.Popup)
        {
            MenuManager.Instance.GoBack();
            return;
        }

        // 2. Defensive check for ScreenReplace
        // If we reach this point, it means a full-screen menu was open 
        // and it didn't override this method to say where to go next.
        throw new System.NotImplementedException(
            $"Menu {View.GetType().Name} uses ScreenReplace but does not override HandleBackInput. " +
            "You must explicitly define which menu or state to transition to here."
        );
    }
}