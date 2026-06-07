using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float time;

    void Update()
    {
        time += Time.deltaTime;
        timerText.text = "Time: " + Mathf.FloorToInt(time);
    }
}