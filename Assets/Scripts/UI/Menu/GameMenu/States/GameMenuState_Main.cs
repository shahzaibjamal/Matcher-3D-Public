using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        View.RestartButton.onClick.AddListener(() =>
        {
            // Scene currentScene = SceneManager.GetActiveScene(); // Reload it by name 
            // SceneManager.LoadScene(currentScene.name);
            Spawner.SpawnGameSystems();
        });
    }

    public override void Exit()
    {
        base.Exit();
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(0.5f);
        Controller.StartGame();
    }
}
