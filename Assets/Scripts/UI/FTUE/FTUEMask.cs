using UnityEngine;
using UnityEngine.EventSystems;

public class FTUEMask : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
{
    private Transform _target; // Changed from RectTransform to Transform
    private bool _isCutoutActive;
    private float _customRadius = 0.15f; // Should match your shader size

    /// <summary>
    /// Configures whether the mask should allow clicks through a specific hole.
    /// </summary>
    public void SetState(Transform target, bool useCutout, float size = 0.15f)
    {
        _target = target;
        _isCutoutActive = useCutout;
        _customRadius = size;
    }

    /// <summary>
    /// Unity's internal check to see if a click "hits" this UI element.
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // 1. Narrative Mode / No Target: Block everything
        if (!_isCutoutActive || _target == null) return true;

        bool isInsideHole = false;

        // 2. Logic for UI Elements
        if (_target is RectTransform rect)
        {
            isInsideHole = RectTransformUtility.RectangleContainsScreenPoint(rect, sp, eventCamera);
        }
        // 3. Logic for 3D Objects
        else
        {
            // Convert 3D target to Screen Space
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(_target.position);

            // Convert click pixel coordinates to Normalized (0-1) to match shader logic
            Vector2 normalizedClick = new Vector2(sp.x / Screen.width, sp.y / Screen.height);
            Vector2 normalizedTarget = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);

            // Check if distance between click and target is within the "Hole" radius
            // We adjust for Aspect Ratio to keep the hole circular
            float aspect = (float)Screen.width / Screen.height;
            Vector2 diff = normalizedClick - normalizedTarget;
            diff.x *= aspect;

            isInsideHole = diff.magnitude < _customRadius;
        }

        // Return FALSE (pass through) if inside, TRUE (block) if outside
        return !isInsideHole;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!FTUEManager.Instance.CurrentStepRequiresClick())
        {
            FTUEManager.Instance.Advance();
        }
    }
}