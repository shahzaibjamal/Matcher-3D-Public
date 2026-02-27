using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scheduler : MonoBehaviour
{
    private static Scheduler _instance;
    public static Scheduler Instance => _instance;

    private readonly List<Action<float>> _updateCallbacks = new List<Action<float>>();
    private readonly List<Action> _guiCallbacks = new List<Action>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        for (int i = 0; i < _updateCallbacks.Count; i++)
        {
            _updateCallbacks[i]?.Invoke(Time.deltaTime);
        }
    }

    private void OnGUI()
    {
        for (int i = 0; i < _guiCallbacks.Count; i++)
        {
            _guiCallbacks[i]?.Invoke();
        }
    }

    /// <summary>
    /// Subscribe an update callback.
    /// </summary>
    public void SubscribeUpdate(Action<float> callback)
    {
        if (callback != null && !_updateCallbacks.Contains(callback))
            _updateCallbacks.Add(callback);
    }

    /// <summary>
    /// Unsubscribe an update callback.
    /// </summary>
    public void UnsubscribeUpdate(Action<float> callback)
    {
        if (callback != null)
            _updateCallbacks.Remove(callback);
    }

    /// <summary>
    /// Subscribe a GUI callback.
    /// </summary>
    public void SubscribeGUI(Action callback)
    {
        if (callback != null && !_guiCallbacks.Contains(callback))
            _guiCallbacks.Add(callback);
    }

    /// <summary>
    /// Unsubscribe a GUI callback.
    /// </summary>
    public void UnsubscribeGUI(Action callback)
    {
        if (callback != null)
            _guiCallbacks.Remove(callback);
    }

    /// <summary>
    /// Run a coroutine globally.
    /// </summary>
    public Coroutine RunCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    /// <summary>
    /// Stop a coroutine globally.
    /// </summary>
    public void StopGlobalCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
    }
    /// <summary>
    /// Helper to run a delayed action and return the coroutine handle.
    /// </summary>
    public Coroutine ExecuteAfterDelay(float delay, Action action)
    {
        return StartCoroutine(DelayedActionRoutine(delay, action));
    }

    private IEnumerator DelayedActionRoutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    public Coroutine ExecuteAfterDelay<T>(float delay, Action<T> action, T arg)
    {
        return StartCoroutine(DelayedActionRoutine(delay, () => action(arg)));
    }

    public Coroutine ExecuteAfterDelay<T1, T2>(float delay, Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        return StartCoroutine(DelayedActionRoutine(delay, () => action(arg1, arg2)));
    }

}
