using UnityEngine;

/// <summary>
/// Place on a trigger Collider positioned over the victim's chest.
/// Detects when a hand/controller enters or leaves the CPR compression zone,
/// and forwards the relevant transform to the CPRCompressionTracker.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CPRZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The compression tracker that should receive hand position updates.")]
    [SerializeField] private CPRCompressionTracker tracker;

    [Header("Filtering")]
    [Tooltip("Only objects with one of these tags will count as a valid 'hand' for CPR. " +
             "Leave empty to accept any collider that enters.")]
    [SerializeField] private string[] validHandTags = { "Hand", "Controller" };

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[CPRZone] Collider on {name} is not set to 'Is Trigger'. Forcing it on.");
            col.isTrigger = true;
        }
    }

    private bool IsValidHand(Collider other)
    {
        if (validHandTags == null || validHandTags.Length == 0)
            return true;

        foreach (var tag in validHandTags)
        {
            // Add this safety check to ignore empty array elements
            if (string.IsNullOrWhiteSpace(tag)) continue;

            if (other.CompareTag(tag)) return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CPRZone] OnTriggerEnter by {other.name}, tag={other.tag}, validHand={IsValidHand(other)}");
        if (!IsValidHand(other)) return;
        tracker.OnHandEnterZone(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidHand(other)) return;
        tracker.OnHandExitZone(other.transform);
    }
}