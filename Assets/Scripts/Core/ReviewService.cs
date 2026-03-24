using Google.Play.Review;
using System.Collections;
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
    /// Starts the Google Play In-App Review flow.
    /// </summary>
    public void LaunchReviewFlow()
    {
        if (_isRequesting) return;

        // Safety check: Don't run if already reviewed
        if (GameManager.Instance.SaveData.IsAppReviewed) return;

        StartCoroutine(ReviewRoutine());
    }

    private IEnumerator ReviewRoutine()
    {
        _isRequesting = true;

        if (_reviewManager == null)
            _reviewManager = new ReviewManager();

        // 1. Request Review Info
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[ReviewService] Request Error: {requestFlowOperation.Error}");
            _isRequesting = false;
            yield break;
        }

        _playReviewInfo = requestFlowOperation.GetResult();

        // 2. Launch Review Flow
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;

        // Cleanup
        _playReviewInfo = null;
        _isRequesting = false;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[ReviewService] Launch Error: {launchFlowOperation.Error}");
            yield break;
        }

        // 3. Mark as reviewed and save
        GameManager.Instance.SaveData.IsAppReviewed = true;
        GameManager.Instance.SaveGame();

        Debug.Log("[ReviewService] Flow finished successfully.");
    }

    /// <summary>
    /// Deep-links directly to the Play Store page as a fallback.
    /// </summary>
    public void OpenStorePageDirectly()
    {
        string appId = Application.identifier;
        Application.OpenURL($"market://details?id={appId}");
    }
}