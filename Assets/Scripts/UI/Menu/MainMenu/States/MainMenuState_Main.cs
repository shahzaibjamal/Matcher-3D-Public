
using DG.Tweening;
using UnityEngine;

public class MainMenuBaseState_Main : MainMenuBaseState
{

    private Sequence _playButtonSequence;
    private Sequence _giftSeq;
    private Sequence _spinSeq;
    public MainMenuBaseState_Main(MainMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        View.StartButton.onClick.AddListener(OnStartButtonClicked);
        View.DebugButton.onClick.AddListener(OnDebugButtonClicked);
        View.SettingsButton.onClick.AddListener(OnSettingsButtonClicked);
        View.GiftButton.onClick.AddListener(OnGiftButtonClicked);
        View.DailySpinButton.onClick.AddListener(OnDailySpinButtonClicked);
        View.DailyRewardButton.onClick.AddListener(OnDailyRewardsButtonClicked);
        View.StoreButton.onClick.AddListener(OnStoreButtonClicked);
        GameEvents.OnGoldUpdatedEvent += HandleGoldUpdate;

        StartPlayButtonAnimation();
        StartGiftAnimation();
        StartSpinAnimation();
        StartShimmerAnimation(View.DailyRewardButton.transform);
        StartShimmerAnimation(View.StoreButton.transform);
        View.GoldMainView.UpdateAmount(GameManager.Instance.SaveData.Inventory.Gold);
        View.StoreShimmer.Play();
        View.RewardShimmer.Play();

        Scheduler.Instance.ExecuteAfterDelay(0.5f, () => RewardManager.Instance.CheckAndShowNext());
    }

    public override void Exit()
    {
        View.StartButton.onClick.RemoveListener(OnStartButtonClicked);
        View.DebugButton.onClick.RemoveListener(OnDebugButtonClicked);
        View.SettingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        View.GiftButton.onClick.RemoveListener(OnGiftButtonClicked);
        View.DailySpinButton.onClick.RemoveListener(OnDailySpinButtonClicked);
        View.DailyRewardButton.onClick.RemoveListener(OnDailyRewardsButtonClicked);
        View.StoreButton.onClick.RemoveListener(OnStoreButtonClicked);
        GameEvents.OnGoldUpdatedEvent -= HandleGoldUpdate;


        // 1. Kill Sequences
        _playButtonSequence?.Kill();
        _giftSeq?.Kill();
        _spinSeq?.Kill();

        // 2. Kill all tweens on the specific Transforms (Cleans up Shimmers and anonymous tweens)
        View.StartButton.transform.DOKill();
        View.GiftButton.transform.DOKill();
        View.DailySpinButton.transform.DOKill();
        View.DailyRewardButton.transform.DOKill();
        View.StoreButton.transform.DOKill();

        // Reset scales/rotations to default so they don't look "stuck"
        View.StartButton.transform.localScale = Vector3.one;
        View.GiftButton.transform.localScale = Vector3.one;
        View.DailySpinButton.transform.localRotation = Quaternion.identity;
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

    private void StartGiftAnimation()
    {
        Transform t = View.GiftButton.transform;

        // 1. Reset and Cleanup
        t.DOKill();
        t.localScale = Vector3.one;

        _giftSeq = DOTween.Sequence();

        // 2. IDLE "BREATHING" (The squishy bubble pulse)
        // This runs before the jump to make it look alive
        _giftSeq.Append(t.DOScale(new Vector3(1.1f, 0.92f, 1f), 0.8f).SetEase(Ease.InOutSine))
               .Append(t.DOScale(new Vector3(0.95f, 1.08f, 1f), 0.8f).SetEase(Ease.InOutSine));

        // 3. THE JUMP (Stretch while rising)
        _giftSeq.Append(t.DOLocalMoveY(20f, 0.45f).SetRelative().SetEase(Ease.OutQuad))
               .Join(t.DOScale(new Vector3(0.85f, 1.2f, 1f), 0.45f).SetEase(Ease.OutQuad));

        // 4. THE FALL (Start flattening for impact)
        _giftSeq.Append(t.DOLocalMoveY(-20f, 0.35f).SetRelative().SetEase(Ease.InQuad))
               .Join(t.DOScale(new Vector3(1.2f, 0.85f, 1f), 0.35f).SetEase(Ease.InQuad));
        _giftSeq.Append(t.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad));
        // 5. THE BUBBLE IMPACT (The magic "Boing")
        // DOPunchScale makes it wobble/vibrate like jelly
        // Parameters: (Strength, Duration, Vibrato, Elasticity)
        _giftSeq.AppendCallback(() =>
        {
            t.DOPunchScale(new Vector3(0.5f, -0.5f, 0f), 1.2f, 5, 1f);
        });

        // 6. DELAY & LOOP
        _giftSeq.AppendInterval(3.0f); // Wait for the wobble to settle
        _giftSeq.SetLoops(-1);
    }

    private void StartSpinAnimation()
    {
        float totalRotation = -2880f;

        _spinSeq = DOTween.Sequence();

        // Ease.OutCubic starts fast and spends most of the time "braking"
        _spinSeq.Append(View.DailySpinButton.transform.DORotate(new Vector3(0, 0, totalRotation), 2.0f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic))
               .AppendInterval(3.0f) // Total 5 second cycle
                .SetLoops(-1);
    }
    private void StartShimmerAnimation(Transform target)
    {
        // Subtle pulse for Store/Reward
        target.DOScale(1.1f, 1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }
    private void OnStartButtonClicked()
    {
        Controller.StartButtonClicked();
    }
    private void OnSettingsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SettingsMenuView, SettingsMenuController, SettingsMenuData>(Menus.Type.Settings);
    }

    private void OnDailyRewardsButtonClicked()
    {
        MenuManager.Instance.OpenMenu<DailyRewardMenuView, DailyRewardMenuController, DailyRewardMenuData>(Menus.Type.DailyReward);

    }
    private void OnDailySpinButtonClicked()
    {
        MenuManager.Instance.OpenMenu<SpinWheelMenuView, SpinWheelMenuController, SpinWheelMenuData>(Menus.Type.SpinWheel);

    }
    private void OnStoreButtonClicked()
    {
        MenuManager.Instance.OpenMenu<LevelSelectMenuView, LevelSelectMenuController, LevelSelectMenuData>(Menus.Type.LevelSelect);

    }
    private void OnGiftButtonClicked()
    {
    }
    private void OnDebugButtonClicked()
    {
        MenuManager.Instance.OpenMenu<DebugMenuView, DebugMenuController, DebugMenuData>(Menus.Type.Debug);
    }
}
