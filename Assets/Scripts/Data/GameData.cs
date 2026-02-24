using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "Game/Game Data")]
public class GameData : ScriptableObject
{
    [Tooltip("List of all durations in the main gameplay loop")]
    public float FlightUpDuration;
    public float FlightToTrayDuration;
    public float LeapDuration;
    public float MergeDuration;
}
