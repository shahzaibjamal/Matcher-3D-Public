using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image _backgroundImage;

    public ItemData CurrentItem { get; private set; }
    public bool IsImageEnabled => icon.enabled;
    public Transform IconTransform => icon.transform;

    private int _index = -1;

    public void SetIndex(int index) => _index = index;
    public int Index => _index;

    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        if (itemData != null)
        {
            icon.sprite = itemData.UISprite;
        }
        // Keep it hidden; the TrayView will call RevealIcon after the animation lands
        icon.enabled = false;
    }

    public void RevealIcon(bool impact = false)
    {
        if (CurrentItem == null) return;

        icon.transform.DOKill(); // Stop any pending match-scales
        icon.enabled = true;
        icon.sprite = CurrentItem.UISprite;

        // Ensure we are at full scale and visible
        icon.transform.localScale = Vector3.one;
        icon.canvasRenderer.SetAlpha(1f);

        // Add the landing juice
        icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

        if (impact)
        {
            RectTransform rect = _backgroundImage.rectTransform;
            Sequence impactSeq = DOTween.Sequence();

            Vector3 originalPos = rect.localPosition;
            // 1. The Slam: Move down quickly with a heavy ease
            impactSeq.Append(transform.DOLocalMoveY(originalPos.y - 20f, 0.1f)
                .SetEase(Ease.InQuad));
            // 2. The Rebound: Use a Punch or Shake to simulate the weight settling
            // This moves it back to originalPos while vibrating
            impactSeq.Append(transform.DOPunchPosition(new Vector3(0, 20f * 0.8f, 0), 0.4f, 5, 0.5f));

            // Ensure it ends exactly where it started
            impactSeq.OnComplete(() => rect.localPosition = originalPos);
        }
    }
    public void Clear()
    {
        CurrentItem = null;
        icon.enabled = false;
        icon.transform.DOKill();
    }
    private int debugFontSize = 38; // adjustable font size

    private void OnGUI()
    {
        if (CurrentItem == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = debugFontSize,
            normal = { textColor = Color.red }
        };

        string truncatedUniqueId = CurrentItem.UniqueId.Length > 7
            ? CurrentItem.UniqueId.Substring(0, 7)
            : CurrentItem.UniqueId;

        string text = $"{truncatedUniqueId} \n {CurrentItem.UID}";

        float startY = Screen.height - debugFontSize * 3; // anchor near bottom
        float xSpacing = 220f;                            // horizontal spacing
        float x = 10f + (_index * xSpacing);

        // Use font size for rect height
        GUI.Label(new Rect(x, startY, 400, debugFontSize * 3), text, style);
    }


}