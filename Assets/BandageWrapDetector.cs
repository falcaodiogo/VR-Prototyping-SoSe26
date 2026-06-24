using UnityEngine;

public class BandageWrapDetector : MonoBehaviour
{
    [Header("Wound / Limb reference")]
    public Transform woundAxis; // its local "up" or "forward" should align with limb length
    public float captureRadius = 0.15f; // how close the bandage needs to stay to count

    [Header("Wrap settings")]
    public float requiredWrapAngle = 360f; // degrees of total rotation needed
    public float maxAngleStepPerFrame = 90f; // ignore jumps bigger than this (anti-cheat / teleport grab)

    [Header("Result")]
    public GameObject finalAppliedBandagePrefab;
    public Transform attachPointOnLimb; // where the finished bandage should snap to

    Transform trackedObject; // the bandage currently being wrapped
    float accumulatedAngle = 0f;
    Vector3 lastDir;
    bool isWrapping = false;
    bool isComplete = false;

    public void BeginWrap(Transform bandageTransform)
    {
        if (isComplete) return;
        trackedObject = bandageTransform;
        accumulatedAngle = 0f;
        isWrapping = true;
        lastDir = GetProjectedDir(bandageTransform.position);
        Debug.Log("begin wrap");

    }

    public void EndWrap()
    {
        isWrapping = false;
        trackedObject = null;
        Debug.Log("end wrap");

    }

    void Update()
    {
        if (!isWrapping || trackedObject == null || isComplete) return;

        // Check it's still near the limb
        float distFromAxis = Vector3.Cross(woundAxis.forward, trackedObject.position - woundAxis.position).magnitude;
        if (distFromAxis > captureRadius)
            return; // too far from the limb to count as wrapping

        Vector3 currentDir = GetProjectedDir(trackedObject.position);
        float stepAngle = Vector3.SignedAngle(lastDir, currentDir, woundAxis.forward);

        if (Mathf.Abs(stepAngle) <= maxAngleStepPerFrame)
            accumulatedAngle += Mathf.Abs(stepAngle);

        lastDir = currentDir;

        if (accumulatedAngle >= requiredWrapAngle)
            CompleteWrap();
    }

    Vector3 GetProjectedDir(Vector3 worldPos)
    {
        Vector3 offset = worldPos - woundAxis.position;
        Vector3 projected = Vector3.ProjectOnPlane(offset, woundAxis.forward);
        return projected.normalized;
    }

    void CompleteWrap()
    {
        isComplete = true;
        isWrapping = false;

        if (trackedObject != null)
            trackedObject.gameObject.SetActive(false); // remove the loose strip being held

        if (finalAppliedBandagePrefab != null && attachPointOnLimb != null)
        {
            Instantiate(finalAppliedBandagePrefab, attachPointOnLimb.position, attachPointOnLimb.rotation, attachPointOnLimb);
        }

        Debug.Log("Bandage wrap complete!");
    }
}