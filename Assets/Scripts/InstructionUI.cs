using UnityEngine;
using TMPro;

public class InstructionUI : MonoBehaviour
{
    public TextMeshProUGUI instructionText;

    public void SetInstruction(string text)
    {
        instructionText.text = text;
    }
}