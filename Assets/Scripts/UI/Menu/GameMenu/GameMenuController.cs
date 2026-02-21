using System;
using UnityEngine;

public class GameMenuController : MenuController<GameMenuView, GameMenuData>
{

    public static event Action OnGameStarted;

    public override void OnEnter()
    {
        SetState(new GameMenuBaseState_Main(this));
    }
    public override void OnExit()
    {
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public void StartGame()
    {
        OnGameStarted?.Invoke();
        View.TrayView.Initialize(Data.SlotCount);
    }
}