using UnityEngine;
using DG.Tweening;
using System;

public abstract class MenuView : MonoBehaviour, IMenuView
{

    public CanvasGroup canvasGroup; // Add this in inspector
    // Implementation of the interface property
    public Menus.MenuDisplayMode DisplayMode { get; set; }

    public virtual void SetVisible(bool visible) => gameObject.SetActive(visible);

    public virtual void Destroy() => Destroy(gameObject);

    protected float fadeDuration = 0.3f;

    // Logic for animations when the menu opens
    public virtual void OnEnter()
    {

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        // Example: Different animations based on DisplayMode
        if (DisplayMode == Menus.MenuDisplayMode.Popup)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
        // Fade in for all menu types
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1f, fadeDuration);
        }
    }

    // Logic for animations when the menu closes
    public virtual void OnExit(Action onComplete)
    {
        if (DisplayMode == Menus.MenuDisplayMode.Popup || DisplayMode == Menus.MenuDisplayMode.Overlay)
        {
            // Chain Scale and Fade, then trigger callback
            transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            // Fallback if no CanvasGroup is found
            onComplete?.Invoke();
        }
    }
}