using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;

public class FTUEMask : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler
{
    private Material _maskMat;
    private Transform _target;
    private bool _isCutoutActive;

    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 0.6f;
    [SerializeField] private float _startSizeMultiplier = 3.0f;
    // Set a valid default preset here
    [SerializeField] private AnimationCurve _shrinkCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private float _currentRadius;
    private float _finalRadius = 0.15f;

    [Tooltip("1,0 pins left. -1,0 pins right. 0,1 pins bottom.")]
    [SerializeField] private Vector2 _pinDirection = new Vector2(1, 0);
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int SizeID = Shader.PropertyToID("_Size");
    private static readonly int AspectID = Shader.PropertyToID("_Aspect");

    private void Awake() => _maskMat = GetComponent<Image>().material;

    // Inside FTUEMask.cs
    public void SetState(Transform target, bool useCutout, float size = 0.15f)
    {
        _target = target;
        _isCutoutActive = useCutout;
        _finalRadius = size;

        if (_isCutoutActive && _target != null)
        {
            // Set initial state or trigger your animation logic here
            StopAllCoroutines();
            StartCoroutine(AnimatePinnedHole()); // Using your previous logic
        }
        else
        {
            _maskMat.SetFloat(SizeID, 0); // Hide hole
        }
    }

    private IEnumerator AnimatePinnedHole()
    {
        float elapsed = 0f;
        float startRadius = _finalRadius * _startSizeMultiplier;

        while (elapsed < _animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = _shrinkCurve.Evaluate(elapsed / _animationDuration);
            _currentRadius = Mathf.Lerp(startRadius, _finalRadius, t);

            UpdateShader(_currentRadius);
            yield return null;
        }
        _currentRadius = _finalRadius;
    }

    private void Update()
    {
        if (_isCutoutActive && _target != null) UpdateShader(_currentRadius);
    }

    private void UpdateShader(float animatedRadius)
    {
        if (_maskMat == null || _target == null) return;

        float aspect = (float)Screen.width / Screen.height;
        Vector2 uvTarget = GetTargetUV(); // 0 to 1 range
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        // 1. Direction from Target to Screen Center
        // This "pulls" the large circle's center toward the middle of the screen.
        Vector2 shiftDir = (screenCenter - uvTarget).normalized;

        // 2. The distance the center needs to travel
        // When animatedRadius == _finalRadius, offset is 0 (Center is exactly on Target).
        float offsetMagnitude = animatedRadius - _finalRadius;

        // 3. Apply the shift
        Vector2 offset = shiftDir * offsetMagnitude;

        // Correct for the Shader's Aspect multiplication on the X axis
        offset.x /= aspect;

        Vector2 shiftedCenter = uvTarget + offset;

        // 4. Update Shader
        _maskMat.SetVector(CenterID, shiftedCenter);
        _maskMat.SetFloat(AspectID, aspect);
        _maskMat.SetFloat(SizeID, animatedRadius);
    }
    private Vector2 GetTargetUV()
    {
        Vector2 screenPos = GetTargetScreenPoint();
        return new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
    }


    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // If the typewriter is talking, the mask returns TRUE for the whole screen.
        // This means the mask 'eats' the click and nothing underneath gets it.
        if (FTUEManager.Instance != null && FTUEManager.Instance.IsTyping)
        {
            return true;
        }
        // 1. If no click is required on a target, the whole screen should be 
        // "Valid" for the Mask so that OnPointerClick can trigger Advance().
        if (FTUEManager.Instance != null && !FTUEManager.Instance.CurrentStepRequiresClick())
        {
            return true;
        }

        // 2. If no cutout is active, the mask is effectively invisible/disabled
        if (!_isCutoutActive || _target == null) return true;

        // 3. Coordinate Conversion
        float aspect = (float)Screen.width / Screen.height;
        Vector2 uvClick = new Vector2(sp.x / Screen.width, sp.y / Screen.height);
        Vector2 uvTarget = GetTargetUV();
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        // 4. Calculate the Shift Toward Screen Center
        // This MUST match the UpdateShader logic exactly
        Vector2 shiftDir = (screenCenter - uvTarget).normalized;
        float offsetMagnitude = _currentRadius - _finalRadius;
        Vector2 offset = shiftDir * offsetMagnitude;
        offset.x /= aspect;

        Vector2 shiftedCenter = uvTarget + offset;

        // 5. Distance Check (Aspect Corrected)
        Vector2 diff = uvClick - shiftedCenter;
        diff.x *= aspect;

        // Return TRUE (Click hits Mask) if outside radius
        // Return FALSE (Click passes through to Item) if inside radius
        return diff.magnitude > _currentRadius;
    }

    private Vector2 GetTargetScreenPoint()
    {
        if (_target == null) return Vector2.zero;

        Vector3 worldCenter;

        // 1. UI ELEMENT (RectTransform)
        if (_target is RectTransform rect)
        {
            // Get the center of the local rectangle in world space
            // This ignores the pivot (0,0 or 1,1) and finds the true geometric middle
            Vector3 localCenter = rect.rect.center;
            worldCenter = rect.TransformPoint(localCenter);
        }
        // 2. 3D OBJECT (Renderer Bounds)
        else if (_target.TryGetComponent<Renderer>(out var renderer))
        {
            // Use the center of the bounding box (includes all child meshes if using a prefab)
            worldCenter = renderer.bounds.center;
        }
        // 3. FALLBACK (Transform Pivot)
        else
        {
            worldCenter = _target.position;
        }

        // Convert the calculated world center to screen pixel coordinates
        if (_target is RectTransform)
        {
            // For UI, null camera assumes Screen Space Overlay
            return RectTransformUtility.WorldToScreenPoint(null, worldCenter);
        }

        return Camera.main.WorldToScreenPoint(worldCenter);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // If the step is narrative (doesn't require a specific item click), 
        // any click on the mask (which is now the whole screen) advances the tutorial.
        if (FTUEManager.Instance != null && !FTUEManager.Instance.CurrentStepRequiresClick())
        {
            FTUEManager.Instance.Advance();
        }
    }
}