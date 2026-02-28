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
    [SerializeField] private float startAngleOffset = 0f;
    [SerializeField] private RectTransform wheelContainer;

    [Header("Needle Animation")]
    [SerializeField] private RectTransform needleRect;
    [SerializeField] private float needlePunchAngle = 45f;
    [SerializeField] private float needleReturnSpeed = 5f;

    [Header("Animation")]
    public AnimationCurve Curve;

    [Header("Result UI")]
    public GameObject resultPanel;
    public Image resultIcon;
    public TextMeshProUGUI resultCount;

    private List<SpinRewardData> _currentRewards;
    private List<SpinRewardView> _spawnedComponents = new List<SpinRewardView>();

    private bool _isStarted;
    private float _startAngle;
    private float _endAngle;
    private int _winningSlotIndex = 0;
    private float _currentRotationTime;
    private float _maxRotationTime;

    // Needle Tracking
    private float _currentNeedleAngle;
    private float _lastTickAngle;

    private void Awake() => HideResult();


    public void Setup(List<SpinRewardData> rewards)
    {
        _currentRewards = rewards;
        foreach (var item in _spawnedComponents) Destroy(item.gameObject);
        _spawnedComponents.Clear();

        float wheelRadius = wheelContainer.rect.width / 2f;
        float spawnRadius = wheelRadius * radiusOffset;
        float angleStep = 360f / numberOfSlots;

        for (int i = 0; i < numberOfSlots; i++)
        {
            // Apply the angle offset here
            float angleDeg = (i * angleStep) + startAngleOffset;

            // Convert to Radians (90 is top)
            float angleRad = (90f - angleDeg) * Mathf.Deg2Rad;

            GameObject go = Instantiate(rewardPrefab, wheelContainer);
            RectTransform rt = go.GetComponent<RectTransform>();

            // Cartesian position
            rt.anchoredPosition = new Vector2(
                Mathf.Cos(angleRad) * spawnRadius,
                Mathf.Sin(angleRad) * spawnRadius
            );

            // Rotate prefab to stay perpendicular to the center
            rt.localEulerAngles = new Vector3(0, 0, -angleDeg);

            var comp = go.GetComponent<SpinRewardView>();
            if (i < _currentRewards.Count)
            {
                Sprite icon = iconMapper.GetIcon(_currentRewards[i].SpinRewardType);
                comp.SetData(icon, _currentRewards[i].Amount);
            }
            _spawnedComponents.Add(comp);
        }
    }

    public void TurnWheel()
    {
        if (_isStarted || _currentRewards == null || _currentRewards.Count == 0) return;

        _isStarted = true;
        _currentRotationTime = 0.0f;
        _maxRotationTime = Random.Range(4.0f, 6.0f);

        // Reset needle tracking
        _startAngle = wheelContainer.localEulerAngles.z;
        _lastTickAngle = _startAngle;

        _winningSlotIndex = Random.Range(0, Mathf.Min(numberOfSlots, _currentRewards.Count));

        float angleStep = 360f / numberOfSlots;
        int fullRotations = Random.Range(8, 12);

        // We target the winning slot. 
        // We must subtract the startAngleOffset so the specific slot lands at the pointer.
        _endAngle = -(fullRotations * 360f + (_winningSlotIndex * angleStep) + startAngleOffset);
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

        // --- Needle Tick Logic ---
        float angleStep = 360f / numberOfSlots;
        // float localizedAngle = Mathf.Abs(currentZ - startAngleOffset);
        if (Mathf.Abs(currentZ - _lastTickAngle) >= angleStep)
        {
            _currentNeedleAngle = needlePunchAngle;
            _lastTickAngle = currentZ;
            OnTriggerNeedle();
        }

        if (t >= 1f)
        {
            _isStarted = false;
            SettleWheel();
        }
    }

    private void HandleNeedleReturn()
    {
        if (needleRect == null) return;

        // Smoothly bring the needle back to 0
        _currentNeedleAngle = Mathf.Lerp(_currentNeedleAngle, 0f, Time.deltaTime * needleReturnSpeed);
        needleRect.localEulerAngles = new Vector3(0, 0, _currentNeedleAngle);
    }

    public void OnTriggerNeedle()
    {
        // Play sound here: SoundManager.Instance.PlayTick();
        Debug.Log("Tick!");
    }

    // ... SettleWheel, ShowResult, etc. remain the same ...
    private void SettleWheel()
    {
        var winData = _currentRewards[_winningSlotIndex];

        Debug.LogError("winData.Amount = " + winData.Amount + " Reward Type " + winData.SpinRewardType);
        ShowResult(iconMapper.GetIcon(winData.SpinRewardType), winData.Amount);
    }

    private void ShowResult(Sprite icon, int amount)
    {
        if (resultPanel)
        {
            resultPanel.SetActive(true);
            resultIcon.sprite = icon;
            resultCount.text = "x" + amount;
            StartCoroutine(DelayedHide());
        }
    }

    private IEnumerator DelayedHide() { yield return new WaitForSeconds(3f); HideResult(); }
    public void HideResult() => resultPanel?.SetActive(false);
}