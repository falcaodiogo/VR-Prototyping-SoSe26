using UnityEngine;

/// <summary>
/// Orchestrates the CPR mini-game flow:
///
///   1. BeepPhase       - 30 compressions, metronome tick guide on (only
///                        while hand is in the zone).
///   2. ShowingSummary1  - Round 1 result shown alone (no music round).
///   3. ChoosingMusic    - blocking dialog: player picks 1 of 3 tracks for
///                        the music round.
///   4. MusicPhase       - next 30 compressions, chosen track plays instead
///                        of the beep guide.
///   5. ShowingSummary2  - Round 2 result shown alone (music round), same
///                        format as Round 1's summary.
///   6. Finished         - comparison screen: both rounds side by side plus
///                        which one scored higher.
///
/// This is the single place that decides when the metronome beep should be
/// audible and when the music should play, so CPRFeedbackUI and
/// CPRCompressionTracker stay dumb/reusable.
/// </summary>
public class CPRSessionManager : MonoBehaviour
{
    public enum Phase
    {
        BeepPhase,
        ShowingSummary1,
        ChoosingMusic,
        MusicPhase,
        ShowingSummary2,
        Finished,
        SessionEnded
    }

    [Header("References")]
    [SerializeField] private CPRCompressionTracker tracker;
    [SerializeField] private CPRFeedbackUI feedbackUI;
    [SerializeField] private CPRMusicSelectionDialog musicSelectionDialog;

    [Header("Music (Phase 2)")]
    [SerializeField] private AudioSource musicAudioSource;

    [Header("Phase Settings")]
    [Tooltip("Compressions required to complete each phase.")]
    [SerializeField] private int compressionsPerPhase = 30;
    [Tooltip("How long each round's own summary stays up before moving on.")]
    [SerializeField] private float summaryDisplayDuration = 5f;
    [Tooltip("How long the final comparison screen stays up at the very end.")]
    [SerializeField] private float finalSummaryDisplayDuration = 30f;

    private const string Round1Label = "Round 1 - No Music";
    private const string Round2Label = "Round 2 - With Music";

    public Phase CurrentPhase { get; private set; }

    private float summaryTimer;
    private CPRSessionSummary phase1Summary;
    private CPRSessionSummary phase2Summary;

    private void OnEnable()
    {
        if (tracker == null)
        {
            Debug.LogError("[CPRSessionManager] 'Tracker' is not assigned in the Inspector — " +
                            "phase transitions (summary, music) will never trigger.", this);
            return;
        }
        if (feedbackUI == null)
        {
            Debug.LogError("[CPRSessionManager] 'Feedback UI' is not assigned in the Inspector.", this);
            return;
        }
        if (musicSelectionDialog == null)
        {
            Debug.LogError("[CPRSessionManager] 'Music Selection Dialog' is not assigned in the Inspector — " +
                            "the session will stall after Round 1.", this);
            return;
        }

        tracker.OnHandInZoneChanged += HandleHandInZoneChanged;
        tracker.OnCompressionCountChanged += HandleCompressionCountChanged;
        musicSelectionDialog.OnMusicChosen += HandleMusicChosen;
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnHandInZoneChanged -= HandleHandInZoneChanged;
            tracker.OnCompressionCountChanged -= HandleCompressionCountChanged;
        }
        if (musicSelectionDialog != null)
        {
            musicSelectionDialog.OnMusicChosen -= HandleMusicChosen;
        }
    }

    private void Start()
    {
        if (tracker == null || feedbackUI == null || musicSelectionDialog == null)
        {
            // Already logged in OnEnable — don't also crash Start().
            return;
        }
        BeginBeepPhase();
    }

    private void Update()
    {
        switch (CurrentPhase)
        {
            case Phase.ShowingSummary1:
                summaryTimer -= Time.deltaTime;
                if (summaryTimer <= 0f)
                    BeginChoosingMusic();
                break;

            case Phase.ShowingSummary2:
                summaryTimer -= Time.deltaTime;
                if (summaryTimer <= 0f)
                    BeginFinalComparison();
                break;

            case Phase.Finished:
                summaryTimer -= Time.deltaTime;
                if (summaryTimer <= 0f)
                {
                    feedbackUI.HideSummary();
                    CurrentPhase = Phase.SessionEnded;
                }
                break;
        }
    }

    // ---------------- Phase transitions ----------------

    private void BeginBeepPhase()
    {
        CurrentPhase = Phase.BeepPhase;
        tracker.ResetSession();
        feedbackUI.HideSummary();
        StopMusic();

        // If the hand happens to already be in the zone when this phase
        // starts, turn the guide on immediately; otherwise wait for entry.
        feedbackUI.SetMetronomeEnabled(tracker.IsHandInZone);
    }

    private void BeginShowSummary1()
    {
        CurrentPhase = Phase.ShowingSummary1;
        summaryTimer = summaryDisplayDuration;

        phase1Summary = tracker.GetSessionSummary();

        feedbackUI.SetMetronomeEnabled(false);
        feedbackUI.ShowSummary(phase1Summary, $"{Round1Label} Complete!");
    }

    private void BeginChoosingMusic()
    {
        CurrentPhase = Phase.ChoosingMusic;
        feedbackUI.HideSummary();
        musicSelectionDialog.ShowDialog();
    }

    private void HandleMusicChosen(AudioClip chosenClip)
    {
        if (CurrentPhase != Phase.ChoosingMusic) return;
        BeginMusicPhase(chosenClip);
    }

    private void BeginMusicPhase(AudioClip chosenClip)
    {
        CurrentPhase = Phase.MusicPhase;
        tracker.ResetSession();
        feedbackUI.HideSummary();
        feedbackUI.SetMetronomeEnabled(false); // no beep guide in this phase
        PlayMusic(chosenClip);
    }

    private void BeginShowSummary2()
    {
        CurrentPhase = Phase.ShowingSummary2;
        summaryTimer = summaryDisplayDuration;

        phase2Summary = tracker.GetSessionSummary();
        StopMusic();

        // Same shape as Round 1's summary: just compressions + accuracy,
        // for this round only.
        feedbackUI.ShowSummary(phase2Summary, $"{Round2Label} Complete!");
    }

    private void BeginFinalComparison()
    {
        CurrentPhase = Phase.Finished;
        summaryTimer = finalSummaryDisplayDuration;

        feedbackUI.SetMetronomeEnabled(false);
        StopMusic();

        float score1 = ComputeOverallScore(phase1Summary);
        float score2 = ComputeOverallScore(phase2Summary);

        string winnerLine;
        if (Mathf.Approximately(score1, score2))
        {
            winnerLine = $"Tied! Both rounds scored {score1:0}%";
        }
        else
        {
            string winnerLabel = score1 > score2 ? Round1Label : Round2Label;
            winnerLine = $"Higher score: {winnerLabel} ({Mathf.Max(score1, score2):0}%)";
        }

        feedbackUI.ShowComparison(phase1Summary, Round1Label, phase2Summary, Round2Label, winnerLine);
    }

    /// <summary>
    /// Blends "was the rate right" and "was the depth right" into one
    /// comparable score per round — the two things real CPR quality is
    /// actually judged on.
    /// </summary>
    private static float ComputeOverallScore(CPRSessionSummary summary)
    {
        float depthCompliance = summary.totalCompressions > 0
            ? (summary.totalCompressions - summary.notEnoughDepthCount) / (float)summary.totalCompressions * 100f
            : 0f;

        return (summary.GoodPercentage + depthCompliance) / 2f;
    }

    // ---------------- Tracker event handlers ----------------

    private void HandleHandInZoneChanged(bool entered)
    {
        // The beep is only ever a rhythm guide for phase 1, and only while
        // the hand is actually in the CPR zone.
        if (CurrentPhase == Phase.BeepPhase)
            feedbackUI.SetMetronomeEnabled(entered);
    }

    private void HandleCompressionCountChanged(int count)
    {
        if (count < compressionsPerPhase) return;

        switch (CurrentPhase)
        {
            case Phase.BeepPhase:
                BeginShowSummary1();
                break;
            case Phase.MusicPhase:
                BeginShowSummary2();
                break;
        }
    }

    // ---------------- Music ----------------

    private void PlayMusic(AudioClip clip)
    {
        if (musicAudioSource == null || clip == null) return;
        musicAudioSource.clip = clip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void StopMusic()
    {
        if (musicAudioSource != null)
            musicAudioSource.Stop();
    }
}