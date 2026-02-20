using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMenuBaseState_Main : GameMenuBaseState
{
    private Dictionary<string, ItemView> itemViews = new Dictionary<string, ItemView>();


    public GameMenuBaseState_Main(GameMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartCoroutine(StartGame());
    }

    public override void Exit()
    {
        base.Exit();
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(3.0f);
        Controller.StartGame();
        View.SlotManager.gameObject.SetActive(true);
    }
}
