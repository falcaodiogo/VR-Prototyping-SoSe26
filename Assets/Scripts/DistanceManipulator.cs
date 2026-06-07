using UnityEngine;
using UnityEngine.InputSystem;


public class DistanceManipulator : MonoBehaviour
{
    public float moveSpeed = 2f;
       public InputActionProperty moveInput;
    private Transform controller;
    private bool isHeld = false;

 // CHANGE HERE: Take XRBaseInteractor
    public void StartManipulation(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor != null)
            controller = interactor.transform; // get transform of the interactor
        isHeld = true;;
    }

    public void StopManipulation(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor = null)
    {
        isHeld = false;
    }

    void Update()
    {
        if (!isHeld || controller == null || moveInput.action == null) 
        return;

                Vector2 inputVec = moveInput.action.ReadValue<Vector2>();
        float input = inputVec.y;

        if (Mathf.Abs(input) < 0.1f) return;

        transform.position += controller.forward * input * moveSpeed * Time.deltaTime;
    }
}