using UnityEngine;
using UnityEngine.EventSystems;

public class FTUEMask : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
{
    private RectTransform _targetRect;
    private bool _isCutoutActive;

    /// <summary>
    /// Configures whether the mask should allow clicks through a specific hole.
    /// </summary>
    public void SetState(RectTransform target, bool useCutout)
    {
        _targetRect = target;
        _isCutoutActive = useCutout;
    }

    /// <summary>
    /// Unity's internal check to see if a click "hits" this UI element.
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 1. If we aren't using a cutout (Narrative Mode), the whole screen is a wall.
        // Returning true means the click hits the mask and is blocked.

        if (!_isCutoutActive || _targetRect == null) return true;

        // 2. If we ARE using a cutout, check if the click is inside the button's area.
        bool isInsideHole = RectTransformUtility.RectangleContainsScreenPoint(_targetRect, sp, eventCamera);

        // 3. Logic Flip: 
        // If the click is INSIDE the hole, we return FALSE so the raycast passes through to the button.
        // If the click is OUTSIDE the hole, we return TRUE so the mask blocks it.
        return !isInsideHole;
    }

    /// <summary>
    /// Detects clicks on the dark area of the mask.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // If the current step allows "Click anywhere to continue" (requireClick = false),
        // then clicking the dark part of the mask will advance the tutorial.
        Debug.LogError("Clicked");
        if (!FTUEManager.Instance.CurrentStepRequiresClick())
        {
            FTUEManager.Instance.Advance();
        }
    }
}