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
            Spawner.SpawnGameSystems();
        });
        GameEvents.OnMatchStarted += HandleMatchStarted;
    }


    void HandleMatchStarted(LevelData levelData)
    {
        foreach (var levelItemEntry in levelData.itemsToSpawn)
        {
            var itemData = Metadata.Instance.itemDatabase.GetItemByUID(levelItemEntry.itemUID);

            ItemView itemView = GameObject.Instantiate<ItemView>(View.ItemViewPrefab, View.ItemViewParent);
            itemView.SetItem(itemData, levelItemEntry.count);
            GameEvents.OnRequestMatchResolve += (_, datas, _) =>
            {
                if (datas.Length > 0 && itemData.UID == datas[0].UID)
                {
                    itemView.UpdateCount(-3);
                }
            };
        }
    }

    public override void Exit()
    {
        base.Exit();
        GameEvents.OnMatchStarted -= HandleMatchStarted;

    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(0.5f);
        Controller.StartGame();
    }
}
