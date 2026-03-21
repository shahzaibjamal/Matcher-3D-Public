using System;

public class LoadingMenuData : MenuData
{
    public float Delay = -1; // Keep for fallback
    public Action OnLoadingComplete;

    // NEW: The actual work to wait for
    public System.Func<System.Threading.Tasks.Task> LoadingTask;
}