using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private List<RewardData> _currentRewards;
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

    public void Setup(List<RewardData> rewards)
    {
        _currentRewards = rewards;
        foreach (var item in _spawnedComponents) if (item != null) Destroy(item.gameObject);
        _spawnedComponents.Clear();

        float wheelRadius = wheelContainer.rect.width / 2f;
        float spawnRadius = wheelRadius * radiusOffset;
        float angleStep = 360f / numberOfSlots;

        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject go = Instantiate(rewardPrefab, wheelContainer);
            RectTransform rt = go.GetComponent<RectTransform>();

            // POSITIONING: Strictly based on Rotation
            // We rotate the container, move the item UP, then rotate back
            rt.localPosition = Vector3.zero;
            rt.localEulerAngles = new Vector3(0, 0, -(i * angleStep) - visualOffset);
            rt.Translate(Vector3.up * spawnRadius, Space.Self);

            // Keep the icon upright relative to the slice
            rt.localEulerAngles = new Vector3(0, 0, -(i * angleStep) - visualOffset);

            var comp = go.GetComponent<SpinRewardView>();
            if (i < _currentRewards.Count)
            {
                Sprite icon = iconMapper.GetIcon(_currentRewards[i].RewardType);
                comp.SetData(icon, _currentRewards[i].Amount);
            }
            _spawnedComponents.Add(comp);
        }

        // Ensure wheel starts at 0
        wheelContainer.localEulerAngles = Vector3.zero;
    }

    public void TurnWheel()
    {
        if (_isStarted || _currentRewards == null || _currentRewards.Count == 0) return;

        _isStarted = true;
        _currentRotationTime = 0.0f;
        _maxRotationTime = Random.Range(4.0f, 6.0f);

        _startAngle = wheelContainer.localEulerAngles.z;
        _winningSlotIndex = Random.Range(0, Mathf.Min(numberOfSlots, _currentRewards.Count));

        float angleStep = 360f / numberOfSlots;
        int fullRotations = Random.Range(8, 12);

        // TARGETING LOGIC:
        // To bring Slot 'i' to the Top (0°):
        // The wheel must be rotated to (i * angleStep)
        // To land in the CENTER of that slot, we add (angleStep / 2)
        float sectorCenter = (_winningSlotIndex * angleStep) + (angleStep / 2f);

        // Subtract from a large multiple of 360 to ensure clockwise spin
        _endAngle = _startAngle - (fullRotations * 360f) - sectorCenter;

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
        wheelContainer.localEulerAngles = new Vector3(0, 0, currentZ);

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
            wheelContainer.localEulerAngles = new Vector3(0, 0, _endAngle);
            SettleWheel();
        }
    }

    private void HandleNeedleReturn()
    {
        if (needleRect == null) return;
        _currentNeedleAngle = Mathf.Lerp(_currentNeedleAngle, 0f, Time.deltaTime * needleReturnSpeed);
        needleRect.localEulerAngles = new Vector3(0, 0, _currentNeedleAngle);
    }

    public void OnTriggerNeedle() => Debug.Log("Tick!");

    private void SettleWheel()
    {
        var winData = _currentRewards[_winningSlotIndex];
        ShowResult(iconMapper.GetIcon(winData.RewardType), winData.Amount);
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