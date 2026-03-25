using System.Collections.Generic;
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

    private int _selectedShaderIndex = 1; // Default to Lit
    private readonly string[] _shaderOptions = { "Simple Lit", "Lit", "Cartoon" };
    private readonly string[] _shaderPaths = {
        "Universal Render Pipeline/Simple Lit",
        "Universal Render Pipeline/Lit",
        "Shader Graphs/CartoonShader" // Ensure this matches your specific path
    };

    private void Start()
    {
        _dirLight = RenderSettings.sun;
        if (Camera.main != null)
            _cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

        _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        DataManager.OnDataLoaded += OnDataManagerLoaded;
    }

    private void OnDataManagerLoaded()
    {
        Scheduler.Instance.SubscribeGUI(DebugOnGUIGraphicsSettings);
        Scheduler.Instance.SubscribeUpdate(DebugOnGUIGraphicsSettingsnUpdate);
    }

    void OnDestroy()
    {
        DataManager.OnDataLoaded -= OnDataManagerLoaded;
        Scheduler.Instance.UnsubscribeGUI(DebugOnGUIGraphicsSettings);
        Scheduler.Instance.UnsubscribeUpdate(DebugOnGUIGraphicsSettingsnUpdate);
    }

    private void DebugOnGUIGraphicsSettingsnUpdate(float dt)
    {
        if (!DataManager.Instance.Metadata.Settings.ShowGraphicsSettings)
            return;
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

    private void DebugOnGUIGraphicsSettings()
    {
        if (!DataManager.Instance.Metadata.Settings.ShowGraphicsSettings)
            return;

        // 1. GLOBAL STYLING
        GUI.skin.button.fontSize = 35;
        GUI.skin.label.fontSize = 35;
        GUI.skin.horizontalSlider.fixedHeight = 60;
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
        // The content rect (3rd param) height should be currentY from the end of the last frame
        _scrollPosition = GUI.BeginScrollView(
            new Rect(0, 130, Screen.width, Screen.height - 150),
            _scrollPosition,
            new Rect(0, 0, Screen.width - 100, 5000)
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

#if UNITY_EDITOR
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"BATCHES: {UnityEditor.UnityStats.batches} | DRAW CALLS: {UnityEditor.UnityStats.drawCalls}");
        currentY += 80;
        GUI.Label(new Rect(xMargin, currentY, itemWidth, 60), $"TRIS: {UnityEditor.UnityStats.triangles:N0}");
        currentY += 100;
#endif
        GUI.contentColor = Color.white;

        // --- SECTION: GLOBAL SHADER SWAPPER (NEW) ---
        GUI.Box(new Rect(xMargin - 10, currentY, itemWidth + 20, 350), ""); // Visual container
        GUI.Label(new Rect(xMargin, currentY + 10, itemWidth, 60), "--- GLOBAL SHADER SWAP ---");
        currentY += 80;

        // SelectionGrid needs to stay within the currentY flow
        int newIndex = GUI.SelectionGrid(new Rect(xMargin, currentY, itemWidth, 240), _selectedShaderIndex, _shaderOptions, 1);
        currentY += 270;

        if (newIndex != _selectedShaderIndex)
        {
            _selectedShaderIndex = newIndex;
            // Call the function that uses targetMaterials
            ApplyShaderToMaterials(_shaderPaths[_selectedShaderIndex]);
        }
        currentY += 50;

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

        GUI.EndScrollView();
    }
    private void ApplyShaderToMaterials(string shaderPath)
    {
        Shader newShader = Shader.Find(shaderPath);
        if (newShader == null)
        {
            Debug.LogError($"Shader not found: {shaderPath}");
            return;
        }

        // 1. Find all Renderers in the scene
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        int count = 0;

        foreach (Renderer rend in allRenderers)
        {
            foreach (Material mat in rend.materials) // Use .materials to avoid modifying project assets permanently
            {
                if (mat == null || mat.name.Contains("floor_mat")) continue;

                // 2. EXTRACT PROPERTIES (Checking both URP and Legacy names)
                Color baseCol = Color.white;
                if (mat.HasProperty("_BaseColor")) baseCol = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) baseCol = mat.GetColor("_Color");

                Texture baseMap = null;
                if (mat.HasProperty("_BaseMap")) baseMap = mat.GetTexture("_BaseMap");
                else if (mat.HasProperty("_MainTex")) baseMap = mat.GetTexture("_MainTex");

                // 3. APPLY NEW SHADER
                mat.shader = newShader;

                // 4. RESTORE PROPERTIES (Apply to all possible slots to be safe)
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);

                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseMap);

                // 5. HANDLE THE BOOLEAN
                if (mat.HasProperty("_UseTexture"))
                {
                    // Set the float value
                    mat.SetFloat("_UseTexture", baseMap != null ? 1.0f : 0.0f);

                    // CRITICAL: Many shaders need the Keyword enabled to actually show the texture
                    if (baseMap != null) mat.EnableKeyword("_USE_TEXTURE_ON");
                    else mat.DisableKeyword("_USE_TEXTURE_ON");
                }

                count++;
            }
        }

        Debug.Log($"[ShaderDebugger] Swapped {count} scene materials to {newShader.name}");
    }
}