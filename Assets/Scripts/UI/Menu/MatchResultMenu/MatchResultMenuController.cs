using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuController : MenuController<MatchResultMenuView, MatchResultMenuData>
{
    public override void OnEnter()
    {
        if (Data.IsWin)
        {
            SetState(new MatchResultMenuBaseState_Win(this));
        }
        else
        {
            SetState(new MatchResultMenuBaseState_Lose(this));
        }
        View.ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.AddListener(OnGoldMultiplierButtonClicked);
        ShowMatchResultAnimation();
        View.GoldRewardView.SetInitialHudAmount(978);
    }

    private void ShowMatchResultAnimation()
    {
        View.Root.alpha = 0.0f;
        View.GodRays.SetActive(false);

        Scheduler.Instance.ExecuteAfterDelay(0.5f, ShowMenu);

        View.GoldMulitplierButton.interactable = false;
        View.ContinueButton.interactable = false;
        Scheduler.Instance.ExecuteAfterDelay(3.0f, () =>
        {
            View.ContinueButton.interactable = true;
            View.GoldMulitplierButton.interactable = true;
            View.TextAnimation.PlayReveal();
            StartButtonGlow(View.GoldMulitplierButton);
        });
        ResetView();
    }
    public override void OnExit()
    {
        View.Root.alpha = 0.0f;
        View.GodRays.SetActive(false);
        View.ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.RemoveListener(OnGoldMultiplierButtonClicked);

        base.OnExit();
    }

    private void ResetView()
    {
        for (int i = 0; i < View.StarViews.Length; i++)
        {
            View.StarViews[i].ResetView();
        }
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public void ShowMenu()
    {
        // 1. Prepare initial state
        View.Root.DOKill(); // Prevent overlapping tweens
        View.Root.alpha = 0f; // Assuming View.Root is a CanvasGroup
        View.Root.transform.localScale = Vector3.one * 0.8f;

        // 2. The Pop-In Sequence
        Sequence openSeq = DOTween.Sequence();

        // Fade in
        openSeq.Append(View.Root.DOFade(1.0f, 0.4f).SetEase(Ease.OutCubic));

        // Scale up with overshoot (0.8 -> 1.1 -> 1.0)
        // Ease.OutBack naturally handles the 1.1 overshoot for you!
        openSeq.Join(View.Root.transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutBack));

        // 3. Trigger the nested animations
        openSeq.OnComplete(() =>
        {
            AnimateAllStars(View.StarCount);
        });
    }

    public void AnimateAllStars(int score)
    {
        float delay = 0f;
        for (int i = 0; i < View.StarViews.Length; i++)
        {
            delay = i * View.StarsApearDelay;
            if (i < score)
            {
                // Stagger by 0.3 seconds each: 0.0s, 0.3s, 0.6s
                View.StarViews[i].Show(delay);
            }
            else
            {
                View.StarViews[i].ResetView();
            }
        }

        // 1. Kill any existing rotation to prevent stacking
        View.GodRays.SetActive(true);
        View.GodRays.transform.DOKill();

        // 2. Start the endless rotation
        View.GodRays.transform.DORotate(new Vector3(0, 0, 360), 10f, RotateMode.FastBeyond360)
            .SetDelay(delay)            // Wait before starting
            .SetEase(Ease.Linear)      // Constant speed (essential for loops)
            .SetLoops(-1, LoopType.Incremental); // -1 means infinite    
        View.GoldRewardView.ShowReward(87, delay, null);

    }
    private Sequence _buttonSequence;
    public void StartButtonGlow(Button targetButton)
    {
        // Kill any existing animation to prevent stacking
        _buttonSequence?.Kill();
        targetButton.transform.localScale = Vector3.one;

        _buttonSequence = DOTween.Sequence();
        _buttonSequence.SetDelay(2.5f);
        // 1. Subtle Pulse (Scale 1.0 -> 1.08)
        _buttonSequence.Append(targetButton.transform.DOScale(1.08f, 0.8f).SetEase(Ease.InOutSine));
        _buttonSequence.Append(targetButton.transform.DOScale(1.0f, 0.8f).SetEase(Ease.InOutSine));

        // 2. Add a little "wobble" every few seconds
        _buttonSequence.Insert(0.2f, targetButton.transform.DOPunchRotation(new Vector3(0, 0, 5f), 0.5f, 10, 1f));

        // 3. Loop infinitely
        _buttonSequence.SetLoops(-1, LoopType.Restart);
    }

    void OnContinueButtonClicked()
    {
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Settings);
    }

    void OnGoldMultiplierButtonClicked()
    {
        ShowMatchResultAnimation();
    }
}