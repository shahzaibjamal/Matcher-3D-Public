
public class LevelDetailMenuController : MenuController<LevelDetailMenuView, LevelDetailMenuData>
{
    public override void OnEnter()
    {
        if (GameManager.Instance.SaveData.CurrentLives == 0)
        {
            SetState(new LevelDetailMenuMenuBaseState_NoLives(this));
        }
        else
        {
            SetState(new LevelDetailMenuMenuBaseState_Main(this));
        }
        UIAnimations.ToonIn(View.canvasGroup, View.Root, null);
    }

    public override void OnExit()
    {
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
        base.HandleBackInput();
    }
}