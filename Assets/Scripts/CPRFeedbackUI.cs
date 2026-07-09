using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space (overlay) UI that:
///  1. Shows live CPR feedback text/color based on CPRCompressionTracker events.
///  2. Drives a 110 BPM visual pulse + tick sound as a rhythm guide, but ONLY
///     while explicitly enabled (see SetMetronomeEnabled) — it no longer runs
///     continuously regardless of zone/phase state.
///  3. Shows/hides an end-of-round summary panel.
///
/// Phase/flow orchestration (when to enable the metronome, when to show the
/// summary, when to switch to music) lives in CPRSessionManager. This class
/// only knows how to render what it's told to render.
/// </summary>
public class CPRFeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CPRCompressionTracker tracker;
    [SerializeField] private CanvasGroup popupGroup;       // the whole popup panel, for fade in/out
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image popupBackground;

    [Header("Metronome Guide")]
    [SerializeField] private RectTransform metronomeIcon;  // small heart/pulse icon, pulses on each beat
    [SerializeField] private AudioSource metronomeAudioSource;
    [SerializeField] private AudioClip tickClip;
    [SerializeField] private float metronomeBPM = 110f;
    [SerializeField] private float pulseScale = 1.3f;
    [SerializeField] private float pulseDuration = 0.12f;

    [Header("Summary Panel")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI summaryText;

    private CanvasGroup summaryCanvasGroup;

    [Header("Colors")]
    [SerializeField] private Color colorGood = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color colorSlow = new Color(0.95f, 0.8f, 0.1f);
    [SerializeField] private Color colorFast = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color colorNeutral = new Color(0.3f, 0.3f, 0.3f);

    private float metronomeTimer;
    private Vector3 metronomeBaseScale;
    private float pulseTimer;

    /// <summary>True only while the session manager wants the beep guide running.</summary>
    private bool metronomeEnabled;

    private void Awake()
    {
        if (metronomeIcon != null)
            metronomeBaseScale = metronomeIcon.localScale;

        if (popupGroup != null)
            popupGroup.alpha = 0f;

        if (summaryPanel != null)
        {
            summaryCanvasGroup = summaryPanel.GetComponent<CanvasGroup>();
            summaryPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (tracker != null)
        {
            tracker.OnFeedback += HandleFeedback;
            tracker.OnHandInZoneChanged += HandleZoneChanged;
        }
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnFeedback -= HandleFeedback;
            tracker.OnHandInZoneChanged -= HandleZoneChanged;
        }
    }

    private void Update()
    {
        if (metronomeEnabled)
            DriveMetronome();

        AnimatePulseDecay();
    }

    // ---------------- Metronome (guide beep, externally gated) ----------------

    /// <summary>
    /// Called by CPRSessionManager to turn the beep guide on/off (e.g. only
    /// while the hand is in the zone during the beep phase, and never during
    /// the music phase).
    /// </summary>
    public void SetMetronomeEnabled(bool enabled)
    {
        if (metronomeEnabled == enabled) return;

        metronomeEnabled = enabled;
        metronomeTimer = 0f;

        if (!enabled)
        {
            // Snap the icon back and make sure no tick keeps ringing out.
            pulseTimer = 0f;
            if (metronomeIcon != null)
                metronomeIcon.localScale = metronomeBaseScale;

            if (metronomeAudioSource != null)
                metronomeAudioSource.Stop();
        }
    }

    private void DriveMetronome()
    {
        float interval = 60f / metronomeBPM;
        metronomeTimer += Time.deltaTime;

        if (metronomeTimer >= interval)
        {
            metronomeTimer -= interval;
            PulseMetronomeIcon();
            PlayTick();
        }
    }

    private void PulseMetronomeIcon()
    {
        pulseTimer = pulseDuration;
        if (metronomeIcon != null)
            metronomeIcon.localScale = metronomeBaseScale * pulseScale;
    }

    private void AnimatePulseDecay()
    {
        if (pulseTimer <= 0f || metronomeIcon == null) return;

        pulseTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(pulseTimer / pulseDuration);
        metronomeIcon.localScale = Vector3.Lerp(metronomeBaseScale, metronomeBaseScale * pulseScale, t);
    }

    private void PlayTick()
    {
        if (metronomeAudioSource != null && tickClip != null)
            metronomeAudioSource.PlayOneShot(tickClip);
    }

    // ---------------- Feedback popup ----------------

    private void HandleZoneChanged(bool entered)
    {
        SetPopupVisible(entered);

        if (entered)
            SetMessage("Start compressions...", colorNeutral);
    }

    public void SetPopupVisible(bool visible)
    {
        if (popupGroup == null) return;
        popupGroup.alpha = visible ? 1f : 0f;
    }

    public void HidePopup() => SetPopupVisible(false);

    private void HandleFeedback(CPRFeedbackType type, float bpm)
    {
        switch (type)
        {
            case CPRFeedbackType.Good:
                SetMessage($"Good rhythm! {bpm:0} BPM", colorGood);
                break;
            case CPRFeedbackType.TooSlow:
                SetMessage($"Too slow, push faster! {bpm:0} BPM", colorSlow);
                break;
            case CPRFeedbackType.TooFast:
                SetMessage($"Too fast, slow down! {bpm:0} BPM", colorFast);
                break;
            case CPRFeedbackType.NotEnoughDepth:
                SetMessage("Push deeper!", colorSlow);
                break;
            case CPRFeedbackType.Stopped:
                SetMessage("Keep compressions going!", colorFast);
                break;
            case CPRFeedbackType.None:
            default:
                SetMessage("", colorNeutral);
                break;
        }
    }

    private void SetMessage(string message, Color color)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (popupBackground != null)
            popupBackground.color = color;
    }

    // ---------------- Summary panel ----------------

    /// <summary>
    /// Shows the end-of-round score and hides the instructions/feedback popup
    /// at the same time, per the requested flow.
    /// </summary>
    public void ShowSummary(CPRSessionSummary summary, string title, string extraLine = null)
    {
        HidePopup();

        if (summaryPanel != null)
            summaryPanel.SetActive(true);

        if (summaryCanvasGroup != null)
        {
            summaryCanvasGroup.alpha = 1f;
            summaryCanvasGroup.interactable = true;
            summaryCanvasGroup.blocksRaycasts = true;
        }

        if (summaryText != null)
        {
            string text =
                $"{title}\n\n" +
                $"Compressions: {summary.totalCompressions}\n" +
                $"Accuracy: {summary.GoodPercentage:0}%";

            if (!string.IsNullOrEmpty(extraLine))
                text += $"\n{extraLine}";

            summaryText.text = text;
        }
    }

    public void HideSummary()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);

        if (summaryCanvasGroup != null)
            summaryCanvasGroup.alpha = 0f;
    }
}