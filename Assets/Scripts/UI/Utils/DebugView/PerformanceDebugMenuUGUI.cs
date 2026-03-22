using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PerformanceDebugMenuUGUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("Performance Stats")]
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Rendering Controls")]
    [SerializeField] private Slider renderScaleSlider;
    [SerializeField] private TextMeshProUGUI renderScaleLabel;
    [SerializeField] private Button postProcessToggle;
    [SerializeField] private Button ssaoToggle;

    [Header("Shadow Controls")]
    [SerializeField] private Slider shadowDistanceSlider;
    [SerializeField] private TextMeshProUGUI shadowDistLabel;
    [SerializeField] private Slider shadowStrengthSlider;
    [SerializeField] private Slider shadowBiasSlider;

    [Header("Rim Light Controls")]
    [SerializeField] private Button rimToggle;
    [SerializeField] private Slider rimPowerSlider;
    [SerializeField] private Slider rimR, rimG, rimB;

    [Header("URP Data")]
    [SerializeField] private UniversalRendererData rendererData;

    private Light _dirLight;
    private UniversalAdditionalCameraData _cameraData;
    private UniversalRenderPipelineAsset _urpAsset;
    private bool _isMenuOpen = false;

    private void Start()
    {
        _dirLight = RenderSettings.sun;
        if (Camera.main != null)
            _cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

        _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        // Initialize UI States
        SetupListeners();
        UpdateUIValues();
        mainPanel.SetActive(false);
    }

    private void SetupListeners()
    {
        toggleButton.onClick.AddListener(ToggleMenu);

        // Rendering
        renderScaleSlider.onValueChanged.AddListener(val => { _urpAsset.renderScale = val; });
        postProcessToggle.onClick.AddListener(() => { if (_cameraData != null) _cameraData.renderPostProcessing = !_cameraData.renderPostProcessing; });

        // Shadows
        shadowDistanceSlider.onValueChanged.AddListener(val => { _urpAsset.shadowDistance = val; });
        shadowStrengthSlider.onValueChanged.AddListener(val => { if (_dirLight) _dirLight.shadowStrength = val; });

        // Rim
        rimToggle.onClick.AddListener(() =>
        {
            if (Shader.IsKeywordEnabled("_RIM_LIGHT_ON")) Shader.DisableKeyword("_RIM_LIGHT_ON");
            else Shader.EnableKeyword("_RIM_LIGHT_ON");
        });
        rimPowerSlider.onValueChanged.AddListener(val => Shader.SetGlobalFloat("_GlobalRimPower", val));
    }

    private void Update()
    {
        if (!_isMenuOpen) return;

        // Update Stats every frame
        float ms = Time.unscaledDeltaTime * 1000f;
        float fps = 1.0f / Time.unscaledDeltaTime;

        string stats = $"<color=#00FFFF>FRAME: {ms:F2}ms ({Mathf.Ceil(fps)} FPS)</color>\n";

#if UNITY_EDITOR
        stats += $"BATCHES: {UnityEditor.UnityStats.batches}\n";
        stats += $"DRAW CALLS: {UnityEditor.UnityStats.drawCalls}\n";
        stats += $"SETPASS: {UnityEditor.UnityStats.setPassCalls}\n";
#endif
        statsText.text = stats;

        // Dynamic Labels
        renderScaleLabel.text = $"Render Scale: {_urpAsset.renderScale:F2}";
        shadowDistLabel.text = $"Shadow Dist: {_urpAsset.shadowDistance:F1}m";

        // Handle Rim Color Real-time
        if (rimR != null)
        {
            Color c = new Color(rimR.value, rimG.value, rimB.value, 1.0f);
            Shader.SetGlobalColor("_RimColor", c);
        }
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
        mainPanel.SetActive(_isMenuOpen);
        toggleButtonText.text = _isMenuOpen ? "CLOSE" : "DEBUG";
    }

    private void UpdateUIValues()
    {
        // Set sliders to current URP values on start
        if (_urpAsset)
        {
            renderScaleSlider.value = _urpAsset.renderScale;
            shadowDistanceSlider.value = _urpAsset.shadowDistance;
        }
        if (_dirLight)
        {
            shadowStrengthSlider.value = _dirLight.shadowStrength;
            shadowBiasSlider.value = _dirLight.shadowBias;
        }
    }

    public void SetQuality(int level)
    {
        QualitySettings.SetQualityLevel(level, true);
    }
}