using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BandageSnap : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isSnapped = false;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isSnapped && other.CompareTag("Wound"))
        ///&& other.CompareTag("KneeWound") && other.CompareTag("Wound_Knee") && other.CompareTag("Wound_Hand") && other.CompareTag("Forehead_Wound")
        {
            SnapToWound(other.transform);
        }
    }

    void SnapToWound(Transform target)

{
    isSnapped = true;

    if (grab != null) grab.enabled = false;

    if (rb != null) rb.isKinematic = true;

    // Parent to wound
    transform.SetParent(target);

    // Match position
    transform.position = target.position;

    // Match rotation automatically
    transform.rotation = target.rotation * Quaternion.Euler(90f, 0f, 0f);

    // Slight offset to sit on skin properly
    transform.position += target.forward * 0.002f;


}

    // Force local rotation to zero to match the parent's orientation
    // If the cloth is still vertical, try (90, 0, 0) to flip it flat
  //  transform.localRotation = Quaternion.Euler(-83.331f, 0, 0); //original

}
