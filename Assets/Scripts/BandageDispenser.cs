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
    public GameObject appliedBandagePrefab;

    [Tooltip("The XRGrabInteractable on the tab itself, used to find which hand is holding it.")]
    public XRGrabInteractable tabGrabInteractable;

    [Tooltip("Reference to the scene's XR Interaction Manager.")]
    public XRInteractionManager interactionManager;

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

        Vector3 p0 = exitPoint.position - right;
        Vector3 p1 = exitPoint.position + right;
        Vector3 p2 = tab.position - right;
        Vector3 p3 = tab.position + right;

        Vector3[] verts = { p0, p1, p2, p3, p0, p1, p2, p3 };

        int[] tris = {
            0, 2, 1,   1, 2, 3,
            5, 6, 4,   7, 6, 5
        };

        Vector2[] uv = {
            new(0,0), new(1,0), new(0,1), new(1,1),
            new(0,0), new(1,0), new(0,1), new(1,1)
        };

        stripMesh.Clear();
        stripMesh.vertices = verts;
        stripMesh.triangles = tris;
        stripMesh.uv = uv;
        stripMesh.RecalculateNormals();
    }

    void CutStrip()
    {
        isCut = true;

        // Figure out who's currently holding the tab BEFORE we deactivate anything
        IXRSelectInteractor holdingInteractor = null;
        if (tabGrabInteractable != null && tabGrabInteractable.isSelected)
        {
            // grab the first interactor currently selecting the tab
            holdingInteractor = tabGrabInteractable.interactorsSelecting.Count > 0
                ? tabGrabInteractable.interactorsSelecting[0]
                : null;
        }

        if (appliedBandagePrefab != null)
        {
            Vector3 midPoint = (exitPoint.position + tab.position) / 2f;
            GameObject appliedBandage = Instantiate(appliedBandagePrefab, midPoint, tab.rotation);

            // Hand the grab off to whoever was holding the tab
            if (holdingInteractor != null && interactionManager != null)
            {
                var appliedGrab = appliedBandage.GetComponent<XRGrabInteractable>();
                if (appliedGrab != null)
                {
                    // Force-release the tab first, then select the new object with the same interactor
                    interactionManager.SelectExit(holdingInteractor, tabGrabInteractable);
                    interactionManager.SelectEnter(holdingInteractor, appliedGrab);
                }
            }
        }

        gameObject.SetActive(false); // hide the stretching strip
        tab.gameObject.SetActive(false); // hide the tab too (optional)
    }
}