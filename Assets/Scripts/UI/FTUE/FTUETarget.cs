using UnityEngine;
using UnityEngine.EventSystems; // Required for UI compatibility

public class FTUETarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string targetID;

    public string TargetID
    {
        get { return targetID; }
        private set { targetID = value; }
    }
    // --- 1. THE SHARED LOGIC ---
    // This is the single "Gatekeeper" for advancing the tutorial
    public void OnObjectClicked()
    {
        if (FTUEManager.Instance != null && FTUEManager.Instance.IsCurrentTarget(targetID))
        {
            if (FTUEManager.Instance.CurrentStepRequiresClick())
            {
                FTUEManager.Instance.Advance();
            }
        }
    }

    // --- 2. THE UI BRIDGE ---
    // Automatically called by Unity when a UI element (with a Graphic component) is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // Simply redirect to the shared logic
        OnObjectClicked();
    }

    // --- 3. REGISTRATION ---
    // This ensures the FTUEManager knows where this object is (2D or 3D)
    private void OnEnable()
    {
        if (FTUEManager.Instance != null && !string.IsNullOrEmpty(targetID))
            FTUEManager.Instance.Register(targetID, transform);
    }

    private void OnDisable()
    {
        if (FTUEManager.Instance != null)
            FTUEManager.Instance.Unregister(targetID);
    }

    public void Init(string id)
    {
        targetID = id;
        // Re-register if the ID is assigned dynamically
        FTUEManager.Instance?.Register(targetID, transform);
    }
}