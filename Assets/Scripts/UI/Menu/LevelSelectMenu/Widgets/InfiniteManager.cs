using System.Collections;
using System.Linq;
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

    void Start()
    {
        // 1. Force stop movement to prevent "ghost" velocity jumps
        _scrollRect.StopMovement();

        _content.pivot = new Vector2(0.5f, 0f);
        _content.anchorMin = new Vector2(0.5f, 0f);
        _content.anchorMax = new Vector2(0.5f, 0f);

        _chunkHeight = _chunks[0].RectTransform.rect.height;

        RefreshAllChunks();

        _scrollRect.onValueChanged.AddListener(OnScroll);

        int currentLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;
        // 2. Use a slight delay to ensure UI layouts are calculated
        StartCoroutine(DelayedFocus(currentLevel));
    }

    private IEnumerator DelayedFocus(int level)
    {
        // Wait for the Canvas to finish its initial layout pass
        yield return new WaitForEndOfFrame();
        FocusOnLevel(level);
    }
    private void RefreshAllChunks()
    {
        int maxUnlocked = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;
        int playerStars = GameManager.Instance.SaveData.Inventory.Stars;

        var nextTheme = DataManager.Instance.Metadata.MapThemes
                        .FirstOrDefault(t => t.StarRequirement > playerStars);

        // Allow scrolling slightly into the locked theme
        int limitLevel = (nextTheme != null) ? nextTheme.StartLevel + _nodesPerMap : maxUnlocked + _nodesPerMap;
        float totalPages = Mathf.Ceil((float)limitLevel / _nodesPerMap);

        // Ensure at least 3 pages exist so chunks have room to cycle
        totalPages = Mathf.Max(totalPages, 3);

        _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalPages * _chunkHeight);

        for (int i = 0; i < _chunks.Length; i++)
        {
            // IMPORTANT: Force chunk pivots to match content
            _chunks[i].RectTransform.pivot = new Vector2(0.5f, 0f);
            _chunks[i].RectTransform.anchorMin = new Vector2(0.5f, 0f);
            _chunks[i].RectTransform.anchorMax = new Vector2(0.5f, 0f);

            // INITIAL STITCHING: 0, 1920, 3840...
            _chunks[i].RectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);
            UpdateChunkData(_chunks[i]);
        }
    }

    private void OnScroll(Vector2 scrollPos)
    {
        float contentY = _content.anchoredPosition.y;
        float viewportHeight = _scrollRect.viewport.rect.height;

        foreach (var chunk in _chunks)
        {
            float chunkY = chunk.RectTransform.anchoredPosition.y;
            float relativeBottom = chunkY + contentY;

            // If chunk fell off the BOTTOM of the viewport
            if (relativeBottom < -_chunkHeight)
            {
                if (CanScrollFurther(chunk, true))
                    MoveChunk(chunk, true);
            }
            // If chunk went past the TOP of the viewport
            else if (relativeBottom > viewportHeight + _chunkHeight)
            {
                if (CanScrollFurther(chunk, false))
                    MoveChunk(chunk, false);
            }
        }
    }

    private bool CanScrollFurther(MapChunk chunk, bool toTop)
    {
        float currentY = chunk.RectTransform.anchoredPosition.y;
        float moveDist = _chunkHeight * _chunks.Length;
        float targetY = toTop ? currentY + moveDist : currentY - moveDist;

        // Ensure the new position is within the calculated content bounds
        return targetY >= 0 && targetY < _content.sizeDelta.y;
    }

    private void MoveChunk(MapChunk chunk, bool toTop)
    {
        float moveDist = _chunkHeight * _chunks.Length;
        float currentY = chunk.RectTransform.anchoredPosition.y;
        float newY = toTop ? (currentY + moveDist) : (currentY - moveDist);

        chunk.RectTransform.anchoredPosition = new Vector2(0, Mathf.Round(newY));
        UpdateChunkData(chunk);
    }

    private void UpdateChunkData(MapChunk chunk)
    {
        int startIndex = CalculateStartIndex(chunk);
        var data = LevelManager.Instance.GetLevelBatch(startIndex, _nodesPerMap);

        if (data != null && data.Count > 0)
        {
            chunk.gameObject.SetActive(true);

            // Get theme based on the first level of this specific chunk
            int firstLevelNum = startIndex + 1;
            var theme = DataManager.Instance.GetThemeByLevelNumber(firstLevelNum);

            // Configure the chunk (Background, Fog, Nodes)
            chunk.Configure(data, startIndex, _nodePrefab, theme.BackgroundSpriteName, theme.FogColorHex);

            // Apply Stars Gate
            int playerStars = GameManager.Instance.SaveData.Inventory.Stars;
            bool isThemeLocked = playerStars < theme.StarRequirement;

            chunk.ShowLockedOverlay(isThemeLocked, isThemeLocked ? $"x{theme.StarRequirement}" : "");
        }
        else
        {
            chunk.gameObject.SetActive(false);
        }
    }

    private int CalculateStartIndex(MapChunk chunk)
    {
        float yPos = chunk.RectTransform.anchoredPosition.y;
        int mapIndex = Mathf.RoundToInt(yPos / _chunkHeight);
        return mapIndex * _nodesPerMap;
    }

    public void FocusOnLevel(int levelNumber)
    {
        _scrollRect.StopMovement(); // 3. IMPORTANT: Clear any current scrolling

        int levelIndex = levelNumber - 1;
        float targetY = (levelIndex / (float)_nodesPerMap) * _chunkHeight;
        targetY -= (_scrollRect.viewport.rect.height / 2f);

        float maxScroll = _content.sizeDelta.y - _scrollRect.viewport.rect.height;
        targetY = Mathf.Clamp(targetY, 0, maxScroll);

        StopAllCoroutines();
        StartCoroutine(SmoothScroll(targetY, 1.0f));
    }

    private IEnumerator SmoothScroll(float targetY, float duration)
    {
        float elapsed = 0;
        Vector2 startPos = _content.anchoredPosition;
        // In upward scrolling, we set content.y to a positive targetY to "pull" the top down
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - (elapsed / duration), 4);
            _content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        _content.anchoredPosition = endPos;
    }
}