using UnityEngine;
using System.Collections;

public class UIScreenShake : MonoBehaviour
{
    public static UIScreenShake Instance;   // Singleton reference
    public RectTransform screenRoot;
    public RectTransform popupRoot;
    public float defaultDuration = 0.3f;
    public float defaultMagnitude = 20f;

    private void Awake()
    {
        Instance = this; // Assign singleton
    }

    public void Shake(float duration, float magnitude)
    {
        Scheduler.Instance.RunCoroutine(DoShake(duration, magnitude));
        Scheduler.Instance.RunCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        Vector3 originalPosScreen = screenRoot.localPosition;
        Vector3 originalPosOverlay = popupRoot.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            screenRoot.localPosition = originalPosScreen + new Vector3(x, y, 0);
            popupRoot.localPosition = originalPosOverlay + new Vector3(x, y, 0);
            Debug.LogError("Shaking");
            elapsed += Time.deltaTime;
            yield return null;
        }

        popupRoot.localPosition = originalPosOverlay;
        screenRoot.localPosition = originalPosScreen;
    }


    // Static wrapper
    public static void ShakeStatic(float duration = 0.3f, float magnitude = 20f)
    {
        if (Instance != null)
            Instance.Shake(duration, magnitude);
        else
            Debug.LogWarning("UIScreenShake not initialized in scene!");
    }
}
