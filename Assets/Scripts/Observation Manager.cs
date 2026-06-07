using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ObservationManager : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI logText;
    public TextMeshProUGUI counterText;

    [Header("Sequence Settings")]
    public float delayBetweenNotes = 10f; // Seconds to wait between each note
    
    // List of notes to show automatically
    private List<string> observationSequence = new List<string>()
    {
        "SCANNING: Accident Zone analysed.",
        "HAZARD: Active engine smoke detected.",
        "IMPACT: Severe car-bicycle collision.",
        "VICTIM: Unresponsive female identified.",
        "SURVEY COMPLETE: Begin Triage.",
        "<color=green>Next Step: Secure the scene and call for backup.</color>" 
    };

    private int currentIndex = 0;
    private string displayString = "SYSTEM INITIALIZING...\n"; 

    void Start()
    {
        logText.text = displayString;
        // Start the automatic timer
        StartCoroutine(AutoPopulateNotes());
    }

    IEnumerator AutoPopulateNotes()
    {
        // Wait 3 seconds at the very start for the player to get settled
        yield return new WaitForSeconds(3f);

        while (currentIndex < observationSequence.Count)
        {
            // Add the next note from the list
            AddNote(observationSequence[currentIndex]);
            currentIndex++;

            // Wait for the specified delay (10 seconds) before the next one
            yield return new WaitForSeconds(delayBetweenNotes);
        }
    }

    void AddNote(string note)
    {
        displayString += "- " + note + "\n";
        logText.text = displayString;
        
        if (counterText != null)
            counterText.text = "Findings: " + currentIndex + " / " + observationSequence.Count;

        // Play a notification sound so the player looks at the tablet
        if (GetComponent<AudioSource>()) GetComponent<AudioSource>().Play();
    }
}