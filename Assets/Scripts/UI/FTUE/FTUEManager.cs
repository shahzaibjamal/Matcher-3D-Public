using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance { get; private set; }

    [SerializeField] private bool debugUpdateOffsets = true;

    [Header("References")]
    [SerializeField] private FTUEDatabase database;
    [SerializeField] private Canvas ftueCanvas;
    [SerializeField] private FTUEMask maskScript;
    [SerializeField] private FTUETypewriter typewriter;

    [Header("Smart Tooltip")]
    [SerializeField] private RectTransform tooltipContainer;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0, 150);
    [SerializeField] private float screenPadding = 50f;
    [SerializeField] private float popDuration = 0.4f;

    [Header("Smart Pointer")]
    [SerializeField] private RectTransform pointerHand;
    [SerializeField] private Vector2 handOffset = new Vector2(0, 80);
    [SerializeField] private float bounceSpeed = 5f;
    [SerializeField] private float bounceDistance = 30f;

    public bool IsTyping => typewriter != null && typewriter.IsTyping;

    private Dictionary<string, Transform> _registeredTargets = new Dictionary<string, Transform>();
    private FTUESequence _activeSequence;
    private int _stepIndex;
    private Coroutine _activeStepRoutine;
    private Coroutine _pointerCoroutine;
    private bool _waitingForSignal;

    // Caching for runtime tuning
    private Vector2 _currentTargetScreenPos;
    private PointerDirection _currentDirection;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ftueCanvas.enabled = false;
        tooltipContainer.gameObject.SetActive(false);
        pointerHand.gameObject.SetActive(false);
        tooltipContainer.localScale = Vector3.zero;
    }

    private void Update()
    {
        // Real-time offset tuning in the Inspector
        if (debugUpdateOffsets && IsTutorialActive() && _activeSequence != null)
        {
            var step = _activeSequence.steps[_stepIndex];
            if (step.ShowCutout)
            {
                PositionSmartPointer(_currentTargetScreenPos, _currentDirection);
                PositionSmartTooltip(_currentTargetScreenPos, step.Message, _currentDirection, false);
            }
        }
    }

    public void PlayTutorial(string sequenceID)
    {
        _activeSequence = database.GetByID(sequenceID);
        if (_activeSequence == null || IsSequenceCompleted(sequenceID)) return;

        _stepIndex = 0;
        ftueCanvas.enabled = true;
        StartStep();
    }

    private void StartStep()
    {
        if (_activeStepRoutine != null) StopCoroutine(_activeStepRoutine);
        _activeStepRoutine = StartCoroutine(ExecuteStepRoutine());
    }

    private IEnumerator ExecuteStepRoutine()
    {
        FTUEStep step = _activeSequence.steps[_stepIndex];
        _waitingForSignal = !string.IsNullOrEmpty(step.RequiredEvent);

        if (step.ShowCutout)
        {
            // Wait for target to be registered (e.g., if UI is spawning)
            while (!_registeredTargets.ContainsKey(step.TargetID)) yield return null;
            Transform target = _registeredTargets[step.TargetID];

            // 1. Update Shader Mask
            maskScript.SetState(target, true, step.CustomSize > 0 ? step.CustomSize : 0.15f);

            // 2. Cache Positions
            _currentTargetScreenPos = GetTargetScreenPoint(target);
            _currentDirection = step.HandDirection;

            // 3. Position and Trigger UI
            PositionSmartPointer(_currentTargetScreenPos, _currentDirection);
            PositionSmartTooltip(_currentTargetScreenPos, step.Message, _currentDirection, true);

            // 4. Start Typewriter
            typewriter.ShowMessage(step.Message);
        }
        else
        {
            // Narrative Mode (No Cutout)
            maskScript.SetState(null, false);
            pointerHand.gameObject.SetActive(false);
            PositionNarrativeTooltip(step.Message);
            typewriter.ShowMessage(step.Message);
        }
    }

    private void PositionSmartTooltip(Vector2 targetPos, string message, PointerDirection handDir, bool useTween)
    {
        tooltipContainer.gameObject.SetActive(true);
        tooltipText.text = message;

        // Force layout update to get actual width/height for clamping
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipContainer);

        Vector2 finalPos = targetPos;
        float yFlip = (targetPos.y < Screen.height * 0.5f) ? 1 : -1;
        float xFlip = (targetPos.x < Screen.width * 0.5f) ? 1 : -1;

        finalPos.x += tooltipOffset.x * xFlip;
        finalPos.y += tooltipOffset.y * yFlip;

        float halfWidth = (tooltipContainer.rect.width / 2) + screenPadding;
        float halfHeight = (tooltipContainer.rect.height / 2) + screenPadding;

        finalPos.x = Mathf.Clamp(finalPos.x, halfWidth, Screen.width - halfWidth);
        finalPos.y = Mathf.Clamp(finalPos.y, halfHeight, Screen.height - halfHeight);

        tooltipContainer.position = finalPos;

        if (useTween)
        {
            tooltipContainer.DOKill();
            tooltipContainer.localScale = Vector3.zero;
            tooltipContainer.DOScale(Vector3.one, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void PositionNarrativeTooltip(string message)
    {
        tooltipContainer.gameObject.SetActive(true);
        tooltipText.text = message;

        // Narrative position (Center Bottom)
        tooltipContainer.position = new Vector2(Screen.width / 2, Screen.height * 0.2f);

        tooltipContainer.DOKill();
        tooltipContainer.localScale = Vector3.zero;
        tooltipContainer.DOScale(Vector3.one, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void PositionSmartPointer(Vector2 targetPos, PointerDirection dir)
    {
        if (dir == PointerDirection.None)
        {
            pointerHand.gameObject.SetActive(false);
            return;
        }

        pointerHand.gameObject.SetActive(true);
        Vector2 dirVec = GetDirectionVector(dir);

        float angle = Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg + 90f;
        pointerHand.localEulerAngles = new Vector3(0, 0, angle);
        pointerHand.position = targetPos + (dirVec * handOffset.magnitude);

        if (_pointerCoroutine != null) StopCoroutine(_pointerCoroutine);
        _pointerCoroutine = StartCoroutine(AnimatePointer(dirVec));
    }

    private IEnumerator AnimatePointer(Vector2 dir)
    {
        while (true)
        {
            Vector2 dirVec = GetDirectionVector(_currentDirection);
            Vector3 centerPos = (Vector3)_currentTargetScreenPos + (Vector3)(dirVec * handOffset.magnitude);
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceDistance;
            pointerHand.position = centerPos + (Vector3)(dirVec * bounce);
            yield return null;
        }
    }

    public void Advance()
    {
        // Don't advance if text is typing or waiting for a specific game event
        if (IsTyping || _waitingForSignal) return;

        _stepIndex++;
        if (_stepIndex < _activeSequence.steps.Count) StartStep();
        else EndTutorial();
    }

    public void EndTutorial()
    {
        if (_activeSequence != null)
        {
            string seqName = _activeSequence.name;
            if (!IsSequenceCompleted(seqName))
            {
                GameManager.Instance.SaveData.CompletedFTUESequences.Add(seqName);
                GameManager.Instance.SaveGame();
            }
        }

        ftueCanvas.enabled = false;
        maskScript.SetState(null, false);
        tooltipContainer.localScale = Vector3.zero;
        StopAllCoroutines();
    }

    // --- Helpers ---

    private Vector2 GetTargetScreenPoint(Transform target)
    {
        if (target is RectTransform rect)
        {
            Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
            return RectTransformUtility.WorldToScreenPoint(null, worldCenter);
        }

        if (target.TryGetComponent<Renderer>(out var ren))
            return Camera.main.WorldToScreenPoint(ren.bounds.center);

        return Camera.main.WorldToScreenPoint(target.position);
    }

    private Vector2 GetDirectionVector(PointerDirection dir)
    {
        switch (dir)
        {
            case PointerDirection.Top: return Vector2.up;
            case PointerDirection.Bottom: return Vector2.down;
            case PointerDirection.Left: return Vector2.left;
            case PointerDirection.Right: return Vector2.right;
            default: return Vector2.zero;
        }
    }

    public void Register(string id, Transform tr) => _registeredTargets[id] = tr;
    public void Unregister(string id) => _registeredTargets.Remove(id);
    public bool IsTutorialActive() => ftueCanvas != null && ftueCanvas.enabled;
    public bool IsCurrentTarget(string id) => _activeSequence != null && _stepIndex < _activeSequence.steps.Count && _activeSequence.steps[_stepIndex].TargetID == id;
    public bool CurrentStepRequiresClick() => _activeSequence != null && _stepIndex < _activeSequence.steps.Count && _activeSequence.steps[_stepIndex].RequireClick;
    public bool IsSequenceCompleted(string sequenceID) => GameManager.Instance?.SaveData?.CompletedFTUESequences?.Contains(sequenceID) ?? false;
}