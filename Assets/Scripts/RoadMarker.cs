using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RoadMarker : MonoBehaviour
{
    public Transform directionRef;   // assign DirectionRef
    public float distance = 1f;      // scaled (1 = ~5m)
    public float lineWidth = 2f;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;

        DrawLine();
    }

    void DrawLine()
    {
        Vector3 forward = directionRef.forward;

        // perpendicular to road
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

        // center position on road
        Vector3 center = directionRef.position + forward * distance;
        center.y += 0.02f;

        Vector3 left = center - side * lineWidth;
        Vector3 right = center + side * lineWidth;

        line.SetPosition(0, left);
        line.SetPosition(1, right);
    }
}