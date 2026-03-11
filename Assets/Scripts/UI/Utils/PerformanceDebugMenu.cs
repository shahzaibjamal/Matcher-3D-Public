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
        GUI.skin.button.fontSize = 35;
        GUI.skin.label.fontSize = 35;
        GUI.skin.verticalScrollbar.fixedWidth = 50;
        GUI.skin.verticalScrollbarThumb.fixedWidth = 50;

        // 1. TOP LEFT TOGGLE
        if (GUI.Button(new Rect(20, 20, 250, 100), _showMenu ? "CLOSE" : "DEBUG"))
        {
            _showMenu = !_showMenu;
        }

        if (!_showMenu) return;

        // 2. FULL SCREEN BACKGROUND
        GUI.backgroundColor = new Color(0, 0, 0, 0.95f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.backgroundColor = Color.white;

        // 3. SCROLL VIEW
        _scrollPosition = GUI.BeginScrollView(
            new Rect(0, 130, Screen.width, Screen.height - 150),
            _scrollPosition,
            new Rect(0, 0, Screen.width - 100, 2500) // Height increased for new settings
        );

        float xMargin = 50;
        float itemWidth = Screen.width - 150;
        float currentY = 20;

        // --- STATS ---
        float ms = Time.unscaledDeltaTime * 1000f;
        float fps = 1.0f / Time.unscaledDeltaTime;
        GUI.contentColor = Color.cyan;
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"FRAME TIME: {ms:F2} ms ({Mathf.Ceil(fps)} FPS)");
        currentY += 100;
        GUI.contentColor = Color.white;

        // --- NEW SECTION: QUALITY SETTINGS SELECTOR ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- PROJECT QUALITY PRESETS ---");
        currentY += 70;

        string[] names = QualitySettings.names;
        int currentLevel = QualitySettings.GetQualityLevel();

        for (int i = 0; i < names.Length; i++)
        {
            // Highlight the currently active quality level in Green
            if (i == currentLevel) GUI.backgroundColor = Color.green;
            else GUI.backgroundColor = Color.white;

            if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 90), names[i].ToUpper()))
            {
                QualitySettings.SetQualityLevel(i, true);
            }
            currentY += 100;
        }
        GUI.backgroundColor = Color.white; // Reset
        currentY += 40;

        // --- GPU TOOLS ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- GPU & RENDERING ---");
        currentY += 70;

        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 120), "TOGGLE POST-PROCESSING"))
        {
            if (_cameraData != null) _cameraData.renderPostProcessing = !_cameraData.renderPostProcessing;
        }
        currentY += 140;

        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 120), "TOGGLE SHADOWS"))
        {
            if (_dirLight != null) _dirLight.shadows = (_dirLight.shadows == LightShadows.None) ? LightShadows.Hard : LightShadows.None;
        }
        currentY += 140;

        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Manual Render Scale: {_urpAsset.renderScale:F2}");
        currentY += 60;
        _urpAsset.renderScale = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _urpAsset.renderScale, 0.1f, 1.0f);
        currentY += 140;

        // --- PHYSICS ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- PHYSICS ---");
        currentY += 70;
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Fixed Timestep: {Time.fixedDeltaTime:F3}");
        currentY += 60;
        if (GUI.Button(new Rect(xMargin, currentY, itemWidth / 2 - 20, 120), "LOW (0.05)")) Time.fixedDeltaTime = 0.05f;
        if (GUI.Button(new Rect(xMargin + itemWidth / 2 + 20, currentY, itemWidth / 2 - 20, 120), "HIGH (0.02)")) Time.fixedDeltaTime = 0.02f;
        currentY += 180;

        // --- FEATURES ---
        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 120), "TOGGLE HINT OUTLINE"))
        {
            if (rendererData != null)
            {
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature.name.Contains("Hint")) feature.SetActive(!feature.isActive);
                }
            }
        }
        currentY += 160;

        // --- SECTION: AMBIENT OCCLUSION (SSAO) ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- ADVANCED EFFECTS ---");
        currentY += 80;

        if (GUI.Button(new Rect(xMargin, currentY, itemWidth, 120), "TOGGLE SSAO (HEAVY)"))
        {
            if (rendererData != null)
            {
                foreach (var feature in rendererData.rendererFeatures)
                {
                    // Usually named "ScreenSpaceAmbientOcclusion" by default
                    if (feature.name.Contains("Ambient") || feature.name.Contains("SSAO"))
                    {
                        feature.SetActive(!feature.isActive);
                    }
                }
            }
        }
        currentY += 140;

        // --- SECTION: SHADOW POLISH & GROUNDING ---
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), "--- SHADOW POLISH ---");
        currentY += 80;

        // Shadow Distance Slider (5-15m range)
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Distance: {_urpAsset.shadowDistance:F1}m");
        currentY += 60;
        _urpAsset.shadowDistance = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _urpAsset.shadowDistance, 5f, 15f);
        currentY += 120;

        // Shadow Strength
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Strength: {_dirLight.shadowStrength:F2}");
        currentY += 60;
        _dirLight.shadowStrength = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _dirLight.shadowStrength, 0f, 1f);
        currentY += 120;

        // Shadow Bias
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"Shadow Bias: {_dirLight.shadowBias:F3}");
        currentY += 60;
        _dirLight.shadowBias = GUI.HorizontalSlider(new Rect(xMargin, currentY, itemWidth, 80), _dirLight.shadowBias, 0f, 0.05f);
        currentY += 140;

        GUI.EndScrollView();
    }
}