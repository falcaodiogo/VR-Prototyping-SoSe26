using UnityEngine;
using TMPro;
using System.Collections;

public class PhoneInteraction : MonoBehaviour
{
    public GameObject bubblePanel;
    public TextMeshProUGUI bubbleText;
    
    private int dialogueIndex = 0; // Keeps track of how many times you reported status

    public void CallEmergency()
    {
        bubblePanel.SetActive(true);
        dialogueIndex = 0; // Reset
        bubbleText.text = "<color=#FFCC00>Operator:</color> 112 Emergency. What is your location?";
    }

    // NEW: Function for the Second Button (Status Report)
    public void SendStatusReport()
    {
        if (!bubblePanel.activeSelf) return; // Don't do anything if call hasn't started

        dialogueIndex++; // Move to the next part of the conversation

        if (dialogueIndex == 1)
        {
            bubbleText.text = "<color=#00FF00>You:</color> My name is McCgrey. I'm at the accident scene on Main Road, near the Auto Workshop,Street 5. \n\n<color=#FFCC00>Operator:</color> Understood. What is the condition of the victim?";
        }
        else if (dialogueIndex == 2)
        {
            bubbleText.text = "<color=#00FF00>You:</color> The victim is <b>unconscious but breathing</b>. I see <b>visible lacerations</b> on the arm and <b>minor bleeding</b> from the knee </b>.\n\n<color=#FFCC00>Operator:</color> Copy that. Help is on the way. ETA is 5 minutes.";
        }
        else if (dialogueIndex == 3)
        {
            bubbleText.text = "<color=#FFCC00>Operator:</color> Do not move the victim. Monitor <b>vital signs</b>. Ensure the <b>Accident Area is secured</b> for the ambulance.";

        }
        else if (dialogueIndex == 4)
        {
            // Ending the call
            bubbleText.text = "<color=#00FF00>Call ended. Proceed to check on victim.</i>";
            // Optional: Invoke an event to unlock the 'Next Step' button here
        }
    }
}