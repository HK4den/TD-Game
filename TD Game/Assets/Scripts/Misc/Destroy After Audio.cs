using UnityEngine;

public class DestroyAfterAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Lifetime")]
    [SerializeField] private float fallbackLifetime = 3f;

    [Header("Pitch Randomization")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (audioSource != null)
        {
            // Apply random pitch BEFORE playback
            if (randomizePitch)
            {
                float randomPitch = Random.Range(minPitch, maxPitch);
                audioSource.pitch = randomPitch;
            }

            // Destroy after clip finishes
            if (audioSource.clip != null)
                Destroy(gameObject, audioSource.clip.length / Mathf.Abs(audioSource.pitch));
            else
                Destroy(gameObject, fallbackLifetime);
        }
        else
        {
            Destroy(gameObject, fallbackLifetime);
        }
    }
}