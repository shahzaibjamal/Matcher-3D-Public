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
        float max = View.MaxTime;
        float duration = Random.Range(min, max);

        StartLoadingAnimations();
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
        View.ProgressSlider.DOValue(0.9f, fastTime).SetEase(Ease.OutBack);
        View.ProgressSlider.DOValue(1.0f, slowTime).SetEase(Ease.Linear).SetDelay(fastTime);
    }

    public void StartLoadingAnimations()
    {
        // 1. Initial Positioning (The Ring)
        float angleStep = 360f / View.OrbitalIcons.Length;

        for (int i = 0; i < View.OrbitalIcons.Length; i++)
        {
            RectTransform icon = View.OrbitalIcons[i];

            // Use Trig to place them in a circle based on your Radius variable
            float angleInRad = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleInRad) * View.Radius;
            float y = Mathf.Sin(angleInRad) * View.Radius;

            icon.anchoredPosition = new Vector2(x, y);

            // Reset their rotation to zero before starting
            icon.localRotation = Quaternion.identity;
        }

        // 2. The Global Orbit (The Parent)
        // Rotating the center moves all children in a circle
        View.IconCenter.DOLocalRotate(new Vector3(0, 0, -360), View.RotationSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);

        // 3. The Counter-Rotation (The Children)
        // Rotating the children in the opposite direction keeps them upright
        foreach (var icon in View.OrbitalIcons)
        {
            icon.DOLocalRotate(new Vector3(0, 0, 360), View.RotationSpeed, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }
    public override void OnExit()
    {
        // Kill all tweens to prevent memory leaks or callbacks on destroyed objects
        View.BarFillArea.DOKill();
        _barSequence?.Kill();

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