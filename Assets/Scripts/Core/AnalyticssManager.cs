using UnityEngine;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameAnalytics.Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- PROGRESSION EVENTS ---
    // Status: Start, Complete, Fail
    public void LogLevelStart(int levelNumber)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "Level_" + levelNumber.ToString("D2"));
    }

    public void LogLevelComplete(int levelNumber, int score = 0)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "Level_" + levelNumber.ToString("D2"), score);
    }

    public void LogLevelFail(int levelNumber)
    {
        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "Level_" + levelNumber.ToString("D2"));
    }

    // --- RESOURCE EVENTS ---
    // Flow: Source (Gained) or Sink (Spent)
    public void LogResourceEvent(GAResourceFlowType flow, string currency, float amount, string itemType, string itemId)
    {
        // currency: "Gold", itemType: "PowerUp", itemId: "Hint"
        GameAnalytics.NewResourceEvent(flow, currency, amount, itemType, itemId);
    }

    // --- DESIGN EVENTS ---
    // For custom tracking like "Clicked Settings" or "Difficulty Slider Value"
    public void LogDesignEvent(string eventName, float value = 0)
    {
        GameAnalytics.NewDesignEvent(eventName, value);
    }

    public void LogAdEvent(GAAdAction adAction, GAAdType adType, string adSdkName, string adPlacement)
    {
        GameAnalytics.NewAdEvent(adAction, adType, adSdkName, adPlacement);
    }
}