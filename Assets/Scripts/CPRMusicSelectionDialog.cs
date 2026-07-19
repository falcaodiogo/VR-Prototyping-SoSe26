using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blocking dialog shown between Round 1 and Round 2, letting the player pick
/// which of 3 music tracks plays as the rhythm guide during the music round.
/// Purely a picker — CPRSessionManager owns what happens with the choice.
/// </summary>
public class CPRMusicSelectionDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup dialogGroup; // the whole dialog panel
    [SerializeField] private Button track1Button;
    [SerializeField] private Button track2Button;
    [SerializeField] private Button track3Button;

    [Header("Tracks")]
    [SerializeField] private AudioClip track1Clip;
    [SerializeField] private AudioClip track2Clip;
    [SerializeField] private AudioClip track3Clip;

    /// <summary>Fired once the player taps a track button, with the clip they picked.</summary>
    public event Action<AudioClip> OnMusicChosen;

    private void Awake()
    {
        Hide();

        if (track1Button != null)
            track1Button.onClick.AddListener(() => Choose(track1Clip));

        if (track2Button != null)
            track2Button.onClick.AddListener(() => Choose(track2Clip));

        if (track3Button != null)
            track3Button.onClick.AddListener(() => Choose(track3Clip));
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

    private void Choose(AudioClip clip)
    {
        Hide();
        OnMusicChosen?.Invoke(clip);
    }
}