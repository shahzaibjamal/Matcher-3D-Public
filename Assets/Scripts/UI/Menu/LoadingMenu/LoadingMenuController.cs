using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class LoadingMenuController : MenuController<LoadingMenuView, LoadingMenuData>
{
    private bool _isRealWorkDone = false;

    public override void OnEnter()
    {
        SetState(new LoadingMenuBaseState_Main(this));

        // Start the process
        StartLoadingSequence();
    }

    private async void StartLoadingSequence()
    {
        // 1. Start the "Fake" smooth progress bar (0 to 0.9)
        // We give it a generous estimated time (e.g., 2 seconds)
        float delay = Random.Range(0, 3.0f);
#if UNITY_EDITOR
        delay = 1;
#endif

        float estimate = Data == null || Data.Delay == -1 ? delay : Data.Delay;
        SetupFakeProgress(estimate);

        // 2. Perform the ACTUAL work
        if (Data?.LoadingTask != null)
        {
            await Data.LoadingTask();
            await System.Threading.Tasks.Task.Delay((int)(estimate * 1000));

        }
        else
        {
            // Fallback if no task was provided
            await System.Threading.Tasks.Task.Delay((int)(estimate * 1000));
        }

        _isRealWorkDone = true;

        // 3. Snap progress to 100% and finish
        View.ProgressSlider.DOKill();
        await View.ProgressSlider.DOValue(1.0f, 0.2f).AsyncWaitForCompletion();

        // 4. Exit
        MenuManager.Instance.GoBack();
        Data?.OnLoadingComplete?.Invoke();
    }
    public void SetupFakeProgress(float estimatedTime)
    {
        View.ProgressSlider.value = 0;
        // Ease out to 90% so it feels like it's "working hard" near the end
        View.ProgressSlider.DOValue(0.9f, estimatedTime).SetEase(Ease.OutCubic);
    }

    public override void OnExit()
    {
        // Safety: ensure no async logic tries to touch destroyed UI
        _isRealWorkDone = true;
        DOTween.Kill(View.ProgressSlider);
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