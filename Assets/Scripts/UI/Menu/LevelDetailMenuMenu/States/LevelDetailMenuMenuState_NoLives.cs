using UnityEngine;
using System;
using TS.LocalizationSystem;

public class LevelDetailMenuMenuBaseState_NoLives : LevelDetailMenuMenuBaseState
{
    private bool _isTicking;

    public LevelDetailMenuMenuBaseState_NoLives(LevelDetailMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        View.NoLivesPanel.SetActive(true);
        View.OkButton.gameObject.SetActive(true);
        View.ShopButton.gameObject.SetActive(true);
        View.ShopButton.onClick.AddListener(OnShopClicked);
        View.OkButton.onClick.AddListener(OnOkButtonClicked);
        View.NoLivesText.text = LocaleManager.Localize(LocalizationKeys.no_lives_anymore);
        View.OkButtonText.text = LocaleManager.Localize(LocalizationKeys.ok);
        View.shopButtonText.text = LocaleManager.Localize(LocalizationKeys.store);

        _isTicking = true;
        // If your Controller has an Update loop that calls State.Update(), use that.
        // Otherwise, we can trigger the first refresh here.
        RefreshTimerUI();
        Scheduler.Instance.SubscribeUpdate(UpdateTimer);
    }

    public void UpdateTimer(float dt) // Assuming your BaseState has a virtual Update called by Controller
    {
        if (!_isTicking) return;
        RefreshTimerUI();
    }

    private void RefreshTimerUI()
    {
        var save = GameManager.Instance.SaveData;

        // 1. Double check: Did we get a life while this menu was open?
        if (save.CurrentLives > 0)
        {
            _isTicking = false;
            Controller.SetState(new LevelDetailMenuMenuBaseState_Main(Controller));
            return;
        }

        // 2. Calculate time remaining
        if (DateTime.TryParse(save.LastLifeLostTime, out DateTime lastLost))
        {
            DateTime nextLifeTime = lastLost.AddSeconds(GameSaveData.SECONDS_TO_RECOVER_LIFE);
            TimeSpan diff = nextLifeTime - DateTime.Now;

            if (diff.TotalSeconds <= 0)
            {
                // Timer finished! The GameSaveData.CurrentLives 'get' property 
                // will automatically run UpdateLivesLogic() when we check it.
                if (save.CurrentLives > 0)
                {
                    _isTicking = false;
                    Controller.SetState(new LevelDetailMenuMenuBaseState_Main(Controller));
                }
            }
            else
            {
                // 3. Format string (MM:SS)
                View.TimerText.text = string.Format("{0:D2}:{1:D2}", diff.Minutes, diff.Seconds);
            }
        }
        else
        {
            View.TimerText.text = "00:00";
        }
    }

    public override void Exit()
    {
        base.Exit();
        _isTicking = false;
        View.NoLivesPanel.SetActive(false);
        View.ShopButton.onClick.RemoveListener(OnShopClicked);
        View.OkButton.onClick.RemoveListener(OnOkButtonClicked);
        Scheduler.Instance.UnsubscribeUpdate(UpdateTimer);
    }

    private void OnOkButtonClicked() => Controller.HandleBackInput();
    private void OnShopClicked() => MenuManager.Instance.OpenMenu<StoreMenuView, StoreMenuController, StoreMenuData>(Menus.Type.Store);
}