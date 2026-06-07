using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreDetailsText;

    [Header("Trophy Assets")]
    public GameObject goldCup;    // Drag your FastMesh Gold Trophy here
    public GameObject silverCup;  // Drag your FastMesh Silver Trophy here
    public GameObject bronzeMedal; // Drag your FastMesh Medal here

    [Header("Effects")]
    public GameObject confettiParticles;
    public AudioSource victorySource;

    // For your thesis, we assume 6/6 is Gold, 4-5 is Silver, <4 is Bronze
    // You can pass this value from your previous scene or set it for testing
    private int finalScore = 6; 

    void Start()
    {
        ShowFinalResults(finalScore);
    }

    void ShowFinalResults(int score)
    {
        // 1. Set the main message
        resultText.text = "TRAINING COMPLETE";
        scoreDetailsText.text = "Rescue Chain Accuracy: " + score + " / 6 Steps";

        // 2. Hide all trophies first to be safe
        goldCup.SetActive(false);
        silverCup.SetActive(false);
        bronzeMedal.SetActive(false);

        // 3. Logic to show the specific 3D asset
        if (score >= 6)
        {
            goldCup.SetActive(true);
            resultText.text = "EXCELLENT WORK!";
            if(confettiParticles) confettiParticles.SetActive(true);
        }
        else if (score >= 4)
        {
            silverCup.SetActive(true);
            resultText.text = "GREAT EFFORT!";
        }
        else
        {
            bronzeMedal.SetActive(true);
            resultText.text = "TRAINING FINISHED";
        }

        // 4. Play Victory Sound
        if (victorySource != null)
        {
            victorySource.Play();
        }
    }

    // --- Button Functions ---

    public void RestartGame()
    {
        // Loads Scene 1 (Accident Scene)
        SceneManager.LoadScene(1);
    }

    public void GoToMainMenu()
    {
        // Loads Scene 0 (Main Menu)
        SceneManager.LoadScene(0);
    }
}