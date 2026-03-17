using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PerformanceDebugMenu : MonoBehaviour
{
    public static bool IsMouseOverMenu { get; private set; }
    private Light _dirLight;
    private UniversalAdditionalCameraData _cameraData;
    private UniversalRenderPipelineAsset _urpAsset;

    private bool _showMenu = false;
    private Vector2 _scrollPosition = Vector2.zero;

    [SerializeField] private UniversalRendererData rendererData;

    private void Start()
    {
        _dirLight = RenderSettings.sun;
        if (Camera.main != null)
            _cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

        _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    }
    private void Update()
    {
        // Update the 'IsMouseOverMenu' state
        if (!_showMenu)
        {
            IsMouseOverMenu = false;
            return;
        }

        // OnGUI uses top-left (0,0), Input uses bottom-left (0,0)
        // We convert the touch/mouse position to check if it's in the menu
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y; // Flip Y for GUI coordinates

        // If the menu is full screen, we check if it's active. 
        // If it's a partial box, we check if mousePos is inside _menuRect.
        IsMouseOverMenu = _showMenu;
    }

    private void OnGUI()
    {
        // 1. GLOBAL STYLING
        GUI.skin.button.fontSize = 35;
        GUI.skin.label.fontSize = 35;
        GUI.skin.horizontalSlider.fixedHeight = 60; // Thicker sliders for easier thumb control
        GUI.skin.horizontalSliderThumb.fixedWidth = 60;
        GUI.skin.horizontalSliderThumb.fixedHeight = 60;

        // 2. TOP LEFT TOGGLE
        if (GUI.Button(new Rect(400, 20, 250, 100), _showMenu ? "CLOSE" : "DEBUG"))
        {
            _showMenu = !_showMenu;
        }

        if (!_showMenu) return;

        // 3. FULL SCREEN BACKGROUND
        GUI.backgroundColor = new Color(0, 0, 0, 0.9f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.backgroundColor = Color.white;

        // 4. DYNAMIC SCROLL VIEW
        // We calculate the content height at the end of the previous frame or use a large buffer
        _scrollPosition = GUI.BeginScrollView(
            new Rect(0, 130, Screen.width, Screen.height - 150),
            _scrollPosition,
            new Rect(0, 0, Screen.width - 100, 4000) // Huge buffer to ensure nothing is cut off
        );

        float xMargin = 50;
        float itemWidth = Screen.width - 150;
        float currentY = 20;

        // --- SECTION: PERFORMANCE STATS ---
        float ms = Time.unscaledDeltaTime * 1000f;
        float fps = 1.0f / Time.unscaledDeltaTime;
        GUI.contentColor = Color.cyan;
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"FRAME TIME: {ms:F2} ms ({Mathf.Ceil(fps)} FPS)");
        currentY += 100;
        GUI.contentColor = Color.white;

        // --- SECTION: QUALITY PRESETS ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- PROJECT QUALITY ---");
        currentY += 70;
        string[] qualityNames = QualitySettings.names;
        int currentLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < qualityNames.Length; i++)
        {
            GUI.backgroundColor = (i == currentLevel) ? Color.green : Color.white;
            if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 90), qualityNames[i].ToUpper()))
            {
                QualitySettings.SetQualityLevel(i, true);
            }
            currentY += 100;
        }
        GUI.backgroundColor = Color.white;
        currentY += 40;

        // --- SECTION: GPU & RENDERING ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- GPU & RENDERING ---");
        currentY += 70;
        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 110), "TOGGLE POST-PROCESSING"))
        {
            if (_cameraData != null) _cameraData.renderPostProcessing = !_cameraData.renderPostProcessing;
        }
        currentY += 130;

        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Render Scale: {_urpAsset.renderScale:F2}");
        currentY += 60;
        _urpAsset.renderScale = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _urpAsset.renderScale, 0.1f, 1.0f);
        currentY += 120;

        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 110), "TOGGLE SSAO (AMBIENT OCCLUSION)"))
        {
            if (rendererData != null)
            {
                foreach (var feature in rendererData.rendererFeatures)
                    if (feature.name.Contains("Ambient") || feature.name.Contains("SSAO")) feature.SetActive(!feature.isActive);
            }
        }
        currentY += 140;

        // --- SECTION: SHADOW POLISH ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- SHADOWS & GROUNDING ---");
        currentY += 70;
        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 110), "TOGGLE LIGHT SHADOWS"))
        {
            if (_dirLight != null) _dirLight.shadows = (_dirLight.shadows == LightShadows.None) ? LightShadows.Hard : LightShadows.None;
        }
        currentY += 130;

        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Distance: {_urpAsset.shadowDistance:F1}m");
        currentY += 60;
        _urpAsset.shadowDistance = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _urpAsset.shadowDistance, 2f, 20f);
        currentY += 120;

        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Strength: {_dirLight.shadowStrength:F2}");
        currentY += 60;
        _dirLight.shadowStrength = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _dirLight.shadowStrength, 0f, 1f);
        currentY += 120;

        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Bias: {_dirLight.shadowBias:F3}");
        currentY += 60;
        _dirLight.shadowBias = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _dirLight.shadowBias, 0f, 0.05f);
        currentY += 140;

        // --- SECTION: RIM LIGHT MASTER ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- STYLIZED RIM LIGHT ---");
        currentY += 70;

        bool isRimActive = Shader.IsKeywordEnabled("_RIM_LIGHT_ON");
        GUI.backgroundColor = isRimActive ? Color.green : Color.red;
        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 110), isRimActive ? "RIM LIGHT: ON" : "RIM LIGHT: OFF"))
        {
            if (isRimActive) Shader.DisableKeyword("_RIM_LIGHT_ON");
            else Shader.EnableKeyword("_RIM_LIGHT_ON");
        }
        GUI.backgroundColor = Color.white;
        currentY += 130;

        float currentPower = Shader.GetGlobalFloat("_GlobalRimPower");
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Rim Sharpness: {currentPower:F1}");
        currentY += 60;
        float newPower = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), currentPower, 1.0f, 12.0f);
        if (newPower != currentPower) Shader.SetGlobalFloat("_GlobalRimPower", newPower);
        currentY += 120;

        // --- SECTION: GRANULAR COLOR (THE SWEET SPOT) ---
        Color c = Shader.GetGlobalColor("_RimColor");
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Rim Tint (RGB): [{c.r:F2}, {c.g:F2}, {c.b:F2}]");
        currentY += 70;

        float r = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 70), c.r, 0f, 1f); currentY += 90;
        float g = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 70), c.g, 0f, 1f); currentY += 90;
        float b = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 70), c.b, 0f, 1f); currentY += 100;

        if (r != c.r || g != c.g || b != c.b) Shader.SetGlobalColor("_RimColor", new Color(r, g, b, 1.0f));

        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 100), "RESET TO SWEET SPOT (0.25, 0, 0)"))
        {
            Shader.SetGlobalColor("_RimColor", new Color(0.25f, 0f, 0f, 1.0f));
        }
        currentY += 150;

        GUI.EndScrollView();
    }
}