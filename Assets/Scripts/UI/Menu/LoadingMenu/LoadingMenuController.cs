using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class LoadingMenuController : MenuController<LoadingMenuView, LoadingMenuData>
{
    private Sequence _barSequence;

    public override void OnEnter()
    {
        // Set state if you're using a State Machine pattern
        SetState(new LoadingMenuBaseState_Main(this));

        // Use Data for timing logic, fallback to defaults if Data is null
        float min = View.MinTime;
        float max = Data.Delay == -1 ? View.MaxTime : Data.Delay;
        float duration = Random.Range(min, max);

        SetupProgressBar(duration);

        // Actual logic timer
        DOVirtual.DelayedCall(duration, () =>
        {
            // Manager handles the FadeOut and Destroy logic via OnExit callback
            MenuManager.Instance.GoBack();
            Data?.OnLoadingComplete?.Invoke();
        });
    }

    public void SetupProgressBar(float totalDuration)
    {
        // 1. Reset Progress Bar (The 90/10 logic you liked)
        float fastTime = totalDuration * 0.7f;
        float slowTime = totalDuration * 0.3f;

        View.ProgressSlider.value = 0;
        View.ProgressSlider.DOValue(0.8f, fastTime).SetEase(Ease.OutSine);
        View.ProgressSlider.DOValue(1.0f, slowTime).SetEase(Ease.Linear).SetDelay(fastTime);
    }

    public override void OnExit()
    {

        Debug.LogError("OnExit called");
        // 1. Kill the Logic Timer (The most dangerous one!)
        // Using DOTween.Kill(target) on the View or Data is a good safety measure
        DOTween.Kill(View.ProgressSlider);
        DOTween.Kill(View.IconCenter);

        // 2. Kill the DelayedCall
        // Since we didn't store the DelayedCall in a variable, 
        // we should kill all tweens related to the Controller or View
        DOTween.Kill(this);

        // 3. Kill the Orbital Icons
        foreach (var icon in View.OrbitalIcons)
        {
            icon.DOKill();
        }

        // 4. Kill the specific Sequence and Bar Fill
        _barSequence?.Kill();
        View.BarFillArea.DOKill();

        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public override void HandleBackInput()
    {
        // no back implemetation
    }
}