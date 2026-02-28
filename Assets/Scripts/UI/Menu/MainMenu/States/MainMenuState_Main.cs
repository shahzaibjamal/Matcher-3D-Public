
using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class MainMenuBaseState_Main : MainMenuBaseState
{
    private Sequence _idleSequence;

    private Sequence _playButtonSequence;

    public MainMenuBaseState_Main(MainMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartButton.onClick.AddListener(OnStartButtonClicked);
        View.DebugButton.onClick.AddListener(OnDebugButtonClicked);
        View.SettingsButton.onClick.AddListener(OnSettingsButtonClicked);
        GameEvents.OnGoldUpdatedEvent += HandleGoldUpdate;

        StartPlayButtonAnimation();
        View.GoldMainView.UpdateAmount(GameManager.Instance.SaveData.Inventory.Gold);
    }
    public override void Exit()
    {
        View.StartButton.onClick.RemoveListener(OnStartButtonClicked);
        View.DebugButton.onClick.RemoveListener(OnDebugButtonClicked);
        View.SettingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        GameEvents.OnGoldUpdatedEvent -= HandleGoldUpdate;
        base.Exit();
    }

    private void HandleGoldUpdate(int amount)
    {
        View.GoldMainView.UpdateAmount(amount);
    }

    public void StartPlayButtonAnimation()
    {
        _playButtonSequence?.Kill();

        Transform btn = View.StartButton.transform;
        btn.localScale = Vector3.one;

        _playButtonSequence = DOTween.Sequence();

        _playButtonSequence.AppendInterval(4.0f);

        _playButtonSequence.Append(btn.DOScale(new Vector3(1.15f, 1.15f, 1f), 0.1f).SetEase(Ease.OutBack));

        _playButtonSequence.Append(btn.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad));

        _playButtonSequence.Append(btn.DOScale(new Vector3(1.08f, 1.08f, 1f), 0.08f).SetEase(Ease.OutQuad));
        _playButtonSequence.Append(btn.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutElastic, 0.3f, 0.5f));

        _playButtonSequence.SetLoops(-1, LoopType.Restart);
    }
    public void StartIdleAnimation()
    {
        _idleSequence?.Kill();

        Transform btn = View.StartButton.transform;
        btn.localScale = Vector3.one;

        _idleSequence = DOTween.Sequence();

        // 1. THE LONG WAIT (The "Slow" part of the InCirc curve)
        // We stay at scale 1 for 2.2 seconds of the 3-second loop
        _idleSequence.AppendInterval(2.2f);

        // 2. THE ANTICIPATION (Starting to accelerate)
        // A quick, sharp stretch upwards
        _idleSequence.Append(btn.DOScale(new Vector3(0.7f, 1.4f, 1f), 0.3f).SetEase(Ease.InCirc));

        // 3. THE "POP" (The sharp peak of the curve)
        // Rapidly slam into the squish
        _idleSequence.Append(btn.DOScale(new Vector3(1.3f, 0.5f, 1f), 0.1f).SetEase(Ease.OutQuad));

        // 4. THE SETTLE (The "Slow" ending/recovery)
        // Jiggly bounce back to rest
        _idleSequence.Append(btn.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic, 0.5f, 0.5f));

        // Loop the whole thing
        _idleSequence.SetLoops(-1, LoopType.Restart);
        // _idleSequence.Insert(1.2f, View.StartButton.transform.DOPunchPosition(Vector3.up * 20f, 0.4f, 2, 0.5f));
    }


    private void OnStartButtonClicked()
    {
        Controller.StartButtonClicked();
    }
    private void OnSettingsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SettingsMenuView, SettingsMenuController, SettingsMenuData>(Menus.Type.Settings);
    }
    private void OnDebugButtonClicked()
    {
        // MenuManager.Instance.OpenMenu<MatchResultMenuView, MatchResultMenuController, MatchResultMenuData>(Menus.Type.MatchResult, new MatchResultMenuData
        // {
        //     IsWin = true,
        //     GoldAmount = 27,
        //     Level = 1,
        //     MatchRate = 0.9f
        // });
        MenuManager.Instance.OpenMenu<DebugMenuView, DebugMenuController, DebugMenuData>(Menus.Type.Debug);
    }
}
