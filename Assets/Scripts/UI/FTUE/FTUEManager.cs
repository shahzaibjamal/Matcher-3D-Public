using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance { get; private set; }

    [SerializeField] private bool debugUpdateOffsets = true;
    [Header("References")]
    [SerializeField] private FTUEDatabase database;
    [SerializeField] private Canvas ftueCanvas;
    [SerializeField] private FTUEMask maskScript;

    [Header("Smart Tooltip")]
    [SerializeField] private RectTransform tooltipContainer;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0, 150);
    [SerializeField] private float screenPadding = 50f;

    [Header("Smart Pointer")]
    [SerializeField] private RectTransform pointerHand;
    [SerializeField] private Vector2 handOffset = new Vector2(0, 80);
    [SerializeField] private float bounceSpeed = 5f;
    [SerializeField] private float bounceDistance = 30f;

    private Dictionary<string, Transform> _registeredTargets = new Dictionary<string, Transform>();
    private FTUESequence _activeSequence;
    private int _stepIndex;
    private Coroutine _activeStepRoutine;
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
    }

    private void Update()
    {
        // This allows you to slide the Offset values in the Inspector 
        // and see the Tooltip/Hand move instantly during Play Mode.
        if (debugUpdateOffsets && IsTutorialActive() && _activeSequence != null)
        {
            var step = _activeSequence.steps[_stepIndex];
            if (step.ShowCutout)
            {
                PositionSmartPointer(_currentTargetScreenPos, _currentDirection);
                PositionSmartTooltip(_currentTargetScreenPos, step.Message, _currentDirection);
            }
        }
    }

    private IEnumerator ExecuteStepRoutine()
    {
        FTUEStep step = _activeSequence.steps[_stepIndex];
        _waitingForSignal = !string.IsNullOrEmpty(step.RequiredEvent);

        if (step.ShowCutout)
        {
            while (!_registeredTargets.ContainsKey(step.TargetID)) yield return null;
            Transform target = _registeredTargets[step.TargetID];

            maskScript.SetState(target, true, step.CustomSize > 0 ? step.CustomSize : 0.15f);

            _currentTargetScreenPos = GetTargetScreenPoint(target);
            _currentDirection = step.HandDirection;

            PositionSmartPointer(_currentTargetScreenPos, _currentDirection);
            PositionSmartTooltip(_currentTargetScreenPos, step.Message, _currentDirection);
        }
        else
        {
            maskScript.SetState(null, false);
            PositionNarrativeTooltip(step.Message);
            pointerHand.gameObject.SetActive(false);
        }
    }

    private void PositionSmartTooltip(Vector2 targetPos, string message, PointerDirection handDir)
    {
        tooltipContainer.gameObject.SetActive(true);
        tooltipText.text = message;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipContainer);

        // Calculate dynamic side-switching
        Vector2 finalPos = targetPos;

        // If in bottom half, push up. Top half, push down.
        float yFlip = (targetPos.y < Screen.height * 0.5f) ? 1 : -1;
        float xFlip = (targetPos.x < Screen.width * 0.5f) ? 1 : -1;

        // Apply Inspector Offsets
        finalPos.x += tooltipOffset.x * xFlip;
        finalPos.y += tooltipOffset.y * yFlip;

        // Screen Boundary Clamping
        float halfWidth = (tooltipContainer.rect.width / 2) + screenPadding;
        float halfHeight = (tooltipContainer.rect.height / 2) + screenPadding;

        finalPos.x = Mathf.Clamp(finalPos.x, halfWidth, Screen.width - halfWidth);
        finalPos.y = Mathf.Clamp(finalPos.y, halfHeight, Screen.height - halfHeight);

        tooltipContainer.position = finalPos;
    }

    private void PositionSmartPointer(Vector2 targetPos, PointerDirection dir)
    {
        if (dir == PointerDirection.None) { pointerHand.gameObject.SetActive(false); return; }
        pointerHand.gameObject.SetActive(true);

        Vector2 dirVec = GetDirectionVector(dir);
        float angle = Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg + 90f;
        pointerHand.localEulerAngles = new Vector3(0, 0, angle);

        // Use the handOffset from inspector
        pointerHand.position = targetPos + (dirVec * handOffset.magnitude);

        // Restart animation with new base position
        if (!debugUpdateOffsets)
        {
            StopCoroutine(_pointerCoroutine);
            _pointerCoroutine = StartCoroutine(AnimatePointer(dirVec));
        }
    }

    private Coroutine _pointerCoroutine;

    private IEnumerator AnimatePointer(Vector2 dir)
    {
        while (true)
        {
            // Note: We don't use a cached basePos here if we want to live-tune 
            // because the base center might be changing in the inspector.
            Vector2 dirVec = GetDirectionVector(_currentDirection);
            Vector3 centerPos = (Vector3)_currentTargetScreenPos + (Vector3)(dirVec * handOffset.magnitude);

            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceDistance;
            pointerHand.position = centerPos + (Vector3)(dirVec * bounce);
            yield return null;
        }
    }
    // --- ESSENTIAL GATEKEEPERS (RESTORED) ---

    public bool IsTutorialActive() => ftueCanvas != null && ftueCanvas.enabled;

    public bool IsSequenceCompleted(string sequenceID)
    {
        if (GameManager.Instance?.SaveData?.CompletedFTUESequences == null) return false;
        return GameManager.Instance.SaveData.CompletedFTUESequences.Contains(sequenceID);
    }

    public bool IsCurrentTarget(string id)
    {
        if (_activeSequence == null || _stepIndex < 0 || _stepIndex >= _activeSequence.steps.Count)
            return false;
        return _activeSequence.steps[_stepIndex].TargetID == id;
    }

    public bool CurrentStepRequiresClick()
    {
        if (_activeSequence == null || _stepIndex >= _activeSequence.steps.Count) return false;
        return _activeSequence.steps[_stepIndex].RequireClick;
    }

    // --- LOGIC ---

    public void Register(string id, Transform tr) => _registeredTargets[id] = tr;
    public void Unregister(string id) => _registeredTargets.Remove(id);

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

    // private IEnumerator ExecuteStepRoutine()
    // {
    //     FTUEStep step = _activeSequence.steps[_stepIndex];
    //     _waitingForSignal = !string.IsNullOrEmpty(step.RequiredEvent);

    //     if (step.ShowCutout)
    //     {
    //         while (!_registeredTargets.ContainsKey(step.TargetID)) yield return null;
    //         Transform target = _registeredTargets[step.TargetID];

    //         // Tell the Mask Script to handle the visuals
    //         maskScript.SetState(target, true, step.CustomSize > 0 ? step.CustomSize : 0.15f);

    //         _currentTargetScreenPos = GetTargetScreenPoint(target);
    //         _currentDirection = step.HandDirection;
    //         PositionSmartPointer(_currentTargetScreenPos, _currentDirection); // Do pointer first so padding is calculated
    //         PositionSmartTooltip(_currentTargetScreenPos, step.Message, _currentDirection);
    //     }
    //     else
    //     {
    //         // Instead of maskImage.gameObject.SetActive(false), 
    //         // we keep it active but tell the shader to be invisible.
    //         maskScript.SetState(null, false);

    //         // This ensures the Image component is still there to catch 
    //         // the "Advance" click even if the shader looks like it's gone.
    //         maskScript.enabled = true;

    //         PositionNarrativeTooltip(step.Message);
    //         pointerHand.gameObject.SetActive(false);
    //     }
    // }

    // // --- UI SMART POSITIONING ---

    // private void PositionSmartTooltip(Vector2 targetPos, string message, PointerDirection handDir)
    // {
    //     tooltipContainer.gameObject.SetActive(true);
    //     tooltipText.text = message;

    //     // 1. Let the text resize and container expand
    //     LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipContainer);

    //     // 2. Calculate dynamic offset to avoid the cutout and the hand
    //     // We move the tooltip further away if there is a hand in the way
    //     float handPadding = (handDir != PointerDirection.None) ? handOffset.magnitude + bounceDistance : 0f;
    //     float verticalBuffer = 100f + handPadding;
    //     float horizontalBuffer = 150f + handPadding;

    //     Vector2 finalPos = targetPos;

    //     // 3. QUADRANT LOGIC: Flip position based on screen half-points
    //     // If target is in bottom half, put tooltip above. If top half, put below.
    //     if (targetPos.y < Screen.height * 0.5f)
    //         finalPos.y += verticalBuffer;
    //     else
    //         finalPos.y -= verticalBuffer;

    //     // If target is in left half, nudge right. If right half, nudge left.
    //     if (targetPos.x < Screen.width * 0.5f)
    //         finalPos.x += (targetPos.x < screenPadding) ? horizontalBuffer : 0;
    //     else
    //         finalPos.x -= (targetPos.x > Screen.width - screenPadding) ? horizontalBuffer : 0;

    //     // 4. SCREEN BOUNDARY CLAMPING
    //     // Get world-space corners of the tooltip to ensure it's fully on screen
    //     float halfWidth = (tooltipContainer.rect.width / 2) + screenPadding;
    //     float halfHeight = (tooltipContainer.rect.height / 2) + screenPadding;

    //     finalPos.x = Mathf.Clamp(finalPos.x, halfWidth, Screen.width - halfWidth);
    //     finalPos.y = Mathf.Clamp(finalPos.y, halfHeight, Screen.height - halfHeight);

    //     tooltipContainer.position = finalPos;
    // }

    // private void PositionSmartPointer(Vector2 targetPos, PointerDirection dir)
    // {
    //     if (dir == PointerDirection.None) { pointerHand.gameObject.SetActive(false); return; }
    //     pointerHand.gameObject.SetActive(true);

    //     Vector2 dirVec = GetDirectionVector(dir);
    //     float angle = Mathf.Atan2(dirVec.y, dirVec.x) * Mathf.Rad2Deg + 90f;
    //     pointerHand.localEulerAngles = new Vector3(0, 0, angle);
    //     pointerHand.position = targetPos + (dirVec * handOffset.magnitude);

    //     StopCoroutine("AnimatePointer");
    //     StartCoroutine("AnimatePointer", dirVec);
    // }

    // private IEnumerator AnimatePointer(Vector2 dir)
    // {
    //     Vector3 basePos = pointerHand.position;
    //     while (true)
    //     {
    //         float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceDistance;
    //         pointerHand.position = basePos + (Vector3)(dir * bounce);
    //         yield return null;
    //     }
    // }

    private void PositionNarrativeTooltip(string message)
    {
        tooltipContainer.gameObject.SetActive(true);
        tooltipText.text = message;
        tooltipContainer.position = new Vector2(Screen.width / 2, Screen.height * 0.2f);
    }

    public void Advance()
    {
        if (_waitingForSignal) return; // Logic lock

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
        StopAllCoroutines();
    }

    private Vector2 GetTargetScreenPoint(Transform target)
    {
        if (target is RectTransform rect) return RectTransformUtility.WorldToScreenPoint(null, rect.position);
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

}