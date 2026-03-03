using System.Collections.Generic;
using UnityEngine;

public class CircularUIAnimator : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private RectTransform container;

    [Header("Responsive Settings")]
    // Use 0.0 to 1.0 (e.g., 0.4 means 40% of container width)
    [Range(0, 1f)][SerializeField] private float radiusPercentA = 0.2f;
    [Range(0, 1f)][SerializeField] private float radiusPercentB = 0.4f;

    // Bring back the offsets for rotation
    [SerializeField] private float angleOffsetA = 0f;
    [SerializeField] private float angleOffsetB = 0f;

    [Header("Set A Settings")]
    [SerializeField] private int countA = 10;
    [SerializeField] private float intervalA = 1.0f;

    [Header("Set B Settings")]
    [SerializeField] private int countB = 10;
    [SerializeField] private float intervalB = 0.5f;
    [SerializeField] private bool counterClockwiseB = false;

    private List<RectTransform> _itemsA = new List<RectTransform>();
    private List<RectTransform> _itemsB = new List<RectTransform>();

    private float _timerA, _timerB;
    private bool _stateA;
    private int _currentIndexB;

    private void Start()
    {
        // Set container anchors to Center/Middle to make math easier
        container.pivot = new Vector2(0.5f, 0.5f);

        SpawnSet(countA, _itemsA);
        SpawnSet(countB, _itemsB);

        // Initial position update
        UpdatePositions();
    }

    private void SpawnSet(int count, List<RectTransform> list)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject item = Instantiate(prefab, container);
            RectTransform rt = item.GetComponent<RectTransform>();

            // Force center anchors so position (0,0) is center of parent
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            list.Add(rt);
        }
    }

    private void Update()
    {
        HandleSetA();
        HandleSetB();

        // Update positions every frame to handle screen resizing/rotation
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        float referenceSize = Mathf.Min(container.rect.width, container.rect.height) * 0.5f;

        // Pass the offsets into the positioning logic
        PositionList(_itemsA, referenceSize * radiusPercentA, angleOffsetA);
        PositionList(_itemsB, referenceSize * radiusPercentB, angleOffsetB);
    }
    private void PositionList(List<RectTransform> list, float actualRadius, float offsetDeg)
    {
        for (int i = 0; i < list.Count; i++)
        {
            // Add the offsetDeg to the per-item angle calculation
            float angleDeg = (i * (360f / list.Count)) + offsetDeg;

            // 90 is the top of the circle in Unity UI space
            float angleRad = (90f - angleDeg) * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * actualRadius;
            float y = Mathf.Sin(angleRad) * actualRadius;

            list[i].anchoredPosition = new Vector2(x, y);

            // This keeps the "light" or "dot" facing inward/outward correctly
            list[i].localEulerAngles = new Vector3(0, 0, -angleDeg);
        }
    }

    private void HandleSetA()
    {
        _timerA += Time.deltaTime;
        if (_timerA >= intervalA)
        {
            _timerA = 0;
            _stateA = !_stateA;
            for (int i = 0; i < _itemsA.Count; i++)
                _itemsA[i].gameObject.SetActive((i % 2 == 0) ? _stateA : !_stateA);
        }
    }

    private void HandleSetB()
    {
        _timerB += Time.deltaTime;
        if (_timerB >= intervalB)
        {
            _timerB = 0;
            if (counterClockwiseB)
            {
                _currentIndexB = (_currentIndexB <= 0) ? _itemsB.Count - 1 : _currentIndexB - 1;
            }
            else
            {
                _currentIndexB = (_currentIndexB + 1) % _itemsB.Count;
            }

            int oppositeIndex = (_currentIndexB + (countB / 2)) % _itemsB.Count;
            for (int i = 0; i < _itemsB.Count; i++)
                _itemsB[i].gameObject.SetActive(i == _currentIndexB || i == oppositeIndex);
        }
    }
}