using UnityEngine;
using UnityEngine.EventSystems;

// IPointerClickHandler allows this script to detect clicks directly
public class FTUETarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string targetID;

    void OnEnable()
    {
        if (FTUEManager.Instance != null)
            FTUEManager.Instance.Register(targetID, GetComponent<RectTransform>());
    }

    void OnDisable()
    {
        if (FTUEManager.Instance != null)
            FTUEManager.Instance.Unregister(targetID);
    }

    // This fires when the user clicks the element (even if it's a Button)
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if this is the target the Manager is currently looking for
        if (FTUEManager.Instance != null && FTUEManager.Instance.IsCurrentTarget(targetID))
        {
            // Only advance if the current step requires a specific click
            if (FTUEManager.Instance.CurrentStepRequiresClick())
            {
                FTUEManager.Instance.Advance();
            }
        }
    }
}