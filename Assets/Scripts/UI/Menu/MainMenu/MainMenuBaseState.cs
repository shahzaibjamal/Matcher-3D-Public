using DG.Tweening;
using Unity.VisualScripting;

public class MainMenuBaseState : MenuBaseState<MainMenuController, MainMenuView, MainMenuData>
{
    public MainMenuBaseState(MainMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        // PlayIntroFadeOutAniamtion();
    }


    public override void Exit()
    {
    }

    private void PlayIntroFadeOutAniamtion()
    {
        View.FadeOutImage.color = new UnityEngine.Color(0, 0, 0, 0.4f);
        View.FadeOutImage.DOFade(0f, 1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Optional: Disable the object so it doesn't block Raycasts
                    View.FadeOutImage.gameObject.SetActive(false);
                });
    }
}