using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BandageSnap : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isSnapped = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isSnapped && other.CompareTag("Wound"))
        {
            SnapToWound(other.transform);
        }
    }

    public void SnapToWound(Transform target)
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        var dispenser = GetComponent<BandageDispenser>();

        isSnapped = true;

        if (dispenser != null) dispenser.enabled = false; // freeze the cloth sim in its final shape
        if (grab != null) grab.enabled = false;
        if (rb != null) rb.isKinematic = true;

        transform.SetParent(target);
        transform.position = target.position;
        transform.rotation = target.rotation * Quaternion.Euler(90f, 0f, 0f);
        transform.position += target.forward * 0.002f;
    }
}