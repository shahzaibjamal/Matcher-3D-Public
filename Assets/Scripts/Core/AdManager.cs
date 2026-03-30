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
    [SerializeField] private List<string> _testDevicesIds;

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

    [Header("Unlock Thresholds")]
    private int _bannerUnlockLevel = 1;
    private int _interstitialUnlockLevel = 1;
    private int _rewardedUnlockLevel = 1;

    private float _interstitialChance = 0.5f;

    private Action _onPendingLoadFailed;

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
    private void Start()
    {
#if USE_ADMOB
        // Configure test devices if any are provided in the inspector
        RequestConfiguration requestConfiguration = new RequestConfiguration { TestDeviceIds = _testDevicesIds }; // Add device IDs here if needed
        MobileAds.SetRequestConfiguration(requestConfiguration);
#endif
    }

    public void Initialize(int bannerUnlockLevel, int interstitialUnlockLevel, int rewardedUnlockLevel, float interstitialChance)
    {
        _bannerUnlockLevel = bannerUnlockLevel;
        _interstitialUnlockLevel = interstitialUnlockLevel;
        _rewardedUnlockLevel = rewardedUnlockLevel;
        _interstitialChance = interstitialChance;
#if USE_ADMOB
        MobileAds.Initialize(initStatus =>
        {
            // Initializing is often on a background thread, so we move to Main Thread to load ads
            Enqueue(() =>
            {
                Debug.Log("[AdManager] Initialized.");
                RefreshAdLoads();
            });
        });
        Debug.LogError("Device Id - " + SystemInfo.deviceUniqueIdentifier);
#endif
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

    public void UpdateLevelProgress(int newLevel)
    {
        currentLevel = newLevel;
        HandleBannerVisibility();
        RefreshAdLoads();
    }

    private void HandleBannerVisibility()
    {
        if (currentLevel < _bannerUnlockLevel) return;

#if USE_ADMOB
        if (_bannerView == null)
        {
            Debug.Log("[AdManager] Creating BannerView...");
            _bannerView = new BannerView(_activeBannerId, AdSize.Banner, AdPosition.Bottom);

            // Add these listeners to see the real error on your device
            _bannerView.OnBannerAdLoaded += () => Debug.Log("<color=green>[AdManager] Banner Loaded Successfully!</color>");

            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogError($"[AdManager] Banner Failed: {error.GetMessage()} | Code: {error.GetCode()}");
            };

            _bannerView.LoadAd(new AdRequest());
        }
#endif
    }
    private void RefreshAdLoads()
    {
#if USE_ADMOB
        if (currentLevel >= _interstitialUnlockLevel && _interstitialAd == null) LoadInterstitialAd();
        if (currentLevel >= _rewardedUnlockLevel && _rewardedAd == null) LoadRewardedAd();
#endif
    }

    // --- 1. Interstitial Logic ---
    public void TryShowInterstitial()
    {
        if (currentLevel < _interstitialUnlockLevel) return;
        if (UnityEngine.Random.value > _interstitialChance) return;

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
        if (currentLevel < _rewardedUnlockLevel)
        {
            onFailed?.Invoke();
            return;
        }

#if USE_ADMOB
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                Enqueue(() =>
                {
                    onFinished?.Invoke();
                });
            });
        }
        else
        {
            // Store the failure callback so LoadRewardedAd can find it
            _onPendingLoadFailed = onFailed;
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

                    // TRIGGER THE STORED CALLBACK HERE
                    _onPendingLoadFailed?.Invoke();
                    _onPendingLoadFailed = null; // Clear it so it doesn't fire twice
                    return;
                }

                _rewardedAd = ad;
                _rewardedAd.OnAdFullScreenContentClosed += LoadRewardedAd;

                // If we were waiting for this ad to show immediately:
                // Optional: You could auto-show here, but usually it's safer to 
                // let the user click again or show a "Ready" toast.
                _onPendingLoadFailed = null;
            });

        });
    }
}
#endif
    #endregion
