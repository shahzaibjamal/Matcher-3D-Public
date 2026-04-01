using TS.LocalizationSystem;

public class MatchLoseMenuController : MenuController<MatchLoseMenuView, MatchLoseMenuData>
{
    public override void OnEnter()
    {
        SetState(new MatchLoseMenuBaseState(this));
        var levelData = LevelManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID);
        AnalyticsManager.Instance.LogLevelFail(levelData.Number);

        View.ClearButton.onClick.AddListener(OnClearButtonClick);
        View.QuitButton.onClick.AddListener(OnQuitButtonClick);
    }
    public override void OnExit()
    {
        View.ClearButton.onClick.RemoveListener(OnClearButtonClick);
        View.QuitButton.onClick.RemoveListener(OnQuitButtonClick);
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
    }
    private void OnClearButtonClick()
    {
        AnalyticsManager.Instance.LogAdEvent(GameAnalyticsSDK.GAAdAction.Clicked, GameAnalyticsSDK.GAAdType.RewardedVideo, "Admobs", "Clear");
        AdManager.Instance.ShowRewarded(OnRewardAdSuccess, OnRewardAdFailed);
    }

    private void OnRewardAdFailed()
    {
        AnalyticsManager.Instance.LogAdEvent(GameAnalyticsSDK.GAAdAction.FailedShow, GameAnalyticsSDK.GAAdType.RewardedVideo, "Admobs", "Clear");
        MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(Menus.Type.GenericPopup, new GenericPopupMenuData
        (
            LocalizationKeys.no_ads,
            LocalizationKeys.no_ads_message,
            LocalizationKeys.ok
        ));
    }

    private void OnRewardAdSuccess()
    {
        MenuManager.Instance.GoBack();
        GameEvents.OnCleanSweepTrayEvent?.Invoke();
        AnalyticsManager.Instance.LogAdEvent(GameAnalyticsSDK.GAAdAction.RewardReceived, GameAnalyticsSDK.GAAdType.RewardedVideo, "Admobs", "Clear");
    }

    private void OnQuitButtonClick()
    {
        MenuManager.Instance.OpenMenu<LoseLifeMenuView, LoseLifeMenuController, LoseLifeMenuData>(Menus.Type.LoseLife, new LoseLifeMenuData
        {
            isRestart = false
        });
    }
}