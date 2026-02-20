using UnityEngine;
using DG.Tweening;

public abstract class MenuView : MonoBehaviour, IMenuView
{
    // Implementation of the interface property
    public Menus.MenuDisplayMode DisplayMode { get; set; }

    public virtual void SetVisible(bool visible) => gameObject.SetActive(visible);

    public virtual void Destroy() => Destroy(gameObject);

    // Logic for animations when the menu opens
    public virtual void OnEnter()
    {
        // Example: Different animations based on DisplayMode
        if (DisplayMode == Menus.MenuDisplayMode.Popup)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            // Default Fade or Slide for screens
        }
    }

    // Logic for animations when the menu closes
    public virtual void OnExit()
    {
        if (DisplayMode == Menus.MenuDisplayMode.Popup)
        {
            transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
        }
    }
}