using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class BandageDispenser : MonoBehaviour
{
    public Transform exitPoint;
    public Transform tab;
    public float maxLength = 0.5f;
    public float stripWidth = 0.05f;
    public GameObject appliedBandagePrefab; // optional: only used if you still want a separate "applied to wound" object later

    public XRGrabInteractable tabGrabInteractable;
    public XRInteractionManager interactionManager;

    MeshFilter mf;
    Mesh stripMesh;
    bool isCut = false;

    Rigidbody stripRb;
    MeshCollider stripCollider;
    XRGrabInteractable stripGrab;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        stripMesh = new Mesh();
        mf.mesh = stripMesh;

        var existingBox = GetComponent<BoxCollider>();
        if (existingBox != null) existingBox.enabled = false; // only the strip's MeshCollider should be active, and only after cutting
    }

    void Update()
    {
        if (isCut || exitPoint == null || tab == null) return;

        float dist = Vector3.Distance(exitPoint.position, tab.position);
        UpdateStripMesh();

        if (dist >= maxLength)
            CutStrip();
    }

    public int lengthSegments = 10;
    public float sagAmount = 0.03f;   // how much it droops, scaled by slack
    public float thickness = 0.01f;   // gives it real volume

    void UpdateStripMesh()
    {
        Vector3 a = exitPoint.position;
        Vector3 b = tab.position;
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        if (dist < 0.0001f) return;
        dir /= dist;

        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Cross(dir, up).normalized;
        Vector3 normal = Vector3.Cross(right, dir).normalized;

        // sag more when slack (close together), less when pulled taut
        float taut = Mathf.Clamp01(dist / maxLength);
        float sag = sagAmount * (1f - taut);

        int n = lengthSegments;
        var verts = new Vector3[(n + 1) * 4]; // 4 verts per ring: top-left/right, bottom-left/right
        var uv = new Vector2[verts.Length];
        var trisList = new System.Collections.Generic.List<int>();

        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(a, b, t);
            p += -normal * sag * (4f * t * (1f - t));

            Vector3 r = right * stripWidth * 0.5f;
            Vector3 th = normal * thickness * 0.5f;

            int baseIdx = i * 4;
            verts[baseIdx + 0] = transform.InverseTransformPoint(p - r + th);
            verts[baseIdx + 1] = transform.InverseTransformPoint(p + r + th);
            verts[baseIdx + 2] = transform.InverseTransformPoint(p - r - th);
            verts[baseIdx + 3] = transform.InverseTransformPoint(p + r - th);

            uv[baseIdx + 0] = new Vector2(0, t);
            uv[baseIdx + 1] = new Vector2(1, t);
            uv[baseIdx + 2] = new Vector2(0, t);
            uv[baseIdx + 3] = new Vector2(1, t);

            if (i < n)
            {
                int b0 = baseIdx, b1 = baseIdx + 4;
                // top face
                trisList.AddRange(new[] { b0, b1, b0 + 1, b0 + 1, b1, b1 + 1 });
                // bottom face
                trisList.AddRange(new[] { b0 + 2, b0 + 3, b1 + 2, b0 + 3, b1 + 3, b1 + 2 });
                // side faces (left edge)
                trisList.AddRange(new[] { b0, b0 + 2, b1, b0 + 2, b1 + 2, b1 });
                // side faces (right edge)
                trisList.AddRange(new[] { b0 + 1, b1, b0 + 3, b1, b1 + 1, b0 + 3 });
            }
        }

        stripMesh.Clear();
        stripMesh.vertices = verts;
        stripMesh.triangles = trisList.ToArray();
        stripMesh.uv = uv;
        stripMesh.RecalculateNormals();
        stripMesh.RecalculateBounds();
    }

    void CutStrip()
    {
        isCut = true;
        gameObject.tag = "Bandage";

        // 1. Find whoever is currently holding the tab
        IXRSelectInteractor holdingInteractor = null;
        if (tabGrabInteractable != null && tabGrabInteractable.isSelected)
        {
            holdingInteractor = tabGrabInteractable.interactorsSelecting.Count > 0
                ? tabGrabInteractable.interactorsSelecting[0]
                : null;
        }

        // 2. Set up the strip's physics/grab BEFORE touching selection state
        stripCollider = GetComponent<MeshCollider>();
        if (stripCollider == null) stripCollider = gameObject.AddComponent<MeshCollider>();
        stripCollider.sharedMesh = stripMesh;
        stripCollider.convex = true;

        stripRb = GetComponent<Rigidbody>();
        if (stripRb == null) stripRb = gameObject.AddComponent<Rigidbody>();
        stripRb.useGravity = true;
        stripRb.isKinematic = false;
        stripRb.linearDamping = 0.5f;
        stripRb.angularDamping = 0.5f;

        stripGrab = GetComponent<XRGrabInteractable>();
        if (stripGrab == null) stripGrab = gameObject.AddComponent<XRGrabInteractable>();
        stripGrab.throwOnDetach = true;
        stripGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        // 3. Create a permanent attach point so we can safely delete the tab
        if (tab != null)
        {
            GameObject attachPoint = new GameObject("BandageAttachPoint");
            attachPoint.transform.SetParent(transform, false);

            // Copy the exact position and rotation of the tab
            attachPoint.transform.position = tab.position;
            attachPoint.transform.rotation = tab.rotation;

            // Assign this new empty object as the grab anchor
            stripGrab.attachTransform = attachPoint.transform;
        }

        // 4. Hand off the grab WHILE the tab is still selected/enabled
        if (holdingInteractor != null && interactionManager != null)
        {
            interactionManager.SelectExit(holdingInteractor, tabGrabInteractable);
            interactionManager.SelectEnter(holdingInteractor, stripGrab);
        }

        // 5. Safely destroy the tab completely
        if (tab != null)
        {
            Destroy(tab.gameObject);
        }
    }
}