using UnityEngine;
using TMPro;
using System.Collections;

public class TextAnimations : MonoBehaviour
{
    [Header("Reveal Settings")]
    [Range(0.1f, 2f)] public float duration = 0.5f;
    [Range(0.01f, 0.5f)] public float characterDelay = 0.05f;

    [Header("Exaggeration")]
    [Range(0f, 10f)] public float overshoot = 3.5f;
    public float waveHeight = 60f;
    public float waveFrequency = 1.5f;

    [Header("Color Flash")]
    public Color32 flashColor = Color.white;

    private TMP_Text _textComponent;
    private TMP_MeshInfo[] _cachedMeshInfo;

    void Awake() => _textComponent = GetComponent<TMP_Text>();

    [ContextMenu("Play Reveal")]
    public void PlayReveal()
    {
        StopAllCoroutines();
        _textComponent.ForceMeshUpdate();
        _cachedMeshInfo = _textComponent.textInfo.CopyMeshInfoVertexData();
        StartCoroutine(RevealCharacters());
    }

    private IEnumerator RevealCharacters()
    {
        int totalCharacters = _textComponent.textInfo.characterCount;
        for (int i = 0; i < totalCharacters; i++)
        {
            ApplyTransformation(i, 0, 0, new Color32(0, 0, 0, 0));
        }

        for (int i = 0; i < totalCharacters; i++)
        {
            if (!_textComponent.textInfo.characterInfo[i].isVisible) continue;
            StartCoroutine(AnimateCharacter(i));
            yield return new WaitForSeconds(characterDelay);
        }
    }

    private IEnumerator AnimateCharacter(int charIndex)
    {
        float timer = 0;

        // Get target color from the cached data
        int mIdx = _textComponent.textInfo.characterInfo[charIndex].materialReferenceIndex;
        int vIdx = _textComponent.textInfo.characterInfo[charIndex].vertexIndex;
        Color32 targetColor = _cachedMeshInfo[mIdx].colors32[vIdx];

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float lerp = Mathf.Clamp01(timer / duration);

            float scale = CalculateOutBack(lerp);
            float yOffset = Mathf.Sin(lerp * Mathf.PI * waveFrequency) * waveHeight * (1 - lerp);

            // Flash color to target color
            Color32 currentColor = Color32.Lerp(flashColor, targetColor, lerp);

            ApplyTransformation(charIndex, scale, yOffset, currentColor);
            yield return null;
        }
        ApplyTransformation(charIndex, 1.0f, 0f, targetColor);
    }

    private void ApplyTransformation(int charIndex, float scale, float yOffset, Color32 color)
    {
        if (_textComponent == null || _cachedMeshInfo == null) return;

        var textInfo = _textComponent.textInfo;
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        int mIdx = charInfo.materialReferenceIndex;
        int vIdx = charInfo.vertexIndex;

        Vector3[] destVerts = textInfo.meshInfo[mIdx].vertices;
        Vector3[] srcVerts = _cachedMeshInfo[mIdx].vertices;
        Color32[] destColors = textInfo.meshInfo[mIdx].colors32;

        Vector3 charCenter = (srcVerts[vIdx + 0] + srcVerts[vIdx + 2]) / 2f;
        Vector3 targetPos = charCenter + new Vector3(0, yOffset, 0);

        Matrix4x4 matrix = Matrix4x4.TRS(targetPos, Quaternion.identity, Vector3.one * scale)
                         * Matrix4x4.TRS(-charCenter, Quaternion.identity, Vector3.one);

        for (int j = 0; j < 4; j++)
        {
            destVerts[vIdx + j] = matrix.MultiplyPoint3x4(srcVerts[vIdx + j]);
            destColors[vIdx + j] = color;
        }

        // IMPORTANT: Tell TMP to update both geometry and colors
        _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    private float CalculateOutBack(float t)
    {
        float s = overshoot;
        return ((t = t - 1) * t * ((s + 1) * t + s) + 1);
    }
}