using UnityEngine;

public class WoundTriggerZone : MonoBehaviour
{
    public BandageWrapDetector wrapDetector;
    public string bandageTag = "Bandage";

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name} | Tag: {other.gameObject.tag}");

        if (other.CompareTag(bandageTag))
            wrapDetector.BeginWrap(other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(bandageTag))
            wrapDetector.EndWrap();
    }
}