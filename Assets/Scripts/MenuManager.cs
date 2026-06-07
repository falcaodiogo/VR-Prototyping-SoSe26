using UnityEngine;
using UnityEngine.SceneManagement; // Allows us to change scenes
using UnityEngine.UI; // For UI elements like sliders and toggles

public class MenuManager : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject MainMenuCanvas;
    public GameObject InstructionPanel; // Drag your new Instruction Panel here
    public GameObject SettingsPanel; // Drag your Settings Panel here

    [Header("Audio Settings")]
    public Slider volumeSlider; // Drag your Volume Slider here

    void Start()
    {
        // Ensure instructions are hidden when the scene starts
        if (InstructionPanel != null)
            InstructionPanel.SetActive(false);
    }

    // --- NEW INSTRUCTION FUNCTIONS ---
    
    public void OpenInstructions()
    {
        if (InstructionPanel != null)
        {
            InstructionPanel.SetActive(true);
            // Optionally hide the main buttons so they don't overlap
            // MainMenuCanvas.SetActive(false); 
        }
    }

    public void CloseInstructions()
    {
        if (InstructionPanel != null)
        {
            InstructionPanel.SetActive(false);
            // Show the main menu buttons again
            // MainMenuCanvas.SetActive(true);
        }
    }


// --- NEW SETTINGS FUNCTIONS ---

    public void OpenSettings()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume; // Globally changes game volume
    }

    public void ToggleSnapTurn(bool isSnap)
    {
        // This will print to your console for now to prove it works
        Debug.Log("Snap Turn Toggled: " + isSnap);
        // Link this to your XR Origin Snap Turn Provider later if needed
    }

    // --- SCENE NAVIGATION FUNCTIONS ---


    // --- EXISTING FUNCTIONS ---

    // 1. USE THIS FOR THE 'START TRAINING' BUTTON
    public void StartTraining()
    {
        Debug.Log("Loading Training Scene...");
        // Loads the scene at Index 1 in Build Settings
        SceneManager.LoadScene(1); 
    }

    // 2. USE THIS FOR THE 'EXIT' BUTTON (TO GO BACK TO MAIN MENU)
    public void GoToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        // Loads the scene at Index 0 in Build Settings
        SceneManager.LoadScene(0); 
    }

    // 3. USE THIS TO CLOSE THE GAME COMPLETELY
    public void ExitApp()
    {
        Debug.Log("Closing Application...");

        #if UNITY_EDITOR
        // This stops the game if you are testing in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // This closes the app on the Quest headset
        Application.Quit();
        #endif
    }

//added for testing purposes to navigate between scenes without going back to the main menu
    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        // Check if we are not at the last scene
        if (currentSceneIndex < SceneManager.sceneCountInBuildSettings - 1)
        {
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
    }

    public void LoadPreviousScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        // Check if we are not at the first scene (Main Menu)
        if (currentSceneIndex > 0)
        {
            SceneManager.LoadScene(currentSceneIndex - 1);
        }
    }
}

