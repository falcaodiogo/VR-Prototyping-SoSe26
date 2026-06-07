using UnityEngine;
using TMPro;

public class MedicalPlus : MonoBehaviour
{
    [Header("Movement")]
    public float rotationSpeed = 100f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 1.5f;

    [Header("Color Settings")]
    public Color assessedColor = Color.green;
    private bool isAssessed = false;

    [Header("UI & Activation")]
    public GameObject checklistCanvas; // Drag 'ChecklistCanvas' here
    public TextMeshProUGUI checklistItem; // Drag 'Text_Head', etc., here

    [Header("Audio")]
    public AudioClip successSound;
    private AudioSource audioSource;

    private Vector3 startPos;
    private MeshRenderer[] childRenderers;

    void Start()
    {
        startPos = transform.position;
        childRenderers = GetComponentsInChildren<MeshRenderer>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        
        // Setup AudioSource for 3D VR sound
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isAssessed && (other.CompareTag("Player") || other.name.Contains("Hand") || other.name.Contains("Controller")))
        {
            CompletePoint();
        }
    }

    void CompletePoint()
    {
        isAssessed = true;

        // 1. Activate the Canvas if it is hidden
        if (checklistCanvas != null && !checklistCanvas.activeSelf)
        {
            checklistCanvas.SetActive(true);
        }

        // 2. Audio Feedback
        if (successSound != null) audioSource.PlayOneShot(successSound);

        // 3. Visual Feedback (Plus Sign)
        foreach (MeshRenderer renderer in childRenderers)
        {
            renderer.material.color = assessedColor;
        }

        // 4. Checklist Feedback (Text)
        if (checklistItem != null)
        {
            checklistItem.color = assessedColor;
            checklistItem.text = "<s>" + checklistItem.text + "</s> ✓";
        }
    }
}