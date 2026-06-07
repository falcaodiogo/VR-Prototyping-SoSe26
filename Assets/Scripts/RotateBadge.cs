using UnityEngine;

public class RotateBadge : MonoBehaviour
{
    void Update()
    {
        // Slowly rotates the medal to make it look high-quality
        transform.Rotate(Vector3.up * 50 * Time.deltaTime);
    }
}