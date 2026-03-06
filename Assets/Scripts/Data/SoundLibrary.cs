using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Game/Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{

    public SoundEffect[] sfxList;

    public SoundEffect GetSFX(string name)
    {
        return System.Array.Find(sfxList, s => s.Name == name);
    }
}
[System.Serializable]
public class SoundEffect
{
    public string Name;
    public AudioClip Clip;
    [Range(0, 1)] public float Volume = 1;
}
