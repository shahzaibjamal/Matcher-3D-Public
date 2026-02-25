using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMenuBaseState_Main : GameMenuBaseState
{
    private List<ItemView> _activeItemViews = new List<ItemView>();
    private List<ItemView> _activeViews = new List<ItemView>();


    public GameMenuBaseState_Main(GameMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        View.StartCoroutine(StartGame());
        View.PauseButton.onClick.AddListener(OnPauseButtonClicked);

        GameEvents.OnMatchStartedEvent += HandleMatchStarted;
    }

    public override void Exit()
    {
        base.Exit();

        GameEvents.OnMatchStartedEvent -= HandleMatchStarted;
        View.PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
    }

    private void HandleMatchStarted(LevelData levelData)
    {
        // Cleanup old
        foreach (var v in _activeViews) if (v) GameObject.Destroy(v.gameObject);
        _activeViews.Clear();

        // Create new
        foreach (var spawn in levelData.itemsToSpawn)
        {
            if (!levelData.itemsToCollect.Contains(spawn.itemUID)) continue;

            var data = Metadata.Instance.itemDatabase.GetItemByUID(spawn.itemUID);
            var view = GameObject.Instantiate(View.ItemViewPrefab, View.ItemViewParent);

            view.SetItem(data, spawn.count, () =>
            {
                _activeViews.Remove(view);
                CheckWin();
            });

            _activeViews.Add(view);
        }
    }

    private void CheckWin()
    {
        if (_activeViews.Count == 0)
        {
            GameEvents.OnItemsCollectedEvent?.Invoke();
        }
    }
    private void HandleGameOver(bool win) => MenuManager.Instance.GoBack();

    private void OnPauseButtonClicked() =>
        MenuManager.Instance.OpenMenu<PauseMenuView, PauseMenuController, PauseMenuData>(Menus.Type.Pause);

    IEnumerator StartGame()
    {
        yield return null;
        Controller.StartGame();
    }
}