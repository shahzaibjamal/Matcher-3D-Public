using TMPro;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float _minFps = Mathf.Infinity;
    private float _maxFps = 0f;
    private float _timer;
    private float _accumulatedDeltaTime;
    private int _frameCount;

    void Awake()
    {
        // LOWEST RESOURCE SETTING: 
        // Unlimited frames cause CPU/GPU "coil whine" and heat in 2D games.
        // Lock it to 60 for mobile/standard or 144 for high-end.
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1; // VSync on prevents screen tearing in 2D
    }

    void Update()
    {
        _accumulatedDeltaTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedDeltaTime > updateInterval)
        {
            float currentFps = _frameCount / _accumulatedDeltaTime;
            float frameTimeMs = (_accumulatedDeltaTime / _frameCount) * 1000f;

            if (currentFps < _minFps && Time.timeSinceLevelLoad > 2f) _minFps = currentFps;
            if (currentFps > _maxFps) _maxFps = currentFps;

            statsText.text = string.Format(
                "FPS: {0:F1} ({1:F1}ms)\n" +
                "<color=red>MIN: {2:F1}</color> | <color=green>MAX: {3:F1}</color>",
                currentFps, frameTimeMs, _minFps, _maxFps);

            _accumulatedDeltaTime = 0;
            _frameCount = 0;
        }
    }
}