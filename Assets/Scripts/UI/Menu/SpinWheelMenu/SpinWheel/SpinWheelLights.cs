using System.Collections.Generic;
using UnityEngine;

public class CircularUIAnimator : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private RectTransform container;

    [Header("Set A: Alternating (Evens/Odds)")]
    [SerializeField] private int countA = 10;
    [SerializeField] private float radiusA = 200f;
    [SerializeField] private float angleOffsetA = 0f;
    [SerializeField] private float intervalA = 1.0f;

    [Header("Set B: Cycling (N and N+5)")]
    [SerializeField] private int countB = 10;
    [SerializeField] private float radiusB = 350f;
    [SerializeField] private float angleOffsetB = 0f;
    [SerializeField] private float intervalB = 0.5f;
    [SerializeField] private bool counterClockwiseB = false;

    private List<GameObject> _itemsA = new List<GameObject>();
    private List<GameObject> _itemsB = new List<GameObject>();

    private float _timerA;
    private float _timerB;
    private bool _stateA;
    private int _currentIndexB;

    private void Start()
    {
        SpawnSet(countA, radiusA, angleOffsetA, _itemsA);
        SpawnSet(countB, radiusB, angleOffsetB, _itemsB);
    }

    private void SpawnSet(int count, float radius, float offset, List<GameObject> list)
    {
        for (int i = 0; i < count; i++)
        {
            // Calculate angle: 90 is top, subtract degrees to go clockwise
            float angleDeg = (i * (360f / count)) + offset;
            float angleRad = (90f - angleDeg) * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;

            GameObject item = Instantiate(prefab, container);
            RectTransform rt = item.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(x, y);
            rt.localEulerAngles = new Vector3(0, 0, -angleDeg); // Optional: face outward

            list.Add(item);
        }
    }

    private void Update()
    {
        HandleSetA();
        HandleSetB();
    }

    private void HandleSetA()
    {
        _timerA += Time.deltaTime;
        if (_timerA >= intervalA)
        {
            _timerA = 0;
            _stateA = !_stateA;

            for (int i = 0; i < _itemsA.Count; i++)
            {
                // Toggle evens on/off vs odds off/on
                bool isEven = i % 2 == 0;
                _itemsA[i].SetActive(isEven ? _stateA : !_stateA);
            }
        }
    }

    // private void HandleSetB()
    // {
    //     _timerB += Time.deltaTime;
    //     if (_timerB >= intervalB)
    //     {
    //         _timerB = 0;
    //         _currentIndexB = (_currentIndexB + 1) % _itemsB.Count;

    //         int oppositeIndex = (_currentIndexB + 5) % _itemsB.Count;

    //         for (int i = 0; i < _itemsB.Count; i++)
    //         {
    //             // Only N and N+5 are active
    //             bool shouldBeActive = (i == _currentIndexB || i == oppositeIndex);
    //             _itemsB[i].SetActive(shouldBeActive);
    //         }
    //     }
    // }
    private void HandleSetB()
    {
        _timerB += Time.deltaTime;
        if (_timerB >= intervalB)
        {
            _timerB = 0;

            // Calculate next index based on direction
            if (counterClockwiseB)
            {
                _currentIndexB--;
                // Wrap around: if below 0, jump to the last index (e.g., 9)
                if (_currentIndexB < 0) _currentIndexB = _itemsB.Count - 1;
            }
            else
            {
                // Standard clockwise increment with modulo wrap
                _currentIndexB = (_currentIndexB + 1) % _itemsB.Count;
            }

            // Calculate the opposite index (e.g., N + 5)
            int oppositeIndex = (_currentIndexB + (countB / 2)) % _itemsB.Count;

            // Apply SetActive
            for (int i = 0; i < _itemsB.Count; i++)
            {
                bool isActive = (i == _currentIndexB || i == oppositeIndex);
                _itemsB[i].SetActive(isActive);
            }
        }
    }
}