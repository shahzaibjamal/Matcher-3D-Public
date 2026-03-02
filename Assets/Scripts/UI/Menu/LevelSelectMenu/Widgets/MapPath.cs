using UnityEngine;

public class MapPath : MonoBehaviour
{
    public Transform[] waypoints; // Place these on your road in the Editor

    public Vector3 GetPointOnPath(float t)
    {
        int numSections = waypoints.Length - 1;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float u = t * numSections - currPt;

        Vector3 a = (currPt > 0) ? waypoints[currPt - 1].position : waypoints[currPt].position;
        Vector3 b = waypoints[currPt].position;
        Vector3 c = waypoints[currPt + 1].position;
        Vector3 d = (currPt + 2 < waypoints.Length) ? waypoints[currPt + 2].position : waypoints[currPt + 1].position;

        return .5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u) +
            (2f * a - 5f * b + 4f * c - d) * (u * u) +
            (-a + c) * u +
            2f * b
        );
    }
}