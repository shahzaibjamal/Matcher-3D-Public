using UnityEngine;
using TMPro;
using System.Text;

public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float _minFps = Mathf.Infinity;
    private float _maxFps = 0f;
    private float _timer;
    private float _accumulatedDeltaTime;
    private int _frameCount;

    // StringBuilder prevents memory allocation (garbage collection) every frame
    private StringBuilder _sb = new StringBuilder();

    void Awake()
    {
        // Optimized for 2D/3D Hybrid
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        // Ensure the text object doesn't block clicks
        if (statsText != null) statsText.raycastTarget = false;
    }

    void Update()
    {
        _accumulatedDeltaTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedDeltaTime > updateInterval)
        {
            float currentFps = _frameCount / _accumulatedDeltaTime;
            float frameTimeMs = (_accumulatedDeltaTime / _frameCount) * 1000f;

            // Start tracking min/max only after game settles (2 sec delay)
            if (Time.timeSinceLevelLoad > 2f)
            {
                if (currentFps < _minFps) _minFps = currentFps;
                if (currentFps > _maxFps) _maxFps = currentFps;
            }

            UpdateDisplay(currentFps, frameTimeMs);

            _accumulatedDeltaTime = 0;
            _frameCount = 0;
        }
    }

    private void UpdateDisplay(float fps, float ms)
    {
        _sb.Clear();
        _sb.Append("FPS: ").Append(fps.ToString("F1"))
        .Append(" (").Append(ms.ToString("F1")).AppendLine("ms)")
        .Append("<color=#FF5555>MIN: ").Append(_minFps.ToString("F1")).Append("</color> | ")
        .Append("<color=#55FF55>MAX: ").Append(_maxFps.ToString("F1")).Append("</color>");

        statsText.text = _sb.ToString();
    }
}