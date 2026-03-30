using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteMapManager : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private MapChunk[] _chunks;
    [SerializeField] private GameObject _nodePrefab;
    [SerializeField] private int _nodesPerMap = 15;

    private float _chunkHeight;
    private bool _isInitialized = false;

    public async Task InitializeMapAsync()
    {
        _isInitialized = false;
        _scrollRect.StopMovement();
        _scrollRect.enabled = false;

        // 1. Initial Setup
        _content.pivot = new Vector2(0.5f, 0f);
        _content.anchorMin = new Vector2(0.5f, 0f);
        _content.anchorMax = new Vector2(0.5f, 0f);
        _chunkHeight = _chunks[0].RectTransform.rect.height;

        // 2. Setup Data
        await SetupChunksAsync();

        // 3. FORCE EVERYTHING TO CALCULATE RIGHT NOW
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.GetComponent<RectTransform>());

        await Task.Yield();
        Canvas.ForceUpdateCanvases();

        // 7. Finalize
        _scrollRect.enabled = true;
        _isInitialized = true;

        // Manually trigger the first chunk refresh at the new position
        OnScroll(Vector2.zero);
    }

    public void FocusOnLevel(int levelNumber)
    {
        if (!_isInitialized) return;

        float targetY = (levelNumber / (float)_nodesPerMap) * _chunkHeight;
        targetY -= (_scrollRect.viewport.rect.height * 0.5f);

        float maxScroll = _content.sizeDelta.y - _scrollRect.viewport.rect.height;
        targetY = Mathf.Clamp(targetY, 0, maxScroll);

        // 5. THE BULLETPROOF SNAP
        _content.anchoredPosition = new Vector2(0, targetY);

        // AND setting normalized position as a backup (0 is bottom, 1 is top)
        float normalizedY = targetY / maxScroll;
        float currentY = _content.anchoredPosition.y;
        float distance = Mathf.Abs(targetY - currentY);
        float scrollSpeed = 2000f; // Pixels per second
        float duration = Mathf.Clamp(distance / scrollSpeed, 0.5f, 1.5f);
        _scrollRect.DONormalizedPos(new Vector2(0, normalizedY), duration);
    }

    private async Task SetupChunksAsync()
    {
        int playerLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;
        int currentMapIndex = (playerLevel - 1) / _nodesPerMap;

        int peekNumber = 1;
        int maxMapIndexAllowed = currentMapIndex + peekNumber;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, (maxMapIndexAllowed + 1) * _chunkHeight);

        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].RectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);
            UpdateChunkData(_chunks[i]);
            if (i % 2 == 0) await Task.Yield();
        }
    }

    private void OnScroll(Vector2 scrollPos)
    {
        if (!_isInitialized) return;

        float contentY = _content.anchoredPosition.y;
        float viewportHeight = _scrollRect.viewport.rect.height;
        float buffer = _chunkHeight * 0.5f; // Recycle half a chunk early to prevent gaps

        foreach (var chunk in _chunks)
        {
            float chunkY = chunk.RectTransform.anchoredPosition.y;
            float relativeBottom = chunkY + contentY;

            if (relativeBottom < -_chunkHeight - buffer)
            {
                if (CanScrollFurther(chunk, true)) MoveChunk(chunk, true);
            }
            else if (relativeBottom > viewportHeight + buffer)
            {
                if (CanScrollFurther(chunk, false)) MoveChunk(chunk, false);
            }
        }
    }

    private bool CanScrollFurther(MapChunk chunk, bool toTop)
    {
        float currentY = chunk.RectTransform.anchoredPosition.y;
        float moveDist = _chunkHeight * _chunks.Length;
        float targetY = toTop ? currentY + moveDist : currentY - moveDist;
        return targetY >= 0 && targetY < _content.sizeDelta.y;
    }

    private void MoveChunk(MapChunk chunk, bool toTop)
    {
        float moveDist = _chunkHeight * _chunks.Length;
        float newY = toTop ? (chunk.RectTransform.anchoredPosition.y + moveDist) : (chunk.RectTransform.anchoredPosition.y - moveDist);
        chunk.RectTransform.anchoredPosition = new Vector2(0, Mathf.Round(newY));
        UpdateChunkData(chunk);
    }

    private void UpdateChunkData(MapChunk chunk)
    {
        int startIndex = Mathf.RoundToInt(chunk.RectTransform.anchoredPosition.y / _chunkHeight) * _nodesPerMap;
        var data = LevelManager.Instance.GetLevelBatch(startIndex, _nodesPerMap);

        if (data != null && data.Count > 0)
        {
            chunk.gameObject.SetActive(true);
            var theme = DataManager.Instance.GetThemeByLevelNumber(startIndex + 1);
            chunk.Configure(data, startIndex, _nodePrefab, theme.BackgroundSpriteName, theme.FogColorHex);

            int playerStars = GameManager.Instance.SaveData.Inventory.Stars;
            bool isThemeLocked = playerStars < theme.StarRequirement;
            chunk.ShowLockedOverlay(isThemeLocked, isThemeLocked ? $"x{theme.StarRequirement}" : "");
        }
        else
        {
            chunk.gameObject.SetActive(false);
        }
    }
}