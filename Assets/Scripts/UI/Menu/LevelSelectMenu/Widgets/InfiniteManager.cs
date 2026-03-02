using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteMapManager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private MapChunk[] chunks; // Assign 3 chunks
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private int nodesPerMap = 15;

    private float _chunkHeight;

    void Start()
    {
        // 1. Force Bottom Pivots for upward math
        content.pivot = new Vector2(0.5f, 0f);
        content.anchorMin = new Vector2(0.5f, 0f);
        content.anchorMax = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;

        _chunkHeight = chunks[0].rectTransform.rect.height;

        // 2. Initial Setup
        RefreshAllChunks();

        // 3. Event-based movement (The jitter-killer)
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void RefreshAllChunks()
    {
        var allLevels = LevelManager.Instance.GetLevelSelectData();
        float totalPages = Mathf.Ceil((float)allLevels.Count / nodesPerMap);

        // Set content height
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalPages * _chunkHeight);

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            chunks[i].rectTransform.anchorMin = new Vector2(0.5f, 0f);
            chunks[i].rectTransform.anchorMax = new Vector2(0.5f, 0f);

            // Stack them UP: 0, Height, 2*Height
            chunks[i].rectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);
            UpdateChunkData(chunks[i]);
        }
    }

    private void OnScroll(Vector2 scrollPos)
    {
        float contentY = content.anchoredPosition.y; // How far content has moved UP (negative)
        float viewportHeight = scrollRect.viewport.rect.height;

        foreach (var chunk in chunks)
        {
            float chunkY = chunk.rectTransform.anchoredPosition.y; // Positive (0, 1920, etc)
            float relativeBottom = chunkY + contentY;

            // If chunk is too far BELOW the viewport (fell off the bottom)
            if (relativeBottom < -_chunkHeight)
            {
                MoveChunk(chunk, true); // Move to Top
            }
            // If chunk is too far ABOVE the viewport (went past the top)
            else if (relativeBottom > viewportHeight + _chunkHeight)
            {
                MoveChunk(chunk, false); // Move to Bottom
            }
        }
    }

    private void MoveChunk(MapChunk chunk, bool toTop)
    {
        float moveDist = _chunkHeight * chunks.Length;
        float currentY = chunk.rectTransform.anchoredPosition.y;
        float newY = toTop ? (currentY + moveDist) : (currentY - moveDist);

        // Boundary checks
        if (newY < 0 || newY >= content.sizeDelta.y) return;

        // Snap to whole pixels to prevent sub-pixel "shimmering"
        chunk.rectTransform.anchoredPosition = new Vector2(0, Mathf.Round(newY));
        UpdateChunkData(chunk);
    }

    private void UpdateChunkData(MapChunk chunk)
    {
        int mapIndex = Mathf.RoundToInt(chunk.rectTransform.anchoredPosition.y / _chunkHeight);
        int startIndex = mapIndex * nodesPerMap;

        var data = LevelManager.Instance.GetLevelBatch(startIndex, nodesPerMap);

        if (data != null && data.Count > 0)
        {
            chunk.gameObject.SetActive(true);
            var theme = DataManager.Instance.GetThemeByLevelNumber(startIndex + 1);
            chunk.Configure(data, startIndex, nodePrefab, theme.BackgroundSpriteName, theme.FogColorHex);
        }
        else
        {
            chunk.gameObject.SetActive(false);
        }
    }

    public void FocusOnLevel(int levelNumber)
    {
        // 1. Calculate which index this is in the data (0-based)
        int levelIndex = levelNumber - 1;
        if (levelIndex < 5) return;
        // 2. Calculate the exact Y position
        // Formula: (LevelIndex / TotalNodesPerChunk) * ChunkHeight
        float targetY = (levelIndex / (float)nodesPerMap) * _chunkHeight;

        // 3. Clamp targetY so we don't scroll past the very top/bottom of content
        float maxScroll = content.sizeDelta.y - scrollRect.viewport.rect.height;
        targetY = Mathf.Clamp(targetY, 0, maxScroll);

        float duration = 0.3f + Mathf.Log(levelIndex + 1, 5f);
        duration = Mathf.Min(duration, 1.5f);
        // 4. Start the smooth glide
        StopAllCoroutines();
        StartCoroutine(SmoothScroll(targetY, duration)); // 0.5 seconds duration
    }

    private IEnumerator SmoothScroll(float targetY, float duration)
    {
        float elapsed = 0;
        Vector2 startPos = content.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY); // Match your upward math

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Exponential "Quartic" Out easing: Starts fast, slows down at the end
            t = 1f - Mathf.Pow(1f - t, 4);

            content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        content.anchoredPosition = endPos;

        // IMPORTANT: Final snap check to ensure chunks are exactly where they should be
        // after a high-speed travel
        foreach (var chunk in chunks)
        {
            UpdateChunkData(chunk);
        }
    }
}