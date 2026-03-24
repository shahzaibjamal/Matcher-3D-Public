using System;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    private const string CHANNEL_ID = "toy_reminders";
    private bool _isInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupChannel();
    }

    private void SetupChannel()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "Toy Box Alerts",
            Importance = Importance.Default,
            Description = "Reminders for toy collection and level progress",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        _isInitialized = true;
#endif
    }

    public void ScheduleAllNotifications(int currentLevel)
    {
#if UNITY_ANDROID
        if (!_isInitialized) return;

        // Clear existing to avoid "Notification Spam"
        AndroidNotificationCenter.CancelAllNotifications();

        // Safety check for Metadata
        if (DataManager.Instance == null || DataManager.Instance.Metadata == null) return;

        bool isDebug = DataManager.Instance.Metadata.Settings.IsDebug;

        // 1. Level Progress Milestone
        ScheduleMilestoneNotification(currentLevel, isDebug);

        // 2. Generic Fun Toy Reminder
        ScheduleToyReminder(isDebug);
#endif
    }

#if UNITY_ANDROID
    private void ScheduleMilestoneNotification(int currentLevel, bool isDebug)
    {
        var allLevels = DataManager.Instance.Metadata.Levels;
        int maxLevelCount = (allLevels != null) ? allLevels.Count : 0;

        if (maxLevelCount == 0) return;

        // if (currentLevel >= maxLevelCount)
        // {
        //     SendCompletionNotification(isDebug);
        //     return;
        // }

        int nextGoal = ((currentLevel / 10) + 1) * 10;
        if (nextGoal > maxLevelCount) nextGoal = maxLevelCount;

        int remaining = nextGoal - currentLevel;

        var notification = new AndroidNotification
        {
            Title = "Almost There! 🏆",
            Text = $"Only {remaining} more levels until you reach Level {nextGoal}!",

            // --- DEBUG LOGIC ---
            // If debug is on, fire in 120 seconds. Otherwise, fire in 24 hours.
            FireTime = isDebug ? DateTime.Now.AddSeconds(120) : DateTime.Now.AddHours(24),
            SmallIcon = "icon_0"
        };

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);

        if (isDebug) Debug.Log($"[Notification] Milestone scheduled for {(isDebug ? "15 seconds" : "24 hours")}");
    }

    private void ScheduleToyReminder(bool isDebug)
    {
        string[] kidFriendlyMessages = {
            "The toys miss you! Come back and play? 🧸",
            "A new toy adventure is waiting for you! ✨",
            "Don't leave the Toy Box closed for too long! 📦"
        };

        var notification = new AndroidNotification();
        notification.Title = "Hey Friend! 👋";
        notification.Text = kidFriendlyMessages[UnityEngine.Random.Range(0, kidFriendlyMessages.Length)];

        // --- DEBUG LOGIC ---
        // If debug is on, fire in 30 seconds. Otherwise, fire in 2 days.
        notification.FireTime = isDebug ? DateTime.Now.AddSeconds(240) : DateTime.Now.AddDays(2);
        notification.SmallIcon = "icon_0";

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
    }

    // private void SendCompletionNotification(bool isDebug)
    // {
    //     var notification = new AndroidNotification();
    //     notification.Title = "Master Toy Collector! 🌟";
    //     notification.Text = "You've beaten every level! Come back to beat your high scores!";
    //     notification.FireTime = isDebug ? DateTime.Now.AddSeconds(60) : DateTime.Now.AddDays(3);
    //     notification.SmallIcon = "icon_0";

    //     AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
    // }
#endif
}