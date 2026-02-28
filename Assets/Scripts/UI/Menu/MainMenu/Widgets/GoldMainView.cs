using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class GoldMainView : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text GoldText;
    public Image GoldIcon;
    public RectTransform Container; // The main parent of this HUD element

    private int _displayedAmount = 0;
    private Tween _countTween;

    public void UpdateAmount(int initialAmount)
    {
        _displayedAmount = initialAmount;
        GoldText.text = initialAmount.ToString("N0");
    }

    /// <summary>
    /// Animates the HUD total with a counting effect and a scale "pulse".
    /// </summary>
    public void PlayCollectAnimation(int targetAmount, float duration = 0.5f)
    {
        // 1. Scale Pulse (Juice)
        // Punch the container slightly when gold hits it
        if (Container != null)
            Container.DOPunchScale(new Vector3(0.15f, 0.15f, 0.15f), 0.3f, 10, 1f);

        // Punch the icon specifically for more visual feedback
        GoldIcon.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f);

        // 2. Numerical Counting Animation
        _countTween?.Kill();
        _countTween = DOTween.To(() => _displayedAmount, x =>
        {
            _displayedAmount = x;
            GoldText.text = _displayedAmount.ToString("N0");
        }, targetAmount, duration).SetEase(Ease.OutQuad);
    }

    // Returns the world position of the icon for the fly animation target
    public Vector3 GetTargetPosition() => GoldIcon.transform.position;
}