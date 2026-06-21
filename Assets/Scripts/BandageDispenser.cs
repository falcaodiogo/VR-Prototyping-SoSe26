using UnityEngine;

public class BandageDispenser : MonoBehaviour
{
    public Transform exitPoint;
    public Transform tab;
    public float maxLength = 0.5f;
    public float stripWidth = 0.05f;
    public GameObject appliedBandagePrefab;

    MeshFilter mf;
    Mesh stripMesh;
    bool isCut = false;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        stripMesh = new Mesh();
        mf.mesh = stripMesh;
    }

    void Update()
    {
        if (isCut || exitPoint == null || tab == null) return;

        float dist = Vector3.Distance(exitPoint.position, tab.position);
        UpdateStripMesh();

        if (dist >= maxLength)
            CutStrip();
    }

    void UpdateStripMesh()
    {
        Vector3 dir = (tab.position - exitPoint.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.95f) up = Vector3.forward;
        Vector3 right = Vector3.Cross(dir, up).normalized * stripWidth * 0.5f;

        Vector3[] verts = {
            exitPoint.position - right,
            exitPoint.position + right,
            tab.position - right,
            tab.position + right
        };
        int[] tris = { 0, 2, 1, 1, 2, 3, 0,1,2, 1,3,2 }; // double-sided
        Vector2[] uv = { new(0,0), new(1,0), new(0,1), new(1,1) };

        stripMesh.Clear();
        stripMesh.vertices = verts;
        stripMesh.triangles = tris;
        stripMesh.uv = uv;
        stripMesh.RecalculateNormals();
    }

    void CutStrip()
    {
        isCut = true;

        if (appliedBandagePrefab != null)
        {
            Vector3 midPoint = (exitPoint.position + tab.position) / 2f;
            Instantiate(appliedBandagePrefab, midPoint, tab.rotation);
        }

        gameObject.SetActive(false); // hide the stretching strip
        tab.gameObject.SetActive(false); // hide the tab too (optional)
    }
}