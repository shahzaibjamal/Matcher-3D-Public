using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance { get; private set; }

    [Header("Data & Database")]
    [SerializeField] private FTUEDatabase database;

    [Header("UI Canvas References")]
    [SerializeField] private Canvas ftueCanvas;
    [SerializeField] private Image maskImage;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private RectTransform pointerHand;

    [Header("Settings")]
    [SerializeField] private float handOffset = 120f;
    [SerializeField] private float bounceSpeed = 8f;
    [SerializeField] private float bounceDistance = 20f;

    private Dictionary<string, RectTransform> _registeredTargets = new Dictionary<string, RectTransform>();
    private FTUESequence _activeSequence;
    private int _stepIndex;
    private Material _maskMat;
    private Coroutine _activeStepRoutine;
    private Coroutine _pointerRoutine;

    // Shader Property IDs
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int SizeID = Shader.PropertyToID("_Size");
    private static readonly int AlphaScaleID = Shader.PropertyToID("_AlphaScale"); // New: Controls hole visibility
    private bool _waitingForSignal;

    void OnEnable()
    {
        FTUEEvents.OnSignal += HandleSignal;
    }

    void OnDisable()
    {
        FTUEEvents.OnSignal -= HandleSignal;
    }

    private void HandleSignal(string signalName)
    {
        if (!_waitingForSignal || _activeSequence == null) return;

        // Check if this signal matches the requirement of the current step
        if (_activeSequence.steps[_stepIndex].RequiredEvent == signalName)
        {
            _waitingForSignal = false; // Unlock
            Advance();

        }
    }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _maskMat = Instantiate(maskImage.material);
        maskImage.material = _maskMat;

        ftueCanvas.enabled = false;
        tooltip.SetActive(false);
        pointerHand.gameObject.SetActive(false);
    }

    public void Register(string id, RectTransform rect) => _registeredTargets[id] = rect;
    public void Unregister(string id) => _registeredTargets.Remove(id);

    public void PlayTutorial(string sequenceID)
    {
        _activeSequence = database.GetByID(sequenceID);
        if (_activeSequence == null) return;

        if (PlayerPrefs.HasKey("FTUE_COMPLETE_" + _activeSequence.name))
        {
            Debug.Log($"Sequence {_activeSequence.name} already finished. Skipping.");
            return;
        }
        _stepIndex = 0;
        ftueCanvas.enabled = true;

        if (_activeStepRoutine != null) StopCoroutine(_activeStepRoutine);
        _activeStepRoutine = StartCoroutine(ExecuteStep());
    }

    private IEnumerator ExecuteStep()
    {
        FTUEStep step = _activeSequence.steps[_stepIndex];
        _waitingForSignal = !string.IsNullOrEmpty(step.RequiredEvent);
        // Hide everything while we prepare
        tooltip.SetActive(false);
        pointerHand.gameObject.SetActive(false);

        if (step.ShowCutout)
        {
            // 1. FOCUS MODE (Dark screen + Hole)
            while (!_registeredTargets.ContainsKey(step.TargetID)) yield return null;

            RectTransform target = _registeredTargets[step.TargetID];
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

            _maskMat.SetFloat(AlphaScaleID, 1f); // Make the dark overlay visible
            _maskMat.SetVector(CenterID, new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height));
            _maskMat.SetFloat(SizeID, step.CustomSize > 0 ? step.CustomSize : 0.15f);

            UpdatePointer(target, step.ShowHand ? step.HandDirection : PointerDirection.None);
            maskImage.GetComponent<FTUEMask>().SetState(target, true);
            maskImage.gameObject.SetActive(true);
            tooltip.transform.position = (Vector3)screenPos + new Vector3(0, 150, 0);
        }
        else
        {
            maskImage.gameObject.SetActive(false);
            // 2. NARRATIVE / INVISIBLE MODE
            // The screen stays 100% clear. No dark overlay.
            _maskMat.SetFloat(AlphaScaleID, 0f);

            // Disable the "Hole" logic in the mask so clicks land anywhere
            maskImage.GetComponent<FTUEMask>().SetState(null, false);

            // Position tooltip in a standard "Dialogue" spot (e.g., bottom-center)
            tooltip.transform.position = new Vector2(Screen.width / 2, 200);
        }

        tooltip.SetActive(true);
        // tooltipText.text = step.message;
    }
    private void UpdatePointer(RectTransform target, PointerDirection direction)
    {
        if (direction == PointerDirection.None) return;

        pointerHand.gameObject.SetActive(true);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
        Vector2 offsetDir = Vector2.zero;

        switch (direction)
        {
            case PointerDirection.Top: offsetDir = Vector2.up; pointerHand.localEulerAngles = new Vector3(0, 0, 180); break;
            case PointerDirection.Bottom: offsetDir = Vector2.down; pointerHand.localEulerAngles = new Vector3(0, 0, 0); break;
            case PointerDirection.Left: offsetDir = Vector2.left; pointerHand.localEulerAngles = new Vector3(0, 0, -90); break;
            case PointerDirection.Right: offsetDir = Vector2.right; pointerHand.localEulerAngles = new Vector3(0, 0, 90); break;
        }

        if (_pointerRoutine != null) StopCoroutine(_pointerRoutine);
        _pointerRoutine = StartCoroutine(AnimatePointer((Vector3)screenPos + (Vector3)(offsetDir * handOffset), offsetDir));
    }

    private IEnumerator AnimatePointer(Vector3 basePos, Vector2 dir)
    {
        while (true)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceDistance;
            pointerHand.position = basePos + (Vector3)(dir * bounce);
            yield return null;
        }
    }

    public void Advance()
    {
        if (_waitingForSignal) return;
        _stepIndex++;
        if (_stepIndex < _activeSequence.steps.Count)
        {
            if (_activeStepRoutine != null) StopCoroutine(_activeStepRoutine);
            _activeStepRoutine = StartCoroutine(ExecuteStep());
        }
        else
        {
            EndTutorial();
        }
    }

    public bool CurrentStepRequiresClick()
    {
        if (_activeSequence == null) return false;
        return _activeSequence.steps[_stepIndex].RequireClick;
    }

    public void EndTutorial()
    {
        if (_activeSequence != null)
        {
            // Mark this sequence as finished forever
            PlayerPrefs.SetInt("FTUE_COMPLETE_" + _activeSequence.name, 1);
            PlayerPrefs.Save();
        }
        ftueCanvas.enabled = false;
        _activeSequence = null;
        if (_pointerRoutine != null) StopCoroutine(_pointerRoutine);
    }

    public bool IsCurrentTarget(string id)
    {
        if (_activeSequence == null || _stepIndex < 0 || _stepIndex >= _activeSequence.steps.Count)
            return false;

        return _activeSequence.steps[_stepIndex].TargetID == id;
    }
}