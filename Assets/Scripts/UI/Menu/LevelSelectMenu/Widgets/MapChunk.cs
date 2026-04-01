using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapChunk : MonoBehaviour
{
    public RectTransform RectTransform;
    public MapPath path;

    [SerializeField] private Transform _nodeParent;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _backgroundImageLow;
    [SerializeField] private Image _topFog;
    [SerializeField] private Image _bottomFog;
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private TMP_Text _starRequirementText;

    private List<LevelNode> _nodePool = new List<LevelNode>();
    private string _currentBaseKey;
    private int _currentStartIndex = -1; // THE GATE
    private Sequence _fadeSequence;

    public void Configure(List<LevelDisplayData> levelBatch, int startLevelIndex, GameObject prefab, string bgName, string themeColor)
    {
        // 1. GATE: If data hasn't changed, do not re-run expensive logic
        if (_currentStartIndex == startLevelIndex) return;
        _currentStartIndex = startLevelIndex;

        // 2. Visuals Update
        UpdateVisuals(bgName, themeColor);

        // 3. Pool Nodes (Reuse instead of Destroy)
        for (int i = 0; i < levelBatch.Count; i++)
        {
            LevelNode node;
            if (i < _nodePool.Count)
            {
                node = _nodePool[i];
                node.gameObject.SetActive(true);
            }
            else
            {
                node = Instantiate(prefab, _nodeParent).GetComponent<LevelNode>();
                _nodePool.Add(node);
            }

            float t = (levelBatch.Count > 1) ? (float)i / (levelBatch.Count - 1) : 0f;
            node.transform.localPosition = path.GetPointOnPath(t);
            node.Setup(levelBatch[i], startLevelIndex + i);
        }

        // Hide unused pooled nodes
        for (int i = levelBatch.Count; i < _nodePool.Count; i++)
        {
            _nodePool[i].gameObject.SetActive(false);
        }
    }

    private void UpdateVisuals(string bgName, string themeColor)
    {
        // Release old assets only if key changed
        if (_currentBaseKey != bgName)
        {
            if (!string.IsNullOrEmpty(_currentBaseKey))
            {
                AssetLoader.Instance.ReleaseIcon(_currentBaseKey);
                AssetLoader.Instance.ReleaseIcon(_currentBaseKey + "_low");
            }
            _currentBaseKey = bgName;
            LoadBackgrounds(bgName);
        }
    }

    private void LoadBackgrounds(string bgName)
    {
        _fadeSequence?.Kill();
        _backgroundImage.color = new Color(1, 1, 1, 0);

        AssetLoader.Instance.LoadIcon(bgName + "_low", (lowSprite) =>
        {
            if (lowSprite != null) _backgroundImageLow.sprite = lowSprite;

            AssetLoader.Instance.LoadIcon(bgName, (highSprite) =>
            {
                if (highSprite != null)
                {
                    _backgroundImage.sprite = highSprite;
                    _fadeSequence = DOTween.Sequence();
                    _fadeSequence.Append(_backgroundImage.DOFade(1f, 0.8f));
                }
            });
        });
    }

    public void ShowLockedOverlay(bool show, string text)
    {
        _lockOverlay.SetActive(show);
        _starRequirementText.text = text;
    }

    public void SetCloudTopState(bool show)
    {
        _topFog.gameObject.SetActive(show);
    }

    private void OnDestroy()
    {
        _fadeSequence?.Kill();
        if (!string.IsNullOrEmpty(_currentBaseKey))
        {
            AssetLoader.Instance.ReleaseIcon(_currentBaseKey);
            AssetLoader.Instance.ReleaseIcon(_currentBaseKey + "_low");
        }
    }
}