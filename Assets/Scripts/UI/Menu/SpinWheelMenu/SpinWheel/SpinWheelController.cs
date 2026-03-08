using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SpinWheelController : MonoBehaviour
{
    [Header("Injected Data")]
    [SerializeField] private RewardIconMapper iconMapper;

    [Header("Wheel Configuration")]
    public int numberOfSlots = 10;
    [SerializeField] private float radiusOffset = 0.8f;
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private RectTransform wheelContainer;

    // ADJUST THIS in Inspector if Index 0 is not under the needle at Start
    // Usually 0, 90, or -90
    [SerializeField] private float visualOffset = 0f;

    [Header("Needle Animation")]
    [SerializeField] private RectTransform needleRect;
    [SerializeField] private float needlePunchAngle = 20f;
    [SerializeField] private float needleReturnSpeed = 5f;

    [Header("Animation")]
    public AnimationCurve Curve;

    [Header("Result UI")]
    public GameObject resultPanel;
    public Image resultIcon;
    public TextMeshProUGUI resultCount;

    private List<SpinWheelData> _currentRewards;
    private List<SpinRewardView> _spawnedComponents = new List<SpinRewardView>();

    private bool _isStarted;
    private float _startAngle;
    private float _endAngle;
    private int _winningSlotIndex = 0;
    private float _currentRotationTime;
    private float _maxRotationTime;

    private float _currentNeedleAngle;
    private float _lastTickAngle;

    private void Awake() => HideResult();

    private Action<SpinWheelData> _onRewardComplete;
    public void Setup(List<SpinWheelData> rewards, Action<SpinWheelData> onRewardComplete)
    {
        _currentRewards = rewards;
        _onRewardComplete = onRewardComplete;
        foreach (var item in _spawnedComponents) if (item != null) Destroy(item.gameObject);
        _spawnedComponents.Clear();

        float angleStep = 360f / numberOfSlots;

        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject go = Instantiate(rewardPrefab, wheelContainer);
            RectTransform rt = go.GetComponent<RectTransform>();

            // 1. Reset Anchors to Center
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localPosition = Vector3.zero;

            // 2. Rotate to the correct slice direction
            // visualOffset helps align slot 0 with your needle
            float targetRotation = -(i * angleStep) - visualOffset;
            rt.localEulerAngles = new Vector3(0, 0, targetRotation);

            // 3. RESPONSIVE RADIUS: 
            // Instead of Translate (Pixels), we use anchoredPosition.
            // We multiply the container's size by our radiusOffset (0.0 to 1.0)
            // This ensures that even if the wheel shrinks for a small phone, the icons stay inside.
            float responsiveRadius = (wheelContainer.rect.width / 2f) * radiusOffset;
            rt.anchoredPosition = rt.up * responsiveRadius;

            // 4. Counter-Rotate the Icon
            // If you want icons to stay "Upright" relative to the world, use 0.
            // If you want them "Upright" relative to the slice, keep them at targetRotation.
            // rt.localEulerAngles = Vector3.zero; // Uncomment if icons should be world-upright

            var comp = go.GetComponent<SpinRewardView>();
            if (i < _currentRewards.Count)
            {
                Sprite icon = iconMapper.GetIcon(_currentRewards[i].Reward.RewardType);
                comp.SetData(icon, _currentRewards[i].Reward.Amount);
            }
            _spawnedComponents.Add(comp);
        }

        wheelContainer.localEulerAngles = Vector3.zero;
    }
    public void TurnWheel()
    {
        if (_isStarted || _currentRewards == null || _currentRewards.Count == 0) return;

        _isStarted = true;
        _currentRotationTime = 0.0f;
        _maxRotationTime = UnityEngine.Random.Range(4.0f, 6.0f);

        _startAngle = wheelContainer.localEulerAngles.z;
        _winningSlotIndex = UnityEngine.Random.Range(0, Mathf.Min(numberOfSlots, _currentRewards.Count));

        float angleStep = 360f / numberOfSlots;
        int fullRotations = UnityEngine.Random.Range(8, 12);

        // TARGETING LOGIC:
        // To bring Slot 'i' to the Top (0°):
        // The wheel must be rotated to (i * angleStep)
        // To land in the CENTER of that slot, we add (angleStep / 2)
        float sectorCenter = (_winningSlotIndex * angleStep) + (angleStep / 2f);

        _endAngle = (fullRotations * 360f) + sectorCenter;

        _lastTickAngle = _startAngle;
    }

    void Update()
    {
        HandleNeedleReturn();

        if (!_isStarted) return;

        _currentRotationTime += Time.deltaTime;
        float t = Mathf.Clamp01(_currentRotationTime / _maxRotationTime);
        float curveStep = Curve.Evaluate(t);

        float currentZ = Mathf.Lerp(_startAngle, _endAngle, curveStep);
        wheelContainer.eulerAngles = new Vector3(0, 0, currentZ);

        // Tick Logic: Based on distance moved
        float angleStep = 360f / numberOfSlots;
        if (Mathf.Abs(currentZ - _lastTickAngle) >= angleStep)
        {
            _currentNeedleAngle = needlePunchAngle;
            _lastTickAngle = currentZ;
            OnTriggerNeedle();
        }

        if (t >= 1f)
        {
            _isStarted = false;
            wheelContainer.eulerAngles = new Vector3(0, 0, _endAngle);
            SettleWheel();
        }
    }

    private void HandleNeedleReturn()
    {
        if (needleRect == null) return;
        _currentNeedleAngle = Mathf.Lerp(_currentNeedleAngle, 0f, Time.deltaTime * needleReturnSpeed);
        needleRect.localEulerAngles = new Vector3(0, 0, -_currentNeedleAngle);
    }

    private float _lastTickTime;
    [SerializeField] private float _tickCooldown = 0.05f; // 50ms minimum between ticks

    public void OnTriggerNeedle()
    {
        if (Time.time - _lastTickTime < _tickCooldown) return;

        _lastTickTime = Time.time;
        SoundController.instance.PlaySoundEffect("tick");
    }

    private void SettleWheel()
    {
        var winData = _currentRewards[_winningSlotIndex];
        _onRewardComplete?.Invoke(winData);
    }

    private void ShowResult(Sprite icon, int amount)
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        resultIcon.sprite = icon;
        resultCount.text = "x" + amount;
        StartCoroutine(DelayedHide());
    }

    private IEnumerator DelayedHide() { yield return new WaitForSeconds(3f); HideResult(); }
    public void HideResult() => resultPanel?.SetActive(false);
}