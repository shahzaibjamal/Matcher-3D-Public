using UnityEditor;
using TMPro;

[InitializeOnLoad]
public static class AutoLocalize
{
    static AutoLocalize()
    {
        // Fires only when a new component is actually added
        ObjectFactory.componentWasAdded += c =>
        {
            if (c is TMP_Text)
            {
                if (!c.gameObject.GetComponent<LocalizedText>())
                {
                    c.gameObject.AddComponent<LocalizedText>();
                }
            }
        };
    }
}