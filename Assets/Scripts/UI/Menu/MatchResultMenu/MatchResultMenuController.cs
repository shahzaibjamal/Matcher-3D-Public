using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MatchResultMenuController : MenuController<MatchResultMenuView, MatchResultMenuData>
{
    public override void OnEnter()
    {
        if (Data.IsWin)
        {
            SetState(new MatchResultMenuBaseState_Win(this));
        }
        else
        {
            SetState(new MatchResultMenuBaseState_Lose(this));
        }
        View.ContinueButton.onClick.AddListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.AddListener(OnGoldMultiplierButtonClicked);
    }

    public override void OnExit()
    {
        View.Root.alpha = 0.0f;
        View.GodRays.SetActive(false);
        View.ContinueButton.onClick.RemoveListener(OnContinueButtonClicked);
        View.GoldMulitplierButton.onClick.RemoveListener(OnGoldMultiplierButtonClicked);

        base.OnExit();
    }


    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    void OnContinueButtonClicked()
    {
        MenuManager.Instance.OpenMenu<MainMenuView, MainMenuController, MainMenuData>(Menus.Type.Settings);
    }

    void OnGoldMultiplierButtonClicked()
    {
        SetState(new MatchResultMenuBaseState_Win(this));
    }
}