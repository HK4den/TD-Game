using UnityEngine;

public class EnemyExplosionSpriteAnimation : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Already-Split Frames")]
    [SerializeField] private Sprite[] frames;

    [Header("Timing")]
    [SerializeField] private float framesPerSecond = 24f;
    [SerializeField] private bool destroyWhenFinished = true;

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;

    private float elapsed;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        float safeFps = Mathf.Max(0.01f, framesPerSecond);
        elapsed += Time.deltaTime;

        int frameIndex = Mathf.FloorToInt(elapsed * safeFps);
        if (frameIndex >= frames.Length)
        {
            if (destroyWhenFinished)
                Destroy(gameObject);
            else
                spriteRenderer.sprite = frames[frames.Length - 1];

            return;
        }

        spriteRenderer.sprite = frames[frameIndex];
    }

    private void LateUpdate()
    {
        if (!faceCamera)
            return;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            targetCamera.transform.forward,
            targetCamera.transform.up);
    }

    public void SetScaleMultiplier(float scaleMultiplier)
    {
        transform.localScale = initialScale * Mathf.Max(0f, scaleMultiplier);
    }
}
