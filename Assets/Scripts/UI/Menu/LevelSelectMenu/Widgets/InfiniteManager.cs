using System.Collections;
using System.Linq;
using System.Threading.Tasks;
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

    // Remove Start() logic and put it here
    public async Task InitializeMapAsync()
    {
        // 1. Basic Setup (Synchronous)
        _scrollRect.StopMovement();
        _content.pivot = new Vector2(0.5f, 0f);
        _content.anchorMin = new Vector2(0.5f, 0f);
        _content.anchorMax = new Vector2(0.5f, 0f);
        _chunkHeight = _chunks[0].RectTransform.rect.height;

        // 2. Perform Data and Layout Calculations (Asynchronous)
        // This prevents the frame from locking up during the loop
        await SetupChunksAsync();

        // 3. Final Focus
        int currentLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;

        // Wait for one frame to ensure RectTransform changes have propagated to the GPU/UI System
        await Task.Yield();

        // FocusOnLevel(currentLevel);
    }

    private async Task SetupChunksAsync()
    {
        int playerLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;
        int currentMapIndex = (playerLevel - 1) / _nodesPerMap;

        int peekNumber = 1;
        int maxMapIndexAllowed = currentMapIndex + peekNumber;
        float totalPages = maxMapIndexAllowed + 1;

        // Set content height
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalPages * _chunkHeight);

        // Position Chunks
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].RectTransform.pivot = new Vector2(0.5f, 0f);
            _chunks[i].RectTransform.anchorMin = new Vector2(0.5f, 0f);
            _chunks[i].RectTransform.anchorMax = new Vector2(0.5f, 0f);

            _chunks[i].RectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);

            // If UpdateChunkData involves disk/DB, we await it
            UpdateChunkData(_chunks[i]);

            // Optimization: Yield every few chunks if the list is long
            if (i % 2 == 0) await Task.Yield();
        }

        _scrollRect.onValueChanged.AddListener(OnScroll);
    }

    // private IEnumerator DelayedFocus(int level)
    // {
    //     // Wait for the Canvas to finish its initial layout pass
    //     yield return new WaitForEndOfFrame();
    //     FocusOnLevel(level);
    // }
    // private void RefreshAllChunks()
    // {
    //     int playerLevel = DataManager.Instance.GetLevelByID(GameManager.Instance.SaveData.CurrentLevelID).Number;
    //     int playerStars = GameManager.Instance.SaveData.Inventory.Stars;

    //     // 1. Determine the "Current Map Index" (0-based)
    //     int currentMapIndex = (playerLevel - 1) / _nodesPerMap;

    //     // 2. Define the Limit: Current Map + 2 extra chunks
    //     // This defines the maximum height the user can ever scroll to
    //     // show upto peekNumber
    //     int peekNumber = 1;
    //     int maxMapIndexAllowed = currentMapIndex + peekNumber;
    //     float totalPages = maxMapIndexAllowed + 1; // +1 because index 0 is page 1

    //     // Update Content Height
    //     _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalPages * _chunkHeight);

    //     // Initial Chunk Placement
    //     for (int i = 0; i < _chunks.Length; i++)
    //     {
    //         _chunks[i].RectTransform.pivot = new Vector2(0.5f, 0f);
    //         _chunks[i].RectTransform.anchorMin = new Vector2(0.5f, 0f);
    //         _chunks[i].RectTransform.anchorMax = new Vector2(0.5f, 0f);

    //         _chunks[i].RectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);
    //         UpdateChunkData(_chunks[i]);
    //     }
    // }


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