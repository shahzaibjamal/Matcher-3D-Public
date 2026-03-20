using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class MapPath : MonoBehaviour
{
    [Header("Path Data")]
    public List<RectTransform> waypoints = new List<RectTransform>();

    [Header("Editor Tools")]
    public RectTransform editorHelper; // Use a UI Image or Empty Rect as helper

    private RectTransform _rectTransform;
    public RectTransform RectTransform => _rectTransform ??= GetComponent<RectTransform>();

    /// <summary>
    /// Returns the anchoredPosition on the spline relative to this MapPath.
    /// </summary>
    public Vector3 GetPointOnPath(float t)
    {
        // Cleanup nulls (if points were deleted in Hierarchy)
        waypoints.RemoveAll(item => item == null);

        if (waypoints.Count < 2) return Vector3.zero;

        int numSections = waypoints.Count - 1;
        int currPt = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float u = t * numSections - currPt;

        // Use anchoredPosition3D to ensure Z-depth is preserved if needed
        Vector3 a = (currPt > 0) ? waypoints[currPt - 1].anchoredPosition3D : waypoints[currPt].anchoredPosition3D;
        Vector3 b = waypoints[currPt].anchoredPosition3D;
        Vector3 c = waypoints[currPt + 1].anchoredPosition3D;
        Vector3 d = (currPt + 2 < waypoints.Count) ? waypoints[currPt + 2].anchoredPosition3D : waypoints[currPt + 1].anchoredPosition3D;

        return 0.5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u) +
            (2f * a - 5f * b + 4f * c - d) * (u * u) +
            (-a + c) * u +
            2f * b
        );
    }

    [ContextMenu("Add Waypoint at Helper %#m")]
    public void AddWaypoint()
    {
        if (editorHelper == null) return;

        // 1. Create a new UI GameObject
        GameObject newPointObj = new GameObject($"WayPoint_{waypoints.Count}", typeof(RectTransform));
        RectTransform newRt = newPointObj.GetComponent<RectTransform>();

        // 2. Parent it to the MapPath
        newRt.SetParent(this.transform, false);

        // 3. Match the helper's position relative to the parent
        // We use anchoredPosition3D so it copies the helper's exact UI alignment
        newRt.anchoredPosition3D = editorHelper.anchoredPosition3D;

        // 4. Reset Scale/Rotation common in UI instantiation
        newRt.localScale = Vector3.one;
        newRt.localRotation = Quaternion.identity;

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(newPointObj, "Create UI Waypoint");
#endif

        waypoints.Add(newRt);
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        // Visualizing in Scene View (Converting Anchored back to World for Gizmos)
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 10f); // Larger radius for UI scale

            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        Gizmos.color = Color.yellow;
        Vector3 prevPos = GetWorldPosFromAnchored(GetPointOnPath(0));
        for (float t = 0.02f; t <= 1.01f; t += 0.02f)
        {
            Vector3 nextPos = GetWorldPosFromAnchored(GetPointOnPath(t));
            Gizmos.DrawLine(prevPos, nextPos);
            prevPos = nextPos;
        }
    }

    private Vector3 GetWorldPosFromAnchored(Vector3 anchoredPos)
    {
        // Internal helper to draw Gizmos correctly by converting relative UI space back to World space
        return transform.TransformPoint(anchoredPos);
    }
}