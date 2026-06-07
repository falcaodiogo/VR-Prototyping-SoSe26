using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Transform cameraTransform;

    void LateUpdate()
    {
        transform.LookAt(cameraTransform);
        transform.Rotate(0, 180, 0);
    }
}