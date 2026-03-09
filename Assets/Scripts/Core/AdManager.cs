using UnityEngine;
using System.Collections.Generic;
using System;

#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    // --- Threading Helper ---
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

    [Header("Current Progress")]
    [SerializeField] private int currentLevel = 1;

    [Header("Unlock Thresholds")]
    public int bannerUnlockLevel = 1;
    public int interstitialUnlockLevel = 1;
    public int rewardedUnlockLevel = 1;

    [Header("Interstitial Settings")]
    [Range(0, 1)] public float interstitialChance = 0.5f;

    [Header("Production IDs")]
    public string realBannerId = "ca-app-pub-3489872370282662/6280758579";
    public string realInterstitialId = "ca-app-pub-3489872370282662/8112026638";
    public string realRewardedId = "ca-app-pub-3489872370282662/2696736786";

    private const string TEST_BANNER = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_REWARDED = "ca-app-pub-3940256099942544/5224354917";

    // Cached IDs to avoid thread-safety issues with Debug.isDebugBuild
    private string _activeBannerId;
    private string _activeInterstitialId;
    private string _activeRewardedId;

#if USE_ADMOB
    private BannerView _bannerView;
    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;
#endif

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Determine IDs once on the Main Thread
        _activeBannerId = Debug.isDebugBuild ? TEST_BANNER : realBannerId;
        _activeInterstitialId = Debug.isDebugBuild ? TEST_INTERSTITIAL : realInterstitialId;
        _activeRewardedId = Debug.isDebugBuild ? TEST_REWARDED : realRewardedId;
    }

    private void Update()
    {
        // Execute queued actions (like reward callbacks) on the Main Thread
        lock (_mainThreadQueue)
        {
            while (_mainThreadQueue.Count > 0)
            {
                _mainThreadQueue.Dequeue().Invoke();
            }
        }
    }

    private void Enqueue(Action action)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(action);
        }
    }

    private void Start()
    {
#if USE_ADMOB
        // Configure test devices if any are provided in the inspector
        RequestConfiguration requestConfiguration = new RequestConfiguration { TestDeviceIds = new List<string>() }; // Add device IDs here if needed
        MobileAds.SetRequestConfiguration(requestConfiguration);

        MobileAds.Initialize(initStatus =>
        {
            // Initializing is often on a background thread, so we move to Main Thread to load ads
            Enqueue(() =>
            {
                Debug.Log("[AdManager] Initialized.");
                RefreshAdLoads();
            });
        });
#endif
    }

    public void UpdateLevelProgress(int newLevel)
    {
        currentLevel = newLevel;
        HandleBannerVisibility();
        RefreshAdLoads();
    }

    private void HandleBannerVisibility()
    {
        if (currentLevel < bannerUnlockLevel) return;

#if USE_ADMOB
        if (_bannerView == null)
        {
            _bannerView = new BannerView(_activeBannerId, AdSize.Banner, AdPosition.Bottom);
            _bannerView.LoadAd(new AdRequest());
            Debug.Log("[AdManager] Banner Requested.");
        }
#endif
    }

    private void RefreshAdLoads()
    {
#if USE_ADMOB
        if (currentLevel >= interstitialUnlockLevel && _interstitialAd == null) LoadInterstitialAd();
        if (currentLevel >= rewardedUnlockLevel && _rewardedAd == null) LoadRewardedAd();
#endif
    }

    // --- 1. Interstitial Logic ---
    public void TryShowInterstitial()
    {
        if (currentLevel < interstitialUnlockLevel) return;
        if (UnityEngine.Random.value > interstitialChance) return;

#if USE_ADMOB
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
#else
        Debug.Log("[Mock] Interstitial Shown.");
#endif
    }

    // --- 2. Rewarded Logic ---
    public void ShowRewarded(Action onFinished, Action onFailed = null)
    {
        if (currentLevel < rewardedUnlockLevel)
        {
            onFailed?.Invoke();
            return;
        }

#if USE_ADMOB
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // CRITICAL: This runs on a background thread. Enqueue to Main Thread.
                Enqueue(() =>
                {
                    Debug.Log("[AdManager] Reward Earned!");
                    onFinished?.Invoke();
                });
            });
        }
        else
        {
            onFailed?.Invoke();
            LoadRewardedAd();
        }
#else
        onFinished?.Invoke();
#endif
    }

    #region Internals (Loading)
#if USE_ADMOB
    private void LoadInterstitialAd()
    {
        if (_interstitialAd != null) _interstitialAd.Destroy();

        InterstitialAd.Load(_activeInterstitialId, new AdRequest(), (ad, err) =>
        {
            Enqueue(() =>
            {
                if (err != null) return;
                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += LoadInterstitialAd;
            });
        });
    }

    private void LoadRewardedAd()
    {
        if (_rewardedAd != null) _rewardedAd.Destroy();

        RewardedAd.Load(_activeRewardedId, new AdRequest(), (ad, err) =>
        {
            Enqueue(() =>
            {
                if (err != null)
                {
                    Debug.LogWarning("[AdManager] Rewarded Load Failed: " + err.GetMessage());
                    return;
                }
                _rewardedAd = ad;
                _rewardedAd.OnAdFullScreenContentClosed += LoadRewardedAd;
            });
        });
    }
#endif
    #endregion
}