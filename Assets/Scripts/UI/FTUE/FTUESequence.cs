using System.Collections.Generic;
using UnityEngine;

// 2. The Sequence (The "Playlist")
[CreateAssetMenu(fileName = "NewSequence", menuName = "FTUE/Sequence")]
public class FTUESequence : ScriptableObject
{
    public List<FTUEStep> steps;
}
