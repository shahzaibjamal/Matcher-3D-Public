using System.Collections.Generic;
using UnityEngine;

// 3. The Database (The "Library")
[CreateAssetMenu(fileName = "FTUEDatabase", menuName = "FTUE/Database")]
public class FTUEDatabase : ScriptableObject
{
    public List<FTUESequence> sequences;

    public FTUESequence GetByID(string id) => sequences.Find(s => s.name == id);
}