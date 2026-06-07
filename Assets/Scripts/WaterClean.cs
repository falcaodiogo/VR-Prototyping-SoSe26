using UnityEngine;

public class WaterClean : MonoBehaviour
{
    public ParticleSystem waterParticles; 
    public float cleanThreshold = 2.0f;    
    public BandageSnap bandageToUnlock; // Drag the specific bandage for this wound here
    
    private float cleanTimer = 0;
    private bool isTouchingWound = false;
    private GameObject currentWound;

    void Start()
    {
        // Ensure the bandage can't snap until we clean
        if (bandageToUnlock != null) bandageToUnlock.enabled = false;
    }

    void Update()
    {
        // IMPROVED TILT LOGIC: 
        // This checks if the bottle's center is higher than the cap.
        // It works no matter which way the bottle was modeled.
        bool isUpsideDown = transform.position.y > waterParticles.transform.position.y;

        if (isUpsideDown)
        {
            if (!waterParticles.isPlaying) waterParticles.Play();

            if (isTouchingWound)
            {
                cleanTimer += Time.deltaTime;
                if (cleanTimer >= cleanThreshold)
                {
                    FinishCleaning();
                }
            }
        }
        else
        {
            if (waterParticles.isPlaying) waterParticles.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Unified Tag Check
        if (other.CompareTag("Wound"))
        {
            isTouchingWound = true;
            currentWound = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Wound"))
        {
            isTouchingWound = false;
            cleanTimer = 0;
        }
    }

    void FinishCleaning()
    {
        Debug.Log("Wound Cleaned!");
        isTouchingWound = false;
        
        if (currentWound != null)
        {
            // This finds ANY child object with "Visual" in the name and hides it
            foreach (Transform child in currentWound.transform)
            {
                if (child.name.Contains("Visual")) child.gameObject.SetActive(false);
            }
            
            if (bandageToUnlock != null) bandageToUnlock.enabled = true;
        }
    }
}