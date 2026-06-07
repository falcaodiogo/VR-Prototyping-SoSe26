using UnityEngine;
using UnityEngine.UI;

public class ProgressSystem : MonoBehaviour
{
    public Slider progressBar;

    public void AddProgress(float value)
    {
        progressBar.value += value;
    }
}