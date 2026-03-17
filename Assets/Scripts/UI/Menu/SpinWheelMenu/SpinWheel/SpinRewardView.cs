using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinRewardView : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text amountText;
    public Image Glow1;
    public Image Glow2;

    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;
    public bool useClockTicking = false;
    public float tickInterval = 0.5f; // Time between jumps
    public float tickAngle = 30f;    // Degrees per jump

    [Header("Glow 2 Scale Settings")]
    public bool enableScaling = true;
    public float scaleSpeed = 2f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    private float _tickTimer;
    private float _pulseTimer;

    public void SetData(Sprite sprite, int amount)
    {
        iconImage.sprite = sprite;
        amountText.text = amount.ToString();
    }

    void Update()
    {
        HandleRotation();

        if (enableScaling)
            HandleScaling();
    }

    private void HandleRotation()
    {
        if (useClockTicking)
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0;
                // Opposite jumps
                Glow1.transform.Rotate(Vector3.forward, -tickAngle);
                Glow2.transform.Rotate(Vector3.forward, tickAngle);
            }
        }
        else
        {
            // Smooth opposite rotation
            float step = rotationSpeed * Time.deltaTime;
            Glow1.transform.Rotate(Vector3.forward, step);
            Glow2.transform.Rotate(Vector3.forward, -step);
        }
    }

    private void HandleScaling()
    {
        // Smoothly oscillate between 0 and 1
        _pulseTimer += Time.deltaTime * scaleSpeed;
        float lerpFactor = (Mathf.Sin(_pulseTimer) + 1f) / 2f;

        float currentScale = Mathf.Lerp(minScale, maxScale, lerpFactor);
        Glow2.transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }
}