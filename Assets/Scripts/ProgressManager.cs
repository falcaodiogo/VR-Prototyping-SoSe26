using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProgressManager : MonoBehaviour
{
    public Color activeColor = Color.yellow;
    public Color doneColor = Color.green;
    public Color lockedColor = Color.grey;
    
    public Image[] stepIcons; // Drag your 7 images here

    void Start()
    {
        // Scene indices based on your build settings (0=Menu, 1=Accident, etc.)
        int currentScene = SceneManager.GetActiveScene().buildIndex;

        for (int i = 0; i < stepIcons.Length; i++)
        {
            int stepNumber = i + 1; // Icons are 0-6, Scenes are 1-7

            if (stepNumber < currentScene) {
                stepIcons[i].color = doneColor; // Past steps
            } 
            else if (stepNumber == currentScene) {
                stepIcons[i].color = activeColor; // Current step
            } 
            else {
                stepIcons[i].color = lockedColor; // Future steps
            }
        }
    }
}