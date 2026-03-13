using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TMP_Text tooltip;
    [SerializeField] private RectTransform pointerHand;

    [Header("Settings")]
    [SerializeField] private float handOffset = 120f;
    [SerializeField] private float bounceSpeed = 8f;
    [SerializeField] private float bounceDistance = 20f;

    private Dictionary<string, Transform> _registeredTargets = new Dictionary<string, Transform>();
    private FTUESequence _activeSequence;
    private int _stepIndex;
    private Material _maskMat;
    private Coroutine _activeStepRoutine;
    private Coroutine _pointerRoutine;

    // Shader Property IDs
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int SizeID = Shader.PropertyToID("_Size");
    private static readonly int AspectID = Shader.PropertyToID("_Aspect");
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
        tooltip.gameObject.SetActive(false);
        pointerHand.gameObject.SetActive(false);
        _maskMat.SetFloat(AspectID, (float)Screen.width / (float)Screen.height);

    }

    public void Register(string id, Transform rect) => _registeredTargets[id] = rect;
    public void Unregister(string id) => _registeredTargets.Remove(id);

    public void PlayTutorial(string sequenceID)
    {
        _activeSequence = database.GetByID(sequenceID);
        if (_activeSequence == null) return;

        // Direct List Check
        if (IsSequenceCompleted(_activeSequence.name))
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
        tooltip.gameObject.SetActive(false);
        pointerHand.gameObject.SetActive(false);

        if (step.ShowCutout)
        {
            // Wait for target to be registered (important if objects are spawned dynamically)
            while (!_registeredTargets.ContainsKey(step.TargetID)) yield return null;

            Transform target = _registeredTargets[step.TargetID];
            Vector2 screenPos;

            // Determine if we are looking at a UI element or a 3D World Object
            if (target is RectTransform rect)
            {
                // UI: Use the specialized utility for RectTransforms
                screenPos = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            }
            else
            {
                // 3D: Project the world position to the screen pixel coordinates
                screenPos = Camera.main.WorldToScreenPoint(target.position);
            }

            // 1. Update Mask Material
            _maskMat.SetFloat(AlphaScaleID, 1f); // Ensure overlay is visible
            _maskMat.SetVector(CenterID, new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height));
            _maskMat.SetFloat(SizeID, step.CustomSize > 0 ? step.CustomSize : 0.15f);

            // 2. Update Pointer
            UpdatePointer(screenPos, step.ShowHand ? step.HandDirection : PointerDirection.None);

            // 3. Update Logical Mask (for blocking clicks)
            // We pass the Transform; the FTUEMask script should be updated to handle non-rects too
            maskImage.GetComponent<FTUEMask>().SetState(target, true);
            maskImage.gameObject.SetActive(true);

            // 4. Position Tooltip
            tooltip.transform.position = (Vector3)screenPos + new Vector3(0, 150, 0);
        }
        else
        {
            // NARRATIVE MODE
            maskImage.gameObject.SetActive(false);
            _maskMat.SetFloat(AlphaScaleID, 0f);
            maskImage.GetComponent<FTUEMask>().SetState(null, false);

            // Fixed dialogue position at the bottom of the screen
            tooltip.transform.position = new Vector2(Screen.width / 2, 200);
        }

        tooltip.gameObject.SetActive(true);
        // If you have a text component, update it here:
        tooltip.text = step.Message;
    }

    // Change this line
    private void UpdatePointer(Vector2 screenPos, PointerDirection direction)
    {
        if (direction == PointerDirection.None)
        {
            pointerHand.gameObject.SetActive(false);
            return;
        }

        pointerHand.gameObject.SetActive(true);

        // REMOVE the line that re-calculates screenPos:
        // Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

        Vector2 offsetDir = Vector2.zero;

        switch (direction)
        {
            case PointerDirection.Top: offsetDir = Vector2.up; pointerHand.localEulerAngles = new Vector3(0, 0, 180); break;
            case PointerDirection.Bottom: offsetDir = Vector2.down; pointerHand.localEulerAngles = new Vector3(0, 0, 0); break;
            case PointerDirection.Left: offsetDir = Vector2.left; pointerHand.localEulerAngles = new Vector3(0, 0, -90); break;
            case PointerDirection.Right: offsetDir = Vector2.right; pointerHand.localEulerAngles = new Vector3(0, 0, 90); break;
        }

        if (_pointerRoutine != null) StopCoroutine(_pointerRoutine);
        // Use the screenPos passed in from ExecuteStep
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
            string seqName = _activeSequence.name;
            if (!IsSequenceCompleted(seqName))
            {
                GameManager.Instance.SaveData.CompletedFTUESequences.Add(seqName);

                // Trigger your save logic here if necessary
                GameManager.Instance.SaveGame();
            }
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
    /// <summary>
    /// Checks if a specific sequence has ever been completed.
    /// </summary>
    public bool IsSequenceCompleted(string sequenceID)
    {
        // We use the same string format as in EndTutorial and PlayTutorial
        if (GameManager.Instance?.SaveData?.CompletedFTUESequences == null) return false;

        // Simple List lookup
        return GameManager.Instance.SaveData.CompletedFTUESequences.Contains(sequenceID);
    }

    /// <summary>
    /// Returns true if there is an active tutorial currently appearing on screen.
    /// </summary>
    public bool IsTutorialActive()
    {
        return ftueCanvas != null && ftueCanvas.enabled;
    }
}