using System.Collections.Generic;
using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class GameMenuBaseState_Main : GameMenuBaseState
{
    private List<ItemView> _activeViews = new List<ItemView>();
    private List<PowerUpButton> _activeButtons = new List<PowerUpButton>();
    private LevelData _currentLevelData = null;
    private Vector3 _leftCurtainPosition;
    private Vector3 _rightCurtainPosition;
    Sequence _curtainSeq;

    public GameMenuBaseState_Main(GameMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        View.PauseButton.onClick.AddListener(OnPauseButtonClicked);
        GameEvents.OnMatchStartedEvent += HandleMatchStarted;
        GameEvents.OnShowMatchResultEvent += HandleMatchResult;
        GameEvents.OnCleanSweepTrayEvent += HandleCleanSweep;
        GameEvents.OnSpawnerInitializedEvent += OnSpawnerInitialized;
        GameEvents.OnHintPowerupEvent += HandleHintPowerUp;

        View.GoldMainView.UpdateAmount(GameManager.Instance.SaveData.Inventory.Gold);
        View.TrayView.Initialize(GameManager.SLOT_COUNT);

        InputManager.Instance.RegisterKey(KeyCode.Z, HandleHintPowerUp);
        _leftCurtainPosition = View.LeftCurtain.anchoredPosition;
        _rightCurtainPosition = View.RightCurtain.anchoredPosition;
    }

    public override void Exit()
    {
        base.Exit();
        InputManager.Instance.UnregisterKey(KeyCode.Z, OnSpawnerInitialized);

        GameEvents.OnMatchStartedEvent -= HandleMatchStarted;
        GameEvents.OnShowMatchResultEvent -= HandleMatchResult;
        GameEvents.OnCleanSweepTrayEvent -= HandleCleanSweep;
        GameEvents.OnSpawnerInitializedEvent -= OnSpawnerInitialized;
        GameEvents.OnHintPowerupEvent -= HandleHintPowerUp;
        View.PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        Cleanup();
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


            // FTUE //
            if (!FTUEManager.Instance.IsSequenceCompleted("Opening") && data.Id == DataManager.Instance.Metadata.Levels[0].ItemsToCollect[0])
            {
                var target = view.gameObject.AddComponent<FTUETarget>();
                target.Init("ItemView");
            }

            _activeViews.Add(view);
        }
        SetupPowerUps();
    }


    public void SetupPowerUps()
    {
        // Create a button for every type defined in the Enum
        foreach (PowerUpType type in System.Enum.GetValues(typeof(PowerUpType)))
        {
            int amount = GameManager.Instance.SaveData.Inventory.GetPowerUpCount(type);

            PowerUpButton btn = GameObject.Instantiate(View.PowerUpPrefab, View.PowerUpContainer);

            // Fetch the specific sprite for this type from our SO database
            Sprite icon = View.PowerUpVisualDatabase != null ? View.PowerUpVisualDatabase.GetIcon(type) : null;
            btn.Initialize(type, amount, icon);
            _activeButtons.Add(btn);


            var ftueTarget = btn.gameObject.AddComponent<FTUETarget>();
            ftueTarget.Init(type.ToString());
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
        _curtainSeq.Kill();
        View.CurtainContainer.SetActive(false);
    }

    private void CheckWin()
    {
        if (_activeViews.Count == 0)
        {
            GameEvents.OnItemsCollectedEvent?.Invoke();
            GameEvents.OnGameOverEvent?.Invoke(true);
        }
    }
    private void HandleMatchResult(bool win, float matchRate)
    {
        if (win)
        {
            AdManager.Instance.TryShowInterstitial();
            MenuManager.Instance.OpenMenu<MatchResultMenuView, MatchResultMenuController, MatchResultMenuData>(Menus.Type.MatchResult, new MatchResultMenuData
            {
                IsWin = win,
                LevelData = _currentLevelData,
                MatchRate = matchRate
            });

        }
        else
        {
            MenuManager.Instance.OpenMenu<MatchLoseMenuMenuView, MatchLoseMenuMenuController, MatchLoseMenuMenuData>(Menus.Type.MatchLose);
        }
    }

    private void HandleCleanSweep()
    {
        View.BroomSweeper.PlayBroomSweep();
    }
    private void HandleHintPowerUp()
    {
        View.MagnifyingGlassSearch.PlaySearch();
    }

    private void OnPauseButtonClicked() => Controller.OpenPauseMenu();

    private void OnSpawnerInitialized()
    {
        Debug.LogError("Called");
        View.CurtainContainer.SetActive(true);
        View.BlackCurtain.alpha = 1.0f;
        float screenWidth = View.GetComponent<RectTransform>().rect.width;

        // 2. Create the Sequence
        Sequence _curtainSeq = DOTween.Sequence();
        View.LeftCurtain.anchoredPosition = _leftCurtainPosition;
        View.RightCurtain.anchoredPosition = _rightCurtainPosition;

        _curtainSeq.Append(View.LeftCurtain.DOAnchorPosX(View.useOutward ? -View.outward : -screenWidth, 0.6f).SetEase(Ease.InBack));
        _curtainSeq.Join(View.RightCurtain.DOAnchorPosX(View.useOutward ? View.outward : screenWidth, 0.6f).SetEase(Ease.InBack));
        _curtainSeq.Join(View.BlackCurtain.DOFade(0.0f, 0.6f).SetEase(Ease.InBack));
        _curtainSeq.OnComplete(() =>
        {
            CheckForFTUE();
        });
    }

    private void CheckForFTUE()
    {

        if (!FTUEManager.Instance.IsSequenceCompleted("Opening") && GameManager.Instance.SaveData.CurrentLevelID == "level_01")
        {
            FTUEManager.Instance.PlayTutorial("Opening");
        }
        if (!FTUEManager.Instance.IsSequenceCompleted("Hint") && GameManager.Instance.SaveData.CurrentLevelID == "level_03")
        {
            FTUEManager.Instance.PlayTutorial("Hint");
        }
        if (!FTUEManager.Instance.IsSequenceCompleted("Shake") && GameManager.Instance.SaveData.CurrentLevelID == "level_04")
        {
            FTUEManager.Instance.PlayTutorial("Shake");
        }
        if (!FTUEManager.Instance.IsSequenceCompleted("Magnet") && GameManager.Instance.SaveData.CurrentLevelID == "level_05")
        {
            FTUEManager.Instance.PlayTutorial("Magnet");
        }

    }
}