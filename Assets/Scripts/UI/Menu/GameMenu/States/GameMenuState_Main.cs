using System;
using System.Collections;
using System.Collections.Generic;
using TS.LocalizationSystem;
using UnityEngine;

public class GameMenuBaseState_Main : GameMenuBaseState
{
    private List<ItemView> _activeViews = new List<ItemView>();
    private List<PowerUpButton> _activeButtons = new List<PowerUpButton>();
    private LevelData _currentLevelData = null;

    public GameMenuBaseState_Main(GameMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        View.StartCoroutine(StartGame());
        View.PauseButton.onClick.AddListener(OnPauseButtonClicked);
        GameEvents.OnMatchStartedEvent += HandleMatchStarted;
        GameEvents.OnShowMatchResultEvent += HandleMatchResult;
        GameEvents.OnCleanSweepTrayEvent += HandleCleanSweep;

        View.GoldMainView.UpdateAmount(GameManager.Instance.SaveData.Inventory.Gold);
        Debug.LogError("GameMenu : OnterCalled called ");

    }

    public override void Exit()
    {
        base.Exit();

        GameEvents.OnMatchStartedEvent -= HandleMatchStarted;
        GameEvents.OnShowMatchResultEvent -= HandleMatchResult;
        GameEvents.OnCleanSweepTrayEvent -= HandleCleanSweep;
        View.PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        Cleanup();
        Debug.LogError("GameMenu : OnExit called ");
    }

    private void HandleMatchStarted(LevelData levelData)
    {
        _currentLevelData = levelData;
        View.LevelId.text = string.Format(LocaleManager.Localize(LocalizationKeys.title_level), levelData.Number);
        // Create new
        foreach (var spawn in levelData.ItemsToSpawn)
        {
            if (!levelData.ItemsToCollect.Contains(spawn.Id)) continue;

            var data = DataManager.Instance.GetItemByID(spawn.Id);
            var view = GameObject.Instantiate(View.ItemViewPrefab, View.ItemViewParent);

            view.SetItem(data, spawn.Count, () =>
            {
                _activeViews.Remove(view);
                CheckWin();
            });

            _activeViews.Add(view);
        }
        SetupPowerUps();
    }


    public void SetupPowerUps()
    {
        // Create a button for every type defined in the Enum
        foreach (PowerUpType type in Enum.GetValues(typeof(PowerUpType)))
        {
            PowerUpButton btn = GameObject.Instantiate(View.PowerUpPrefab, View.PowerUpContainer);

            // Fetch the specific sprite for this type from our SO database
            Sprite icon = View.PowerUpVisualDatabase != null ? View.PowerUpVisualDatabase.GetIcon(type) : null;
            int amount = GameManager.Instance.SaveData.Inventory.GetPowerUpCount(type);
            btn.Initialize(type, amount, icon);
            _activeButtons.Add(btn);
        }
    }

    private void Cleanup()
    {
        // Clear old powerup Button
        foreach (var child in _activeButtons) if (child != null) GameObject.Destroy(child.gameObject);
        _activeButtons.Clear();

        // Cleanup old itemviews
        foreach (var v in _activeViews) if (v) GameObject.Destroy(v.gameObject);
        _activeViews.Clear();

        _currentLevelData = null;
    }

    private void CheckWin()
    {
        if (_activeViews.Count == 0)
        {
            // Debug.LogError("GameMenuBaseState_Main: OnItemsCollectedEvent fired");
            GameEvents.OnItemsCollectedEvent?.Invoke();
            GameEvents.OnGameOverEvent?.Invoke(true);
        }
    }
    private void HandleMatchResult(bool win, float matchRate)
    {
        // Debug.LogError(" GameMenuBaseState_Main: HandleMatchResult");
        MenuManager.Instance.OpenMenu<MatchResultMenuView, MatchResultMenuController, MatchResultMenuData>(Menus.Type.MatchResult, new MatchResultMenuData
        {
            IsWin = win,
            LevelData = _currentLevelData,
            MatchRate = matchRate
        });
    }

    private void HandleCleanSweep()
    {
        View.BroomSweeper.PlayBroomSweep();
    }

    private void OnPauseButtonClicked() => Controller.OpenPauseMenu();

    IEnumerator StartGame()
    {
        yield return null;
        Controller.StartGame();
    }
}