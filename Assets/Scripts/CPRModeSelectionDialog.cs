using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blocking dialog shown before a CPR attempt starts, letting the player choose
/// whether they want a background-music rhythm guide or the plain metronome tick.
/// Call <see cref="ShowDialog"/> whenever you want to (re)prompt the player
/// (e.g. once at scene start, or again before a retry).
/// </summary>
public class CPRModeSelectionDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup dialogGroup; // the whole dialog panel
    [SerializeField] private Button musicButton;
    [SerializeField] private Button metronomeButton;

    /// <summary>Fired once the player taps either button, with the mode they picked.</summary>
    public event Action<CPRSessionMode> OnModeChosen;

    private void Awake()
    {
        Hide();

        if (musicButton != null)
            musicButton.onClick.AddListener(() => Choose(CPRSessionMode.Music));

        if (metronomeButton != null)
            metronomeButton.onClick.AddListener(() => Choose(CPRSessionMode.Metronome));
    }

    public void ShowDialog()
    {
        if (dialogGroup == null) return;
        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (dialogGroup == null) return;
        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;
    }

    private void Choose(CPRSessionMode mode)
    {
        Hide();
        OnModeChosen?.Invoke(mode);
    }
}