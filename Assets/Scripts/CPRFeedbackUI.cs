using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space (overlay) UI that:
///  1. Shows live CPR feedback text/color based on CPRCompressionTracker events.
///  2. Drives a constant 110 BPM visual pulse + tick sound as a rhythm guide,
///     independent of the player's actual performance.
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

    [Header("Colors")]
    [SerializeField] private Color colorGood = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color colorSlow = new Color(0.95f, 0.8f, 0.1f);
    [SerializeField] private Color colorFast = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color colorNeutral = new Color(0.3f, 0.3f, 0.3f);

    private float metronomeTimer;
    private Vector3 metronomeBaseScale;
    private float pulseTimer;

    private void Awake()
    {
        if (metronomeIcon != null)
            metronomeBaseScale = metronomeIcon.localScale;

        if (popupGroup != null)
            popupGroup.alpha = 0f;
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
        DriveMetronome();
        AnimatePulseDecay();
    }

    // ---------------- Metronome (constant 110 BPM guide) ----------------

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
        if (popupGroup == null) return;
        popupGroup.alpha = entered ? 1f : 0f;

        if (entered)
            SetMessage("Start compressions...", colorNeutral);
    }

    private void HandleFeedback(CPRFeedbackType type, float bpm)
    {
        switch (type)
        {
            case CPRFeedbackType.Good:
                SetMessage($"Good rhythm! {bpm:0} BPM", colorGood);
                break;
            case CPRFeedbackType.TooSlow:
                SetMessage($"Too slow — push faster! {bpm:0} BPM", colorSlow);
                break;
            case CPRFeedbackType.TooFast:
                SetMessage($"Too fast — slow down! {bpm:0} BPM", colorFast);
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
}