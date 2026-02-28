using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuBaseState : MenuBaseState<MatchResultMenuController, MatchResultMenuView, MatchResultMenuData>
{
    private Sequence _buttonSequence;

    public MatchResultMenuBaseState(MatchResultMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        View.GoldMainView.Initialize(GameManager.Instance.SaveData.Inventory.Gold);
        ShowMatchResultAnimation();
    }


    public override void Exit()
    {
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
            StartButtonGlow(View.GoldMulitplierButton);
        });
        ResetView();
    }

    private void ResetView()
    {
        for (int i = 0; i < View.StarViews.Length; i++)
        {
            View.StarViews[i].ResetView();
        }
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
        openSeq.OnComplete(OnMenuOpenAnimationComplete);
    }


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

    protected virtual void OnMenuOpenAnimationComplete()
    {
        // implemented by inherited state classes
    }

}