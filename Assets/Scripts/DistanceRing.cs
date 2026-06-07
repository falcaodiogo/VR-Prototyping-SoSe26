using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DistanceRing : MonoBehaviour
{
    [Header("Ring Settings")]
    public float radius = 10f;        // Use 10 for your current scaled scene (NOT 100)
    public int segments = 100;
    public float heightOffset = 0.02f;

    [Header("Visibility")]
    public bool showRing = false;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        // Line Renderer setup
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = segments;
        line.enabled = false;

        DrawCircle();
    }

    void Update()
    {
        // Toggle visibility
        line.enabled = showRing;

        // Keep slightly above ground (avoid flickering)
        transform.localPosition = new Vector3(0, heightOffset, 0);
    }

    void DrawCircle()
    {
        float angle = 0f;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;

            line.SetPosition(i, transform.position + new Vector3(x, 0, z));

            angle += 360f / segments;
        }
    }

    // Call this when triangle is grabbed
    public void ShowRing()
    {
        showRing = true;
    }

    // Call this when triangle is released
    public void HideRing()
    {
        showRing = false;
    }
}