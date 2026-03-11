using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;
using System;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private RawImage graphImage;

    [Header("Graph Settings")]
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private int graphWidth = 200;
    [SerializeField] private int graphHeight = 60;
    [SerializeField] private Color lineColor = Color.green;
    [SerializeField] private int smoothingWindow = 5; // Higher = smoother graph
    [SerializeField] private CanvasGroup monitorCanvasGroup;
    private float _minFps = Mathf.Infinity;
    private float _maxFps = 0f;
    private float _accumulatedDeltaTime;
    private int _frameCount;
    private StringBuilder _sb = new StringBuilder();
    private bool _isVisible = true;
    private Texture2D _graphTexture;
    private Color[] _blankPixels;
    private float[] _fpsHistory;
    private int _historyIndex;

    [Header("Performance")]
    [Range(30, 120)]
    [SerializeField] private int targetFrameRate = 60;

    [Tooltip("0 = Don't Sync, 1 = 60fps, 2 = 30fps")]
    [Range(0, 2)]
    [SerializeField] private int vSyncCount = 1;

    [Header("Physics Stability")]
    [Tooltip("Lower values prevent 'explosions' when items overlap on spawn.")]
    [SerializeField] private float maxDepenetrationVelocity = 0.25f;

    [Tooltip("Default is -9.81. Lower values make items feel floatier.")]
    [SerializeField] private Vector3 customGravity = new Vector3(0, -4.0f, 0);

    void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = vSyncCount;

        // Apply Global Physics Settings
        Physics.defaultMaxDepenetrationVelocity = maxDepenetrationVelocity;
        Physics.gravity = customGravity;
        InitGraph();
    }
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Physics.defaultMaxDepenetrationVelocity = maxDepenetrationVelocity;
            Physics.gravity = customGravity;
        }
    }
    private void InitGraph()
    {
        _graphTexture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear // Bilinear helps smoothen the line visually
        };
        graphImage.texture = _graphTexture;

        _blankPixels = new Color[graphWidth * graphHeight];
        for (int i = 0; i < _blankPixels.Length; i++) _blankPixels[i] = new Color(0, 0, 0, 0.4f);

        _fpsHistory = new float[graphWidth];
        for (int i = 0; i < graphWidth; i++)
        {
            _fpsHistory[i] = 60f; // Start at a "clean" 60fps line
        }
    }

    void Update()
    {
        _accumulatedDeltaTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedDeltaTime > 0.05f) // High frequency for smooth data
        {
            float currentFps = _frameCount / _accumulatedDeltaTime;

            // 1. ALWAYS track data (cheap)
            _fpsHistory[_historyIndex] = currentFps;
            _historyIndex = (_historyIndex + 1) % graphWidth;

            if (Time.timeSinceLevelLoad > 2f)
            {
                if (currentFps < _minFps) _minFps = currentFps;
                if (currentFps > _maxFps) _maxFps = currentFps;
            }

            // 2. ONLY render the texture if the panel is active (expensive)
            if (_isVisible)
            {
                UpdateGraph(GetMovingAverage(currentFps));

                // Text updates slightly slower to stay readable
                if (_accumulatedDeltaTime > updateInterval)
                {
                    UpdateDisplay(currentFps, (_accumulatedDeltaTime / _frameCount) * 1000f);
                }
            }

            if (_accumulatedDeltaTime > updateInterval)
            {
                _accumulatedDeltaTime = 0;
                _frameCount = 0;
            }
        }
    }
    private float GetMovingAverage(float newFps)
    {
        _fpsHistory[_historyIndex] = newFps;
        _historyIndex = (_historyIndex + 1) % graphWidth;

        float sum = 0;
        for (int i = 0; i < smoothingWindow; i++)
        {
            int idx = (_historyIndex - 1 - i + graphWidth) % graphWidth;
            sum += _fpsHistory[idx];
        }
        return sum / smoothingWindow;
    }

    private void UpdateGraph(float fps)
    {
        _graphTexture.SetPixels(_blankPixels);

        for (int x = 0; x < graphWidth - 1; x++)
        {
            int idx1 = (_historyIndex + x) % graphWidth;
            int idx2 = (_historyIndex + x + 1) % graphWidth;

            // Using -10 to 90 provides a vertical margin
            float y1 = Mathf.InverseLerp(-10, 90, _fpsHistory[idx1]) * graphHeight;
            float y2 = Mathf.InverseLerp(-10, 90, _fpsHistory[idx2]) * graphHeight;

            DrawLine((int)x, (int)y1, (int)x + 1, (int)y2, lineColor);
        }
        _graphTexture.Apply();
    }

    private void DrawHorizontalLine(int y, Color col)
    {
        if (y < 0 || y >= graphHeight) return;
        for (int x = 0; x < graphWidth; x++)
        {
            _graphTexture.SetPixel(x, y, col);
        }
    }

    // Basic line drawing algorithm
    private void DrawLine(int x0, int y0, int x1, int y1, Color col)
    {
        int dy = y1 - y0;
        int dx = x1 - x0;
        int step = Mathf.Abs(dx) > Mathf.Abs(dy) ? Mathf.Abs(dx) : Mathf.Abs(dy);

        float xInc = dx / (float)step;
        float yInc = dy / (float)step;

        float x = x0;
        float y = y0;

        for (int i = 0; i <= step; i++)
        {
            _graphTexture.SetPixel((int)x, (int)y, col);
            x += xInc;
            y += yInc;
        }
        for (int i = 0; i <= step; i++)
        {
            _graphTexture.SetPixel((int)x, (int)y, col);
            // Add this line for a "Toon" double-thick line:
            _graphTexture.SetPixel((int)x, (int)y + 1, col);

            x += xInc;
            y += yInc;
        }
    }


    private void UpdateDisplay(float fps, float ms)
    {
        _sb.Clear();
        _sb.Append("FPS: ").Append(fps.ToString("F1"))
           .Append(" (").Append(ms.ToString("F1")).AppendLine("ms)")
           .Append("<color=#FF5555>MIN: ").Append(_minFps == Mathf.Infinity ? "0" : _minFps.ToString("F1")).Append("</color>  ")
           // Changed " | " to " / " or just extra spaces below:
           .Append("<color=#AAAAAA>/</color> ")
           .Append("<color=#55FF55>MAX: ").Append(_maxFps.ToString("F1")).Append("</color>");

        statsText.text = _sb.ToString();
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        monitorCanvasGroup.alpha = _isVisible ? 1f : 0f;
        monitorCanvasGroup.blocksRaycasts = _isVisible;
    }

    private void OnGUI()
    {
        // 1. Position the button in the top-left corner
        // GUI.Button(Rect(x, y, width, height), text)
        if (GUI.Button(new Rect(20, 20, 250, 100), "Light"))
        {
            ToggleLight();
        }
    }
    private void ToggleLight()
    {
        Light sceneLight = null;
        if (sceneLight == null)
        {
            sceneLight = RenderSettings.sun; // Try to grab the default Sun if not assigned
        }

        if (sceneLight != null)
        {
            sceneLight.enabled = !sceneLight.enabled;
        }
    }
}
