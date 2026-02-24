using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenuBaseState_Main : GameMenuBaseState
{
    // private Dictionary<string, ItemView> itemViews = new Dictionary<string, ItemView>();
    private List<ItemView> activeItemViews = new List<ItemView>();
    private List<Action<int, ItemData[], Action>> activeResolvers = new List<Action<int, ItemData[], Action>>();
    private Dictionary<string, int> collectionGoals = new Dictionary<string, int>();
    public GameMenuBaseState_Main(GameMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartCoroutine(StartGame());
        View.PauseButton.onClick.AddListener(OnPauseButtonClicked);
        GameEvents.OnMatchStartedEvent += HandleMatchStarted;
        GameEvents.OnGameOverEvent += HandleGameOver;
    }
    public override void Exit()
    {
        base.Exit();
        ClearExistingItems();
        GameEvents.OnMatchStartedEvent -= HandleMatchStarted;
        GameEvents.OnGameOverEvent -= HandleGameOver;
        View.PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
    }
    void HandleMatchStarted(LevelData levelData)
    {
        ClearExistingItems();
        collectionGoals.Clear();

        // 1. Loop through everything the level intends to spawn
        foreach (var spawnEntry in levelData.itemsToSpawn)
        {
            // 2. CHECK: Is this item actually supposed to be a "Collection Goal"?
            // We look for the UID in the itemsToCollect list
            bool isGoal = levelData.itemsToCollect.Exists(c => c == spawnEntry.itemUID);

            if (!isGoal) continue; // If it's not a goal, don't create a UI element for it

            // 3. Since it IS a goal, get the metadata and the count from spawnEntry
            var itemData = Metadata.Instance.itemDatabase.GetItemByUID(spawnEntry.itemUID);
            int targetCount = spawnEntry.count;

            // 4. Instantiate the View
            ItemView itemView = GameObject.Instantiate<ItemView>(View.ItemViewPrefab, View.ItemViewParent);
            itemView.SetItem(itemData, targetCount);
            activeItemViews.Add(itemView);

            // 5. Track the goal
            collectionGoals[itemData.UID] = targetCount;
            ItemView currentView = itemView;

            Action<int, ItemData[], Action> resolver = (_, datas, _) =>
            {
                if (currentView != null && datas.Length > 0 && itemData.UID == datas[0].UID)
                {
                    currentView.UpdateCount(-3);

                    if (collectionGoals.ContainsKey(itemData.UID))
                    {
                        collectionGoals[itemData.UID] -= 3;

                        if (collectionGoals[itemData.UID] <= 0)
                        {
                            RemoveItemView(currentView);
                        }

                        CheckWinCondition();
                    }
                }
            };

            GameEvents.OnRequestMatchResolveEvent += resolver;
            activeResolvers.Add(resolver);
        }
    }
    private void RemoveItemView(ItemView view)
    {
        if (view == null) return;

        // Remove from our tracking list so ClearExistingItems doesn't try to destroy it again
        if (activeItemViews.Contains(view))
        {
            activeItemViews.Remove(view);
        }

        // Optionally: Play a "Goal Reached" sound or particle effect here
        GameObject.Destroy(view.gameObject);
    }
    private void CheckWinCondition()
    {
        // If all values in the dictionary are 0 or less, the player wins
        bool allCollected = true;
        foreach (var remaining in collectionGoals.Values)
        {
            if (remaining > 0)
            {
                allCollected = false;
                break;
            }
        }

        if (allCollected)
        {
            // Trigger GameOver with win = true
            // We use a small delay or check to ensure UI finishes animating
            // GameEvents.OnGameOverEvent?.Invoke(true);
            GameEvents.OnItemsCollectedEvent?.Invoke();
        }
    }
    private void ClearExistingItems()
    {
        if (activeResolvers.Count > 0)
        {
            // Copy to array to avoid "Collection Modified" during unsubscription
            var resolversToClear = activeResolvers.ToArray();
            foreach (var resolver in resolversToClear)
            {
                GameEvents.OnRequestMatchResolveEvent -= resolver;
            }
            activeResolvers.Clear();
        }

        foreach (var view in activeItemViews)
        {
            if (view != null) GameObject.Destroy(view.gameObject);
        }
        activeItemViews.Clear();
    }
    private void HandleGameOver(bool win)
    {
        MenuManager.Instance.GoBack();

        // show victory/los screen
    }

    IEnumerator StartGame()
    {
        yield return null;
        Controller.StartGame();
    }

    private void OnPauseButtonClicked()
    {
        MenuManager.Instance.OpenMenu<PauseMenuView, PauseMenuController, PauseMenuData>(Menus.Type.Pause);
    }
}
