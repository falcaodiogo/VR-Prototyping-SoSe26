using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class BandageDispenser : MonoBehaviour
{
    [Header("Anchors")]
    public Transform exitPoint;
    public Transform tab;
    public float maxLength = 0.5f;
    public float stripWidth = 0.05f;
    public GameObject appliedBandagePrefab;

    [Header("XR")]
    public XRGrabInteractable tabGrabInteractable;
    public XRInteractionManager interactionManager;

    [Header("Mesh")]
    public int lengthSegments = 20;
    public float thickness = 0.01f;

    [Header("Cloth-like Sway (Verlet Rope)")]
    public float gravity = -2.0f;
    public float damping = 0.98f;
    [Range(1, 12)] public int constraintIterations = 6;

    MeshFilter mf;
    Mesh stripMesh;

    enum StripState { Dispensing, Cut }
    StripState state = StripState.Dispensing;

    Rigidbody stripRb;
    XRGrabInteractable stripGrab;
    Transform heldInteractorTransform; // whoever is currently holding the CUT strip

    Vector3[] points;
    Vector3[] prevPoints;
    float segmentLength;
    bool simInitialized = false;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        stripMesh = new Mesh();
        mf.mesh = stripMesh;

        var existingBox = GetComponent<BoxCollider>();
        if (existingBox != null) existingBox.enabled = false;
    }

    void InitSim()
    {
        int n = lengthSegments + 1;
        points = new Vector3[n];
        prevPoints = new Vector3[n];
        segmentLength = maxLength / lengthSegments;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)lengthSegments;
            Vector3 p = Vector3.Lerp(exitPoint.position, tab.position, t);
            points[i] = p;
            prevPoints[i] = p;
        }
        simInitialized = true;
    }

    void FixedUpdate()
    {
        if (state == StripState.Dispensing)
        {
            if (exitPoint == null || tab == null) return;
            if (!simInitialized) InitSim();
        }
        else if (!simInitialized) return;

        SimulateRope(Time.fixedDeltaTime);
    }

    void Update()
    {
        if (state == StripState.Dispensing && (exitPoint == null || tab == null)) return;
        if (!simInitialized) return;

        BuildMeshFromPoints();

        if (state == StripState.Dispensing)
        {
            float dist = Vector3.Distance(exitPoint.position, tab.position);
            if (dist >= maxLength)
                CutStrip();
        }
    }

    void SimulateRope(float dt)
    {
        int n = points.Length;

        // Integrate every free point
        for (int i = 1; i < n - 1; i++)
        {
            Vector3 current = points[i];
            Vector3 velocity = (current - prevPoints[i]) * damping;
            Vector3 next = current + velocity + Vector3.up * gravity * dt * dt;
            prevPoints[i] = current;
            points[i] = next;
        }

        bool nearPinned;
        bool farPinned;

        if (state == StripState.Dispensing)
        {
            // Still on the roll: near end pinned to the dispenser, far end to the tab
            points[0] = exitPoint.position;
            prevPoints[0] = exitPoint.position;
            points[n - 1] = tab.position;
            prevPoints[n - 1] = tab.position;
            nearPinned = true;
            farPinned = true;
        }
        else
        {
            // Cut: near end is now loose — this is what gives it real cloth inertia.
            // Far end follows whichever hand is actually holding it right now.
            nearPinned = false;
            farPinned = heldInteractorTransform != null;
            if (farPinned)
            {
                points[n - 1] = heldInteractorTransform.position;
                prevPoints[n - 1] = heldInteractorTransform.position;
            }
        }

        for (int iter = 0; iter < constraintIterations; iter++)
        {
            for (int i = 0; i < n - 1; i++)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = points[i + 1];
                Vector3 delta = p1 - p0;
                float dist = delta.magnitude;
                if (dist < 0.0001f) continue;
                float diff = (dist - segmentLength) / dist;

                bool p0Fixed = nearPinned && i == 0;
                bool p1Fixed = farPinned && (i + 1 == n - 1);
                if (p0Fixed && p1Fixed) continue;

                if (p0Fixed) points[i + 1] -= delta * diff;
                else if (p1Fixed) points[i] += delta * diff;
                else
                {
                    points[i] += delta * diff * 0.5f;
                    points[i + 1] -= delta * diff * 0.5f;
                }
            }
        }
    }

    void BuildMeshFromPoints()
    {
        int n = points.Length;
        var verts = new Vector3[n * 4];
        var uv = new Vector2[verts.Length];
        var trisList = new List<int>(n * 12);

        for (int i = 0; i < n; i++)
        {
            Vector3 p = points[i];

            Vector3 tangent;
            if (i == 0) tangent = (points[1] - points[0]).normalized;
            else if (i == n - 1) tangent = (points[n - 1] - points[n - 2]).normalized;
            else tangent = (points[i + 1] - points[i - 1]).normalized;
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.forward;

            Vector3 upRef = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(tangent, upRef).normalized;
            Vector3 normal = Vector3.Cross(right, tangent).normalized;

            Vector3 r = right * stripWidth * 0.5f;
            Vector3 th = normal * thickness * 0.5f;

            int baseIdx = i * 4;
            verts[baseIdx + 0] = transform.InverseTransformPoint(p - r + th);
            verts[baseIdx + 1] = transform.InverseTransformPoint(p + r + th);
            verts[baseIdx + 2] = transform.InverseTransformPoint(p - r - th);
            verts[baseIdx + 3] = transform.InverseTransformPoint(p + r - th);

            float t = i / (float)(n - 1);
            uv[baseIdx + 0] = new Vector2(0, t);
            uv[baseIdx + 1] = new Vector2(1, t);
            uv[baseIdx + 2] = new Vector2(0, t);
            uv[baseIdx + 3] = new Vector2(1, t);

            if (i < n - 1)
            {
                int b0 = baseIdx, b1 = baseIdx + 4;
                trisList.AddRange(new[] { b0, b1, b0 + 1, b0 + 1, b1, b1 + 1 });
                trisList.AddRange(new[] { b0 + 2, b0 + 3, b1 + 2, b0 + 3, b1 + 3, b1 + 2 });
                trisList.AddRange(new[] { b0, b0 + 2, b1, b0 + 2, b1 + 2, b1 });
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
        state = StripState.Cut;
        gameObject.tag = "Bandage";

        IXRSelectInteractor holdingInteractor = null;
        if (tabGrabInteractable != null && tabGrabInteractable.isSelected)
        {
            holdingInteractor = tabGrabInteractable.interactorsSelecting.Count > 0
                ? tabGrabInteractable.interactorsSelecting[0]
                : null;
        }

        // Small trigger handle instead of a big padded box — this is what was
        // physically colliding with the environment and fighting the grab force.
        BoxCollider grabCollider = GetComponent<BoxCollider>();
        if (grabCollider == null) grabCollider = gameObject.AddComponent<BoxCollider>();
        grabCollider.enabled = true;
        grabCollider.isTrigger = true;
        grabCollider.center = Vector3.zero;
        grabCollider.size = new Vector3(stripWidth * 3f, 0.05f, 0.05f);

        stripRb = GetComponent<Rigidbody>();
        if (stripRb == null) stripRb = gameObject.AddComponent<Rigidbody>();
        stripRb.useGravity = false;
        stripRb.isKinematic = true; // no physics forces — movement comes from Instantaneous grab + our own sim
        stripRb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        stripGrab = GetComponent<XRGrabInteractable>();
        if (stripGrab == null) stripGrab = gameObject.AddComponent<XRGrabInteractable>();
        stripGrab.throwOnDetach = false;
        stripGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;

        stripGrab.colliders.Clear();
        stripGrab.colliders.Add(grabCollider);

        stripGrab.selectEntered.AddListener(OnStripGrabbed);
        stripGrab.selectExited.AddListener(OnStripReleased);

        if (holdingInteractor != null && interactionManager != null)
        {
            heldInteractorTransform = holdingInteractor.transform;
            interactionManager.SelectExit(holdingInteractor, tabGrabInteractable);
            interactionManager.SelectEnter(holdingInteractor, stripGrab);
        }

        if (tab != null) Destroy(tab.gameObject);
    }

    void OnStripGrabbed(SelectEnterEventArgs args)
    {
        heldInteractorTransform = args.interactorObject.transform;
    }

    void OnStripReleased(SelectExitEventArgs args)
    {
        heldInteractorTransform = null;
    }
}