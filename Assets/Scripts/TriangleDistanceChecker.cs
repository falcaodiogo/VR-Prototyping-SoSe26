using UnityEngine;
using TMPro;

public class TriangleDistanceChecker : MonoBehaviour
{
    public Transform victim;
    public TextMeshProUGUI distanceText;

    public float safeDistance = 100f;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, victim.position);

        distanceText.text = "Distance: " + distance.ToString("F1") + " m";

        if (distance < safeDistance - 10)
        {
            distanceText.color = Color.red;
        }
        else if (distance > safeDistance + 10)
        {
            distanceText.color = Color.yellow;
        }
        else
        {
            distanceText.color = Color.green;
        }
    }
}