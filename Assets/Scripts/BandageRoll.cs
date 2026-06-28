using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BandageRoll : MonoBehaviour
{
    [SerializeField] private Transform tipTransform; // The BandageTip object
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float maxUnrollLength = 2.0f;

    private void Update()
    {
        // Calculate distance between the Roll (Hand 1) and the Tip (Hand 2)
        float distance = Vector3.Distance(transform.position, tipTransform.position);

        // Clamp the distance so the bandage doesn't stretch infinitely
        float currentLength = Mathf.Clamp(distance, 0, maxUnrollLength);

        // Update LineRenderer
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, tipTransform.position);
        
        // Visual feedback: If fully unrolled, maybe enable a "cuttable" state
        if (currentLength >= maxUnrollLength)
        {
            // Logic to signal readiness to cut
        }
    }
}