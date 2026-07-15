using System.Collections;
using UnityEngine;

public class BounceObject : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] private Transform launchDirection;
    [SerializeField] private float upwardForce = 12f;
    [SerializeField] private float forwardForce = 6f;
    [SerializeField] private float bounceCooldown = 0.25f;

    [Header("Audio")]
    [Tooltip("Bounce one-shot prefab. This CAN use DestroyAfterAudio.")]
    [SerializeField] private GameObject bounceSfxPrefab;
    [SerializeField] private Transform bounceSfxSpawnPoint;

    [Header("Visual Squash")]
    [SerializeField] private Transform squashTarget;
    [SerializeField][Range(0.5f, 1f)] private float squashScaleMultiplier = 0.7f;
    [SerializeField] private float squashLerpSpeed = 16f;

    private bool isOnCooldown;
    private Vector3 originalSquashScale;
    private Coroutine squashRoutine;

    private void Awake()
    {
        if (squashTarget != null)
            originalSquashScale = squashTarget.localScale;
    }

    public void TryBounce(Collider other)
    {
        if (isOnCooldown)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        Vector3 launchVector = GetLaunchVelocity();
        player.ApplyLaunch(launchVector, replaceHorizontal: true, replaceVertical: true);
        SpawnBounceSfx();

        if (squashTarget != null)
        {
            if (squashRoutine != null)
                StopCoroutine(squashRoutine);

            squashRoutine = StartCoroutine(SquashRoutine());
        }

        StartCoroutine(CooldownRoutine());
    }

    private Vector3 GetLaunchVelocity()
    {
        Vector3 forward = launchDirection != null ? launchDirection.forward : transform.forward;
        Vector3 flattenedForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

        Vector3 launch = (flattenedForward * forwardForce) + (Vector3.up * upwardForce);
        return launch;
    }

    private void SpawnBounceSfx()
    {
        if (bounceSfxPrefab == null)
            return;

        Vector3 spawnPosition = bounceSfxSpawnPoint != null ? bounceSfxSpawnPoint.position : transform.position;
        Instantiate(bounceSfxPrefab, spawnPosition, Quaternion.identity);
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(bounceCooldown);
        isOnCooldown = false;
    }

    private IEnumerator SquashRoutine()
    {
        Vector3 squashedScale = originalSquashScale * squashScaleMultiplier;

        while ((squashTarget.localScale - squashedScale).sqrMagnitude > 0.0001f)
        {
            squashTarget.localScale = Vector3.Lerp(
                squashTarget.localScale,
                squashedScale,
                squashLerpSpeed * Time.deltaTime
            );
            yield return null;
        }

        squashTarget.localScale = squashedScale;

        while ((squashTarget.localScale - originalSquashScale).sqrMagnitude > 0.0001f)
        {
            squashTarget.localScale = Vector3.Lerp(
                squashTarget.localScale,
                originalSquashScale,
                squashLerpSpeed * Time.deltaTime
            );
            yield return null;
        }

        squashTarget.localScale = originalSquashScale;
        squashRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform dir = launchDirection != null ? launchDirection : transform;

        Vector3 start = dir.position;
        Vector3 forward = Vector3.ProjectOnPlane(dir.forward, Vector3.up).normalized;
        Vector3 end = start + forward * 2f + Vector3.up * 2f;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
}
