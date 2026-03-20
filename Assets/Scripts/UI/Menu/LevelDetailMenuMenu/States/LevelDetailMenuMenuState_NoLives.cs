using System;
using DG.Tweening;
using TS.LocalizationSystem;
using UnityEngine;

public class LevelDetailMenuMenuBaseState_NoLives : LevelDetailMenuMenuBaseState
{
    private bool _isTicking;
    private Sequence _heartSequence;
    private Sequence _textFlashSequence;
    private Color _originalTextColor;
    private int _lastDisplayedSecond = -1;

    public LevelDetailMenuMenuBaseState_NoLives(LevelDetailMenuController controller) : base(controller) { }

    public override void Enter()
    {
        base.Enter();
        _originalTextColor = View.TimerText.color;

        View.NoLivesPanel.SetActive(true);
        View.OkButton.gameObject.SetActive(true);
        View.ShopButton.gameObject.SetActive(true);

        View.ShopButton.onClick.AddListener(OnShopClicked);
        View.OkButton.onClick.AddListener(OnOkButtonClicked);

        View.NoLivesText.text = LocaleManager.Localize(LocalizationKeys.no_lives_anymore);
        View.OkButtonText.text = LocaleManager.Localize(LocalizationKeys.ok);
        View.shopButtonText.text = LocaleManager.Localize(LocalizationKeys.store);

        _isTicking = true;
        RefreshTimerUI();
        Scheduler.Instance.SubscribeUpdate(UpdateTimer);

        PlayHeartErrorAnimation();
    }

    public void PlayHeartErrorAnimation()
    {
        // Initial "Ouch" feedback
        View.HeartIcon.transform.DOKill();
        View.HeartIcon.transform.DOShakePosition(0.4f, 10f, 20);

        GameManager.Instance.Vibrate(Haptics.HapticTypes.Warning);
        SoundController.Instance.PlaySoundEffect("error");
    }

    private void RefreshTimerUI()
    {
        var save = GameManager.Instance.SaveData;
        int secondsToRecover = DataManager.Instance.Metadata.Settings.SecondsToRecover;

        if (save.CurrentLives > 0)
        {
            _isTicking = false;
            PlayHeartRefillJuice();
            return;
        }

        if (!string.IsNullOrEmpty(save.LastLifeLostTime) && DateTime.TryParse(save.LastLifeLostTime, out DateTime lastLost))
        {
            DateTime nextLifeTime = lastLost.AddSeconds(secondsToRecover);
            TimeSpan diff = nextLifeTime - DateTime.Now;

            double displaySeconds = diff.TotalSeconds + 1;

            if (displaySeconds >= 1)
            {
                TimeSpan displayTime = TimeSpan.FromSeconds(displaySeconds);
                View.TimerText.text = string.Format("{0:D2}:{1:D2}", (int)displayTime.TotalMinutes, displayTime.Seconds);

                HandleTimerAlert(displaySeconds);

                // --- SMOOTH FILL SYNC ---
                // This ensures the fill bar moves every frame, not just every second
                float fillProgress = (float)(diff.TotalSeconds / secondsToRecover);
                View.HeartFillIcon.fillAmount = 1 - fillProgress;

                // --- SYNCED HEARTBEAT ---
                int currentSec = displayTime.Seconds;
                if (currentSec != _lastDisplayedSecond)
                {
                    _lastDisplayedSecond = currentSec;

                    // Trigger the "Dub-Dub" punch exactly on the second tick
                    if (_lastDisplayedSecond % 2 == 0)
                        TriggerSyncedHeartbeat();

                    // Text Jump
                    View.TimerText.transform.DOKill(true);
                    View.TimerText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.2f, 5, 0.5f);
                }
            }
        }
    }

    private void TriggerSyncedHeartbeat()
    {
        // Kill previous punch to avoid overlap if the frame rate stutters
        View.HeartIcon.transform.DOKill(true);

        Sequence syncBeat = DOTween.Sequence();

        // The "Dub" (Immediate on the second tick)
        syncBeat.Append(View.HeartIcon.transform.DOScale(1.15f, 0.1f).SetEase(Ease.OutQuad));
        syncBeat.Append(View.HeartIcon.transform.DOScale(1.0f, 0.05f).SetEase(Ease.InQuad));

        syncBeat.AppendInterval(0.05f);

        // The "DUB" (The follow-up punch)
        syncBeat.Append(View.HeartIcon.transform.DOScale(1.25f, 0.12f).SetEase(Ease.OutQuad));
        syncBeat.Append(View.HeartIcon.transform.DOScale(1.0f, 0.15f).SetEase(Ease.InQuad));
    }
    private void PlayHeartRefillJuice()
    {
        // Clean up existing sequences
        _heartSequence?.Kill();
        _textFlashSequence?.Kill();

        // Final "Pop" to show life is back
        View.HeartIcon.transform.localScale = Vector3.one;
        View.TimerText.text = string.Empty;
        View.TimerText.color = Color.green;

        View.HeartIcon.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 10, 1f)
            .OnComplete(() =>
            {
                Controller.SetState(new LevelDetailMenuMenuBaseState_Main(Controller));
            });

        SoundController.Instance.PlaySoundEffect("item_complete");
    }

    private void HandleTimerAlert(double secondsRemaining)
    {
        if (secondsRemaining < 20 && _textFlashSequence == null)
        {
            // Start the red-white flash sequence
            _textFlashSequence = DOTween.Sequence();
            _textFlashSequence.Append(View.TimerText.DOColor(Color.red, 0.25f));
            _textFlashSequence.Append(View.TimerText.DOColor(Color.white, 0.25f));
            _textFlashSequence.SetLoops(-1);
        }
        else if (secondsRemaining >= 20 && _textFlashSequence != null)
        {
            // Reset if we somehow go back above 20 (e.g., debug or edge case)
            _textFlashSequence.Kill();
            _textFlashSequence = null;
            View.TimerText.color = _originalTextColor;
        }
    }

    public override void Exit()
    {
        base.Exit();
        _isTicking = false;

        // --- CLEANUP: Kill sequences ---
        _heartSequence?.Kill();
        _textFlashSequence?.Kill();
        View.TimerText.color = _originalTextColor;
        View.HeartIcon.transform.localScale = Vector3.one;

        View.NoLivesPanel.SetActive(false);
        View.ShopButton.onClick.RemoveListener(OnShopClicked);
        View.OkButton.onClick.RemoveListener(OnOkButtonClicked);
        Scheduler.Instance.UnsubscribeUpdate(UpdateTimer);
    }
    public void UpdateTimer(float dt)
    {
        if (!_isTicking) return;
        RefreshTimerUI();
    }

    private void OnOkButtonClicked() => Controller.HandleBackInput();
    private void OnShopClicked() => MenuManager.Instance.OpenMenu<StoreMenuView, StoreMenuController, StoreMenuData>(Menus.Type.Store);
}