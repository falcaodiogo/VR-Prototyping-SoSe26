using System;
using System.Collections.Generic;
using UnityEngine;

public enum CPRFeedbackType
{
    None,
    TooSlow,
    TooFast,
    Good,
    NotEnoughDepth,
    Stopped
}

/// <summary>
/// Result snapshot of a CPR attempt, used to render the end-of-session summary.
/// </summary>
public struct CPRSessionSummary
{
    public int totalCompressions;
    public int goodCount;
    public int tooSlowCount;
    public int tooFastCount;
    public int notEnoughDepthCount;

    /// <summary>
    /// % of BPM-evaluated compressions that landed in the "Good" range.
    /// (NotEnoughDepth beats aren't counted since no BPM was ever assessed for them.)
    /// </summary>
    public float GoodPercentage
    {
        get
        {
            int evaluated = goodCount + tooSlowCount + tooFastCount;
            return evaluated > 0 ? (goodCount / (float)evaluated) * 100f : 0f;
        }
    }
}

/// <summary>
/// Tracks vertical hand movement while inside the CPR zone, detects compression
/// "beats" (the bottom of each downward push), and computes a rolling BPM estimate.
/// Also accumulates per-session stats (compression count + BPM quality tally) so
/// the UI can unlock a "stop" action and show a results summary.
/// </summary>
public class CPRCompressionTracker : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Minimum downward travel (meters) required to count as a real compression, " +
             "filters out hand jitter / tiny movements.")]
    [SerializeField] private float minCompressionDepth = 0.03f;

    [Tooltip("How many recent compressions to average when computing BPM.")]
    [SerializeField] private int rollingWindowSize = 5;

    [Tooltip("If no compression is detected for this many seconds while hand is in zone, report 'Stopped'.")]
    [SerializeField] private float stalledTimeout = 2.5f;

    [Header("Target BPM Range (AHA guideline: 100-120)")]
    [SerializeField] private float idealMin = 100f;
    [SerializeField] private float idealMax = 120f;
    [SerializeField] private float acceptableMin = 90f;
    [SerializeField] private float acceptableMax = 130f;

    // --- Events ---
    public event Action<float> OnBPMUpdated;             // fires with latest computed BPM
    public event Action<CPRFeedbackType, float> OnFeedback; // feedback type + current bpm (bpm may be 0)
    public event Action OnCompressionBeat;                // fires exactly once per detected compression (good hook for SFX/haptics)
    public event Action<bool> OnHandInZoneChanged;         // true = entered, false = exited
    public event Action<int> OnCompressionCountChanged;    // fires with running total compression count for this session

    private Transform handTransform;
    private bool handInZone = false;

    private float lastY;
    private float troughY;
    private bool movingDown;
    private float startOfDownstrokeY;

    private readonly List<float> compressionTimestamps = new List<float>();
    private float lastCompressionTime;

    // --- Session stats ---
    private int totalCompressions;
    private int goodCount;
    private int tooSlowCount;
    private int tooFastCount;
    private int notEnoughDepthCount;

    public bool IsHandInZone => handInZone;
    public int TotalCompressions => totalCompressions;

    public void OnHandEnterZone(Transform hand)
    {
        Debug.Log($"[Tracker] Hand entered zone: {hand.name}");
        handTransform = hand;
        handInZone = true;
        lastY = hand.position.y;
        troughY = lastY;
        movingDown = false;
        lastCompressionTime = Time.time;
        OnHandInZoneChanged?.Invoke(true);
    }

    public void OnHandExitZone(Transform hand)
    {
        if (handTransform != hand) return;
        handInZone = false;
        handTransform = null;
        compressionTimestamps.Clear();
        OnHandInZoneChanged?.Invoke(false);
        OnFeedback?.Invoke(CPRFeedbackType.None, 0f);
    }

    /// <summary>
    /// Clears all session stats and timestamp history. Call this whenever a fresh
    /// CPR attempt begins (e.g. once the player has picked Music/Metronome mode).
    /// </summary>
    public void ResetSession()
    {
        compressionTimestamps.Clear();
        totalCompressions = 0;
        goodCount = 0;
        tooSlowCount = 0;
        tooFastCount = 0;
        notEnoughDepthCount = 0;
        OnCompressionCountChanged?.Invoke(totalCompressions);
    }

    public CPRSessionSummary GetSessionSummary()
    {
        return new CPRSessionSummary
        {
            totalCompressions = totalCompressions,
            goodCount = goodCount,
            tooSlowCount = tooSlowCount,
            tooFastCount = tooFastCount,
            notEnoughDepthCount = notEnoughDepthCount
        };
    }

    private void Update()
    {
        if (!handInZone || handTransform == null) return;

        TrackVerticalMovement(handTransform.position.y);

        if (Time.time - lastCompressionTime > stalledTimeout && compressionTimestamps.Count > 0)
        {
            compressionTimestamps.Clear();
            OnFeedback?.Invoke(CPRFeedbackType.Stopped, 0f);
        }
    }

    private void TrackVerticalMovement(float currentY)
    {
        if (currentY < lastY)
        {
            // moving down
            if (!movingDown)
            {
                movingDown = true;
                startOfDownstrokeY = lastY;
            }
            troughY = Mathf.Min(troughY, currentY);
        }
        else if (currentY > lastY && movingDown)
        {
            // direction reversed -> bottom of compression reached
            float depth = startOfDownstrokeY - troughY;
            movingDown = false;

            if (depth >= minCompressionDepth)
            {
                RegisterCompression();
            }
            else
            {
                notEnoughDepthCount++;
                OnFeedback?.Invoke(CPRFeedbackType.NotEnoughDepth, GetCurrentBPM());
            }

            troughY = currentY;
        }

        lastY = currentY;
    }

    private void RegisterCompression()
    {
        float now = Time.time;
        lastCompressionTime = now;

        compressionTimestamps.Add(now);
        if (compressionTimestamps.Count > rollingWindowSize)
            compressionTimestamps.RemoveAt(0);

        totalCompressions++;
        OnCompressionBeat?.Invoke();
        OnCompressionCountChanged?.Invoke(totalCompressions);

        if (compressionTimestamps.Count >= 2)
        {
            float bpm = GetCurrentBPM();
            OnBPMUpdated?.Invoke(bpm);
            EvaluateBPM(bpm);
        }
    }

    private float GetCurrentBPM()
    {
        if (compressionTimestamps.Count < 2) return 0f;
        float span = compressionTimestamps[^1] - compressionTimestamps[0];
        int intervals = compressionTimestamps.Count - 1;
        if (span <= 0f) return 0f;
        float avgInterval = span / intervals;
        return 60f / avgInterval;
    }

    private void EvaluateBPM(float bpm)
    {
        if (bpm < acceptableMin)
        {
            tooSlowCount++;
            OnFeedback?.Invoke(CPRFeedbackType.TooSlow, bpm);
        }
        else if (bpm > acceptableMax)
        {
            tooFastCount++;
            OnFeedback?.Invoke(CPRFeedbackType.TooFast, bpm);
        }
        else if (bpm >= idealMin && bpm <= idealMax)
        {
            goodCount++;
            OnFeedback?.Invoke(CPRFeedbackType.Good, bpm);
        }
        else if (bpm < idealMin)
        {
            tooSlowCount++;
            OnFeedback?.Invoke(CPRFeedbackType.TooSlow, bpm);
        }
        else
        {
            tooFastCount++;
            OnFeedback?.Invoke(CPRFeedbackType.TooFast, bpm);
        }
    }
}