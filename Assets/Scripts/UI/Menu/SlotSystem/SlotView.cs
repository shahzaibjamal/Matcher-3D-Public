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

    private Vector3 _originalSlotPos; // Store the home position

    private void Awake()
    {
        // Capture the starting position on start
        _originalSlotPos = _backgroundImage.rectTransform.localPosition;
    }
    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        if (itemData != null)
        {
            AssetLoader.Instance.LoadIcon(itemData.IconName, (sprite) =>
            {
                icon.sprite = sprite;
            });

        }
        // Keep it hidden; the TrayView will call RevealIcon after the animation lands
        icon.enabled = false;
    }

    public void RevealIcon(bool impact = false)
    {
        if (CurrentItem == null) return;

        icon.transform.DOKill(); // Stop any pending match-scales
        icon.enabled = true;
        AssetLoader.Instance.LoadIcon(CurrentItem.IconName, (sprite) =>
        {
            icon.sprite = sprite;
        });

        // Ensure we are at full scale and visible
        icon.transform.localScale = Vector3.one;
        icon.canvasRenderer.SetAlpha(1f);

        // Add the landing juice
        icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

        if (impact)
        {
            RectTransform rect = _backgroundImage.rectTransform;
            Sequence impactSeq = DOTween.Sequence();

            // 1. The Slam: Move down quickly with a heavy ease
            impactSeq.Append(rect.DOLocalMoveY(_originalSlotPos.y - 10f, 0.1f)
                .SetEase(Ease.InQuad));
            // 2. The Rebound: Use a Punch or Shake to simulate the weight settling
            // This moves it back to originalPos while vibrating
            impactSeq.Append(rect.DOPunchPosition(new Vector3(0, 10f * 0.8f, 0), 0.2f, 2, 0.5f));

            // Ensure it ends exactly where it started
            impactSeq.OnComplete(() =>
            {
                if (CurrentItem == null)
                {
                    Clear();
                }
            });
        }
    }
    public void Clear()
    {
        CurrentItem = null;
        icon.enabled = false;
        // 1. Kill any active tweens on the icon and the slot itself
        icon.transform.DOKill();
        _backgroundImage.rectTransform.DOKill();        // transform.DOKill();
        _backgroundImage.rectTransform.localPosition = _originalSlotPos;
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

        string truncatedUniqueId = CurrentItem.UId.Length > 7
            ? CurrentItem.UId.Substring(0, 7)
            : CurrentItem.UId;

        string text = $"{truncatedUniqueId} \n {CurrentItem.Id}";

        float startY = Screen.height - debugFontSize * 3; // anchor near bottom
        float xSpacing = 220f;                            // horizontal spacing
        float x = 10f + (_index * xSpacing);

        // Use font size for rect height
        GUI.Label(new Rect(x, startY, 400, debugFontSize * 3), text, style);
    }


}