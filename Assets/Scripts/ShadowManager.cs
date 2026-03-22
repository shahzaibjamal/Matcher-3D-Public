using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    public static ShadowManager Instance;

    private class ShadowPair
    {
        public Transform Target;
        public GameObject ShadowObj;
        public Transform ShadowTransform; // Cached for speed
        public Renderer ShadowRenderer;   // For instancing
        public Vector3 InitialScale;
    }

    private List<ShadowPair> _activeShadows = new List<ShadowPair>();
    private MaterialPropertyBlock _propBlock;

    [SerializeField] private float yOffset = 0.01f;
    [SerializeField] private LayerMask groundLayer;
    [Range(0f, 1f)][SerializeField] private float minScalePercent = 0.3f;
    [SerializeField] private float maxDistance = 5f;

    [Header("Light Control")]
    [Range(0f, 1f)][SerializeField] private float lightInfluence = 0.5f;
    [Range(0f, 1f)][SerializeField] private float maxShadowAlpha = 0.5f;
    private Light _sun;

    void Awake()
    {
        Instance = this;
        _propBlock = new MaterialPropertyBlock();
        _sun = RenderSettings.sun;
        if (_sun == null) _sun = FindFirstObjectByType<Light>();
    }

    public void RegisterShadow(Transform target)
    {
        AssetLoader.Instance.InstantiatePrefab("ShadowBlob", go =>
        {
            _activeShadows.Add(new ShadowPair
            {
                Target = target,
                ShadowObj = go,
                ShadowTransform = go.transform,
                ShadowRenderer = go.GetComponent<Renderer>(),
                InitialScale = go.transform.localScale
            });
        });
    }

    public void UnregisterShadow(Transform target)
    {
        int index = _activeShadows.FindIndex(s => s.Target == target);
        if (index != -1)
        {
            AssetLoader.Instance.ReleaseInstance(_activeShadows[index].ShadowObj);
            _activeShadows.RemoveAt(index);
        }
    }

    void LateUpdate()
    {
        if (_activeShadows.Count == 0) return;

        // Cache the light direction once per frame instead of inside the loop
        Vector3 rawLightDir = _sun != null ? _sun.transform.forward : Vector3.down;
        Vector3 biasedDir = Vector3.Lerp(Vector3.down, rawLightDir, lightInfluence).normalized;

        for (int i = _activeShadows.Count - 1; i >= 0; i--)
        {
            var pair = _activeShadows[i];

            if (pair.Target == null)
            {
                AssetLoader.Instance.ReleaseInstance(pair.ShadowObj);
                _activeShadows.RemoveAt(i);
                continue;
            }

            UpdateSingleShadow(pair, biasedDir);
        }
    }

    private void UpdateSingleShadow(ShadowPair pair, Vector3 biasedDir)
    {
        Vector3 rayOrigin = pair.Target.position + Vector3.up * 1.0f;

        if (Physics.Raycast(rayOrigin, biasedDir, out RaycastHit hit, maxDistance + 1.0f, groundLayer))
        {
            // FIX: Use pair.ShadowTransform instead of 'transform'
            pair.ShadowTransform.position = hit.point + Vector3.up * yOffset;

            Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            pair.ShadowTransform.rotation = groundRotation * Quaternion.Euler(90f, 0f, 0f);

            float verticalDist = Mathf.Max(0, (pair.Target.position.y - hit.point.y));
            float t = Mathf.Clamp01(verticalDist / maxDistance);
            float scaleMultiplier = Mathf.Lerp(1f, minScalePercent, t);

            pair.ShadowTransform.localScale = pair.InitialScale * scaleMultiplier;

            float alpha = Mathf.Lerp(0f, maxShadowAlpha, scaleMultiplier);
            pair.ShadowRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", new Color(0, 0, 0, alpha));
            pair.ShadowRenderer.SetPropertyBlock(_propBlock);
        }
        else
        {
            pair.ShadowTransform.localScale = Vector3.zero;
        }
    }
}