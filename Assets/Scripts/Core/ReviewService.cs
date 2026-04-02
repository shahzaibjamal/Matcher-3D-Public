using System;
using System.Collections;
using Google.Play.Review;
using UnityEngine;

public class ReviewService : MonoBehaviour
{
    public static ReviewService Instance { get; private set; }

    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;
    private bool _isRequesting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Evaluates if the player should see the review popup based on Level and Time.
    /// </summary>
    public bool ShouldShowReview()
    {
        if (_isRequesting) return false;

        var save = GameManager.Instance.SaveData;
        var settings = DataManager.Instance.Metadata.Settings;

        if (save.IsAppReviewed) return false;

        // 1. Get current progress
        int currentLevel = LevelManager.Instance.GetCurrentProgressLevel().Number;

        // 2. The Global vs. Local Gate
        // This ensures we never ask before the 'ReviewLevel' metadata floor,
        // but also respects the 'ReminderLevel' (which increases every time they say "Later").
        int effectiveGate = Mathf.Max(settings.ReviewLevel, save.AppReviewReminderLevel);

        if (currentLevel < effectiveGate) return false;

        // 3. The 7-Day Cooldown (Hard-coded safety)
        if (!string.IsNullOrEmpty(save.LastReviewRequestDate))
        {
            if (DateTime.TryParse(save.LastReviewRequestDate, out DateTime lastAsked))
            {
                if ((DateTime.UtcNow - lastAsked).TotalDays < 7) return false;
            }
        }

        return true;
    }    /// <summary>
         /// Triggers the Google Play Review flow. Call this when the user clicks 'Yes'.
         /// </summary>
    public void LaunchReviewFlow(Action onComplete = null)
    {
        StartCoroutine(ReviewRoutine(onComplete));
    }

    private IEnumerator ReviewRoutine(Action onComplete)
    {
        _isRequesting = true;
        var save = GameManager.Instance.SaveData;

        // Push metadata forward immediately to prevent duplicate prompts 
        // in the same session if the Google UI is slow to load.
        UpdateNextReminderMetadata();

        if (_reviewManager == null)
            _reviewManager = new ReviewManager();

        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[ReviewService] Request Error: {requestFlowOperation.Error}");
            _isRequesting = false;
            onComplete?.Invoke();
            yield break;
        }

        _playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;

        // Mark as reviewed so we never ask again. 
        // Google Play API doesn't tell us if they actually typed a review, 
        // only that the window finished.
        save.IsAppReviewed = true;
        GameManager.Instance.SaveGame();

        _playReviewInfo = null;
        _isRequesting = false;
        onComplete?.Invoke();
        Debug.Log("[ReviewService] Flow completed successfully.");
    }

    /// <summary>
    /// Moves the next reminder goal 10 levels ahead and resets the timestamp.
    /// Call this if the user clicks 'No' or when the flow starts.
    /// </summary>
    public void UpdateNextReminderMetadata()
    {
        var save = GameManager.Instance.SaveData;
        int currentLevel = LevelManager.Instance.GetCurrentProgressLevel().Number;
        int interval = DataManager.Instance.Metadata.Settings.ReviewReminderLevelsInterval;

        save.AppReviewReminderLevel = currentLevel + interval;
        save.LastReviewRequestDate = DateTime.UtcNow.ToString();
        GameManager.Instance.SaveGame();
    }

    /// <summary>
    /// Standard fallback to the store page if needed.
    /// </summary>
    public void OpenStorePageDirectly()
    {
        Application.OpenURL($"market://details?id={Application.identifier}");
    }
}