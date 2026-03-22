using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    public Transform TargetObject;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float yOffset = 0.01f;

    [Header("Light Control")]
    [Range(0f, 1f)]
    [SerializeField] private float lightInfluence = 0.5f; // 0 = Always straight down, 1 = Full light angle

    [Header("Scaling")]
    [SerializeField] private float maxDistance = 5f;
    [Range(0f, 1f)][SerializeField] private float minScalePercent = 0.3f;

    private Light _sun;
    private Vector3 _initialScale;

    private void Start()
    {
        _sun = RenderSettings.sun;
        if (_sun == null) _sun = FindFirstObjectByType<Light>();
        _initialScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (TargetObject == null)
        {
            Destroy(gameObject);
            return;
        }

        // 1. CALCULATE BIASED DIRECTION
        // We blend the real light direction with a "Straight Down" vector
        Vector3 rawLightDir = _sun != null ? _sun.transform.forward : Vector3.down;
        Vector3 biasedDir = Vector3.Lerp(Vector3.down, rawLightDir, lightInfluence).normalized;

        // 2. RAYCAST
        Vector3 rayOrigin = TargetObject.position + Vector3.up * 1.0f;

        if (Physics.Raycast(rayOrigin, biasedDir, out RaycastHit hit, maxDistance + 1.0f, groundLayer))
        {
            // Position and Rotation
            transform.position = hit.point + Vector3.up * yOffset;
            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            transform.rotation = groundRotation * Quaternion.Euler(90f, 0f, 0f);

            // 3. SCALE (Based on the vertical height, not the diagonal ray length)
            float verticalDist = Mathf.Max(0, (TargetObject.position.y - hit.point.y));
            float t = Mathf.Clamp01(verticalDist / maxDistance);
            transform.localScale = _initialScale * Mathf.Lerp(1f, minScalePercent, t);
        }
        else
        {
            transform.localScale = Vector3.zero;
        }
    }
}