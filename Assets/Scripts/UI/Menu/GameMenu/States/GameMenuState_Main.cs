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
        View.UndoButton.onClick.AddListener(OnUndoButtonClicked);
        View.ShakeButton.onClick.AddListener(OnShakeButtonClicked);
        View.HintButton.onClick.AddListener(OnHintButtonClicked);
        View.MagnetButton.onClick.AddListener(OnMagnetButtonClicked);
        // GameEvents.OnGameOverEvent += HandleGameOver;
        GameEvents.OnMatchStartedEvent += HandleMatchStarted;
        GameEvents.OnShowMatchResultEvent += HandleMatchResult;
    }

    public override void Exit()
    {
        base.Exit();

        GameEvents.OnMatchStartedEvent -= HandleMatchStarted;
        // GameEvents.OnGameOverEvent -= HandleGameOver;
        GameEvents.OnShowMatchResultEvent -= HandleMatchResult;
        View.PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        View.UndoButton.onClick.RemoveListener(OnUndoButtonClicked);
        View.ShakeButton.onClick.RemoveListener(OnShakeButtonClicked);
        View.HintButton.onClick.RemoveListener(OnHintButtonClicked);
        View.MagnetButton.onClick.RemoveListener(OnMagnetButtonClicked);
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
            Debug.LogError("GameMenuBaseState_Main: OnItemsCollectedEvent fired");
            GameEvents.OnItemsCollectedEvent?.Invoke();
            GameEvents.OnGameOverEvent?.Invoke(true);
        }
    }
    private void HandleMatchResult(bool win, float matchRate)
    {
        Debug.LogError(" GameMenuBaseState_Main: HandleMatchResult");
        MenuManager.Instance.OpenMenu<MatchResultMenuView, MatchResultMenuController, MatchResultMenuData>(Menus.Type.MatchResult, new MatchResultMenuData
        {
            IsWin = win,
            GoldAmount = 27,
            Level = 1,
            MatchRate = matchRate
        });
    }
    private void OnPauseButtonClicked() =>
        MenuManager.Instance.OpenMenu<PauseMenuView, PauseMenuController, PauseMenuData>(Menus.Type.Pause);

    IEnumerator StartGame()
    {
        yield return null;
        Controller.StartGame();
    }

    private void OnUndoButtonClicked()
    {
        GameEvents.OnUndoPowerupEvent?.Invoke();
    }
    private void OnShakeButtonClicked()
    {
        GameEvents.OnShakePowerupEvent?.Invoke();
    }
    private void OnMagnetButtonClicked()
    {
        GameEvents.OnMagnetPowerupEvent?.Invoke();
    }
    private void OnHintButtonClicked()
    {
        GameEvents.OnHintPowerupEvent?.Invoke();
    }
}