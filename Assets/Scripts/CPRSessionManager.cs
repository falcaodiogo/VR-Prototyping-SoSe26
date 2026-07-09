using UnityEngine;

/// <summary>
/// Orchestrates the CPR mini-game flow:
///
///   1. BeepPhase  - 30 compressions, metronome tick guide on (only while hand
///                   is in the zone).
///   2. Summary    - score is shown automatically, instructions/feedback popup
///                   hidden, for a few seconds.
///   3. MusicPhase - next 30 compressions, background music instead of the
///                   beep guide.
///   4. Finished   - final score shown.
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
        ShowingSummary,
        MusicPhase,
        Finished,
        SessionEnded
    }

    [Header("References")]
    [SerializeField] private CPRCompressionTracker tracker;
    [SerializeField] private CPRFeedbackUI feedbackUI;

    [Header("Music (Phase 2)")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioClip musicClip;

    [Header("Phase Settings")]
    [Tooltip("Compressions required to complete each phase.")]
    [SerializeField] private int compressionsPerPhase = 30;
    [Tooltip("How long the auto score screen stays up between phase 1 and phase 2.")]
    [SerializeField] private float summaryDisplayDuration = 5f;
    [Tooltip("How long the final combined score stays up at the very end.")]
    [SerializeField] private float finalSummaryDisplayDuration = 30f;

    public Phase CurrentPhase { get; private set; }

    private float summaryTimer;
    private CPRSessionSummary phase1Summary;

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

        tracker.OnHandInZoneChanged += HandleHandInZoneChanged;
        tracker.OnCompressionCountChanged += HandleCompressionCountChanged;
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnHandInZoneChanged -= HandleHandInZoneChanged;
            tracker.OnCompressionCountChanged -= HandleCompressionCountChanged;
        }
    }

    private void Start()
    {
        if (tracker == null || feedbackUI == null)
        {
            // Already logged in OnEnable — don't also crash Start().
            return;
        }
        BeginBeepPhase();
    }

    private void Update()
    {
        if (CurrentPhase == Phase.ShowingSummary)
        {
            summaryTimer -= Time.deltaTime;
            if (summaryTimer <= 0f)
                BeginMusicPhase();
        }
        else if (CurrentPhase == Phase.Finished)
        {
            summaryTimer -= Time.deltaTime;
            if (summaryTimer <= 0f)
            {
                feedbackUI.HideSummary();
                CurrentPhase = Phase.SessionEnded;
            }
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

    private void BeginShowSummary()
    {
        CurrentPhase = Phase.ShowingSummary;
        summaryTimer = summaryDisplayDuration;

        phase1Summary = tracker.GetSessionSummary();

        feedbackUI.SetMetronomeEnabled(false);
        feedbackUI.ShowSummary(phase1Summary, "Round 1 complete!");
    }

    private void BeginMusicPhase()
    {
        CurrentPhase = Phase.MusicPhase;
        tracker.ResetSession();
        feedbackUI.HideSummary();
        feedbackUI.SetMetronomeEnabled(false); // no beep guide in this phase
        PlayMusic();
    }

    private void BeginFinished()
    {
        CurrentPhase = Phase.Finished;
        summaryTimer = finalSummaryDisplayDuration;

        feedbackUI.SetMetronomeEnabled(false);
        StopMusic();

        CPRSessionSummary phase2Summary = tracker.GetSessionSummary();
        CPRSessionSummary combined = CombineSummaries(phase1Summary, phase2Summary);

        float depthCompliance = combined.totalCompressions > 0
            ? (combined.totalCompressions - combined.notEnoughDepthCount) / (float)combined.totalCompressions * 100f
            : 0f;

        // Overall score blends "was the rate right" and "was the depth right" —
        // the two things real CPR quality is actually judged on.
        float overallScore = (combined.GoodPercentage + depthCompliance) / 2f;
        string grade = GetGrade(overallScore);

        string extraLine = $"Depth compliance: {depthCompliance:0}%\nOverall score: {overallScore:0}% ({grade})";

        feedbackUI.ShowSummary(combined, "Session Complete!", extraLine);
    }

    private static CPRSessionSummary CombineSummaries(CPRSessionSummary a, CPRSessionSummary b)
    {
        return new CPRSessionSummary
        {
            totalCompressions = a.totalCompressions + b.totalCompressions,
            goodCount = a.goodCount + b.goodCount,
            tooSlowCount = a.tooSlowCount + b.tooSlowCount,
            tooFastCount = a.tooFastCount + b.tooFastCount,
            notEnoughDepthCount = a.notEnoughDepthCount + b.notEnoughDepthCount
        };
    }

    private static string GetGrade(float overallScorePercent)
    {
        if (overallScorePercent >= 90f) return "Excellent - Life Saver!";
        if (overallScorePercent >= 75f) return "Good";
        if (overallScorePercent >= 50f) return "Fair - Keep Practicing";
        return "Needs More Practice";
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
                BeginShowSummary();
                break;
            case Phase.MusicPhase:
                BeginFinished();
                break;
        }
    }

    // ---------------- Music ----------------

    private void PlayMusic()
    {
        if (musicAudioSource == null || musicClip == null) return;
        musicAudioSource.clip = musicClip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void StopMusic()
    {
        if (musicAudioSource != null)
            musicAudioSource.Stop();
    }
}