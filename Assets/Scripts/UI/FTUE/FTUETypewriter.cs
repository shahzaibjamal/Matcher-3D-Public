using System.Collections;
using TMPro;
using UnityEngine;

public class FTUETypewriter : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private float charactersPerSecond = 30f;

    private bool _isTyping;
    public bool IsTyping => _isTyping;

    public void ShowMessage(string message, System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(TypeText(message, onComplete));
    }

    private IEnumerator TypeText(string message, System.Action onComplete)
    {
        _isTyping = true;

        // 1. Set the full text immediately so the layout box sizes correctly
        textComponent.text = message;

        // 2. Hide all characters
        textComponent.maxVisibleCharacters = 0;

        // 3. Force a mesh update to prevent ghosting or frame-delayed rendering
        textComponent.ForceMeshUpdate();

        int totalVisibleCharacters = message.Length;
        int counter = 0;

        while (counter <= totalVisibleCharacters)
        {
            // 4. Increment visible count
            textComponent.maxVisibleCharacters = counter;
            counter++;

            // Wait based on speed
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }

        _isTyping = false;
        onComplete?.Invoke();
    }
}