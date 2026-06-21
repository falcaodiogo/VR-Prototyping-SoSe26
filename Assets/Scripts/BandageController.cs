using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; 

public class BandageController : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        // Get the interactable component attached to this object
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        // Subscribe to the grab and release events
        grabInteractable.selectEntered.AddListener(OnBandageTipGrabbed);
        grabInteractable.selectExited.AddListener(OnBandageTipReleased);
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        grabInteractable.selectEntered.RemoveListener(OnBandageTipGrabbed);
        grabInteractable.selectExited.RemoveListener(OnBandageTipReleased);
    }

    private void OnBandageTipGrabbed(SelectEnterEventArgs args)
    {
        // 'args.interactorObject' tells you WHICH hand/controller grabbed it
        Debug.Log($"Bandage tip grabbed by: {args.interactorObject.transform.name}");
        
        // Add your unrolling or application logic here!
    }

    private void OnBandageTipReleased(SelectExitEventArgs args)
    {
        Debug.Log("Bandage tip released!");
    }
}   