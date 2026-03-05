using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

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

    private float _minFps = Mathf.Infinity;
    private float _maxFps = 0f;
    private float _accumulatedDeltaTime;
    private int _frameCount;
    private StringBuilder _sb = new StringBuilder();

    private Texture2D _graphTexture;
    private Color[] _blankPixels;
    private float[] _fpsHistory;
    private int _historyIndex;

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        InitGraph();
    }

    private void InitGraph()
    {
        _graphTexture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);
        _graphTexture.filterMode = FilterMode.Bilinear; // Bilinear helps smoothen the line visually
        graphImage.texture = _graphTexture;

        _blankPixels = new Color[graphWidth * graphHeight];
        for (int i = 0; i < _blankPixels.Length; i++) _blankPixels[i] = new Color(0, 0, 0, 0.4f);

        _fpsHistory = new float[graphWidth];
    }

    void Update()
    {
        _accumulatedDeltaTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedDeltaTime > 0.1f) // Faster internal update for smoother graph line
        {
            float currentFps = _frameCount / _accumulatedDeltaTime;

            if (Time.timeSinceLevelLoad > 2f)
            {
                if (currentFps < _minFps) _minFps = currentFps;
                if (currentFps > _maxFps) _maxFps = currentFps;
            }

            // Smoothing via Moving Average
            float smoothedFps = GetMovingAverage(currentFps);
            UpdateGraph(smoothedFps);

            // Text only updates at the user-defined interval
            if (_accumulatedDeltaTime > updateInterval)
            {
                UpdateDisplay(currentFps, (_accumulatedDeltaTime / _frameCount) * 1000f);
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

            // Map FPS (0-80) to graph height
            float y1 = Mathf.InverseLerp(0, 80, _fpsHistory[idx1]) * graphHeight;
            float y2 = Mathf.InverseLerp(0, 80, _fpsHistory[idx2]) * graphHeight;

            DrawLine((int)x, (int)y1, (int)x + 1, (int)y2, lineColor);
        }

        _graphTexture.Apply();
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


}