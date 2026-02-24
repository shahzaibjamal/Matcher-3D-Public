using UnityEngine;

public class Metadata : MonoBehaviour
{
    public static Metadata Instance { get; private set; }

    [Header("Databases")]
    public LevelDatabase levelDatabase;
    public ItemDatabase itemDatabase;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // enforce singleton
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // persist across scenes
    }
}
