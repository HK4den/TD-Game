using UnityEngine;

public class PlayerMovementAudio : MonoBehaviour
{
    private enum FootstepState
    {
        None,
        Walk,
        Sprint,
        Boost
    }

    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Looping Footstep Prefabs")]
    [Tooltip("Looping walk audio prefab. Should have an AudioSource. Do NOT put DestroyAfterAudio on this one.")]
    [SerializeField] private GameObject walkLoopPrefab;

    [Tooltip("Looping sprint audio prefab. Should have an AudioSource. Do NOT put DestroyAfterAudio on this one.")]
    [SerializeField] private GameObject sprintLoopPrefab;

    [Tooltip("Looping boost-walk audio prefab. Should have an AudioSource. Do NOT put DestroyAfterAudio on this one.")]
    [SerializeField] private GameObject boostLoopPrefab;

    [Header("One-Shot Prefabs")]
    [Tooltip("Jump one-shot prefab. This CAN use DestroyAfterAudio.")]
    [SerializeField] private GameObject jumpSfxPrefab;

    [Tooltip("Landing one-shot prefab. This CAN use DestroyAfterAudio.")]
    [SerializeField] private GameObject landSfxPrefab;

    [Tooltip("One-shot prefab that plays once after being airborne for the 1-second fall threshold.")]
    [SerializeField] private GameObject fallOneSecondSfxPrefab;

    [Tooltip("One-shot prefab that plays once after being airborne for the 3-second fall threshold.")]
    [SerializeField] private GameObject fallThreeSecondSfxPrefab;

    [Header("Footstep Detection")]
    [SerializeField] private float movementThreshold = 0.15f;
    [SerializeField] private float footstepFadeDuration = 0.08f;

    [Header("Jump / Land Detection")]
    [SerializeField] private float minLandingAirborneTime = 0.08f;
    [SerializeField] private float minLandingVerticalVelocity = 2f;

    [Header("Long Fall Detection")]
    [SerializeField] private float fallOneSecondThreshold = 1f;
    [SerializeField] private float fallThreeSecondThreshold = 3f;

    [Header("Loop Pitch Randomization")]
    [SerializeField] private bool randomizeLoopPitch = true;
    [SerializeField] private float minLoopPitch = 0.95f;
    [SerializeField] private float maxLoopPitch = 1.05f;

    private GameObject walkLoopInstance;
    private GameObject sprintLoopInstance;
    private GameObject boostLoopInstance;

    private AudioSource walkSource;
    private AudioSource sprintSource;
    private AudioSource boostSource;

    private float walkBaseVolume = 1f;
    private float sprintBaseVolume = 1f;
    private float boostBaseVolume = 1f;

    private FootstepState currentState = FootstepState.None;

    private bool wasGroundedLastFrame;
    private float lastVerticalVelocity;
    private float airborneTime;
    private bool playedFallOneSecondSfx;
    private bool playedFallThreeSecondSfx;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        CreateLoopInstance(walkLoopPrefab, ref walkLoopInstance, ref walkSource, ref walkBaseVolume);
        CreateLoopInstance(sprintLoopPrefab, ref sprintLoopInstance, ref sprintSource, ref sprintBaseVolume);
        CreateLoopInstance(boostLoopPrefab, ref boostLoopInstance, ref boostSource, ref boostBaseVolume);
    }

    private void Start()
    {
        if (playerMovement != null)
        {
            wasGroundedLastFrame = playerMovement.IsGrounded;
            lastVerticalVelocity = playerMovement.VerticalVelocity;
            playerMovement.Jumped += OnPlayerJumped;
        }
    }

    private void Update()
    {
        if (playerMovement == null)
            return;

        if (PauseState.IsPaused)
        {
            StopLoopingMovementAudio();
            wasGroundedLastFrame = playerMovement.IsGrounded;
            lastVerticalVelocity = playerMovement.VerticalVelocity;
            return;
        }

        HandleJumpAndLandOneShots();
        UpdateFootstepState();
        UpdateLoopVolumes();

        wasGroundedLastFrame = playerMovement.IsGrounded;
        lastVerticalVelocity = playerMovement.VerticalVelocity;
    }

    private void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.Jumped -= OnPlayerJumped;

        DestroyLoopInstance(ref walkLoopInstance, ref walkSource);
        DestroyLoopInstance(ref sprintLoopInstance, ref sprintSource);
        DestroyLoopInstance(ref boostLoopInstance, ref boostSource);
    }

    private void OnPlayerJumped()
    {
        SpawnOneShot(jumpSfxPrefab);
    }

    private void HandleJumpAndLandOneShots()
    {
        bool isGrounded = playerMovement.IsGrounded;

        bool justLanded = !wasGroundedLastFrame && isGrounded;
        bool justLeftGround = wasGroundedLastFrame && !isGrounded;

        if (justLeftGround)
        {
            airborneTime = 0f;
            playedFallOneSecondSfx = false;
            playedFallThreeSecondSfx = false;
        }

        if (!isGrounded)
        {
            airborneTime += Time.deltaTime;
            HandleLongFallOneShots();
        }

        // Land: only play if we were falling fast enough before touching down.
        if (justLanded && airborneTime >= minLandingAirborneTime && lastVerticalVelocity < -minLandingVerticalVelocity)
        {
            SpawnOneShot(landSfxPrefab);
        }

        if (isGrounded)
        {
            airborneTime = 0f;
            playedFallOneSecondSfx = false;
            playedFallThreeSecondSfx = false;
        }
    }

    private void HandleLongFallOneShots()
    {
        if (!playedFallOneSecondSfx && airborneTime >= fallOneSecondThreshold)
        {
            playedFallOneSecondSfx = true;
            SpawnOneShot(fallOneSecondSfxPrefab);
        }

        if (!playedFallThreeSecondSfx && airborneTime >= fallThreeSecondThreshold)
        {
            playedFallThreeSecondSfx = true;
            SpawnOneShot(fallThreeSecondSfxPrefab);
        }
    }

    private void UpdateFootstepState()
    {
        FootstepState desiredState = GetDesiredFootstepState();

        if (desiredState == currentState)
            return;

        currentState = desiredState;

        if (currentState == FootstepState.Walk)
            StartLoopIfNeeded(walkSource);
        else if (currentState == FootstepState.Sprint)
            StartLoopIfNeeded(sprintSource);
        else if (currentState == FootstepState.Boost)
            StartLoopIfNeeded(boostSource);
    }

    private FootstepState GetDesiredFootstepState()
    {
        if (playerMovement.IsMovementLocked)
            return FootstepState.None;

        if (!playerMovement.IsGrounded)
            return FootstepState.None;

        float horizontalSpeed = playerMovement.HorizontalVelocity.magnitude;
        if (horizontalSpeed < movementThreshold)
            return FootstepState.None;

        if (playerMovement.HasSpeedOverride)
            return FootstepState.Boost;

        if (playerMovement.IsSprinting)
            return FootstepState.Sprint;

        return FootstepState.Walk;
    }

    private void UpdateLoopVolumes()
    {
        float fadeSpeed = footstepFadeDuration <= 0.0001f ? 9999f : (1f / footstepFadeDuration);

        UpdateSourceVolume(
            walkSource,
            currentState == FootstepState.Walk ? walkBaseVolume : 0f,
            fadeSpeed);

        UpdateSourceVolume(
            sprintSource,
            currentState == FootstepState.Sprint ? sprintBaseVolume : 0f,
            fadeSpeed);

        UpdateSourceVolume(
            boostSource,
            currentState == FootstepState.Boost ? boostBaseVolume : 0f,
            fadeSpeed);
    }

    private void StopLoopingMovementAudio()
    {
        currentState = FootstepState.None;
        StopSource(walkSource);
        StopSource(sprintSource);
        StopSource(boostSource);
    }

    private void StopSource(AudioSource source)
    {
        if (source == null)
            return;

        source.volume = 0f;

        if (source.isPlaying)
            source.Stop();
    }

    private void UpdateSourceVolume(AudioSource source, float targetVolume, float fadeSpeed)
    {
        if (source == null)
            return;

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);

        bool shouldBeStopped = targetVolume <= 0.0001f && source.volume <= 0.0001f;
        if (shouldBeStopped && source.isPlaying)
            source.Stop();
    }

    private void StartLoopIfNeeded(AudioSource source)
    {
        if (source == null)
            return;

        if (randomizeLoopPitch)
            source.pitch = Random.Range(minLoopPitch, maxLoopPitch);
        else
            source.pitch = 1f;

        if (!source.isPlaying)
            source.Play();
    }

    private void SpawnOneShot(GameObject prefab)
    {
        if (prefab == null)
            return;

        Instantiate(prefab, transform.position, Quaternion.identity);
    }

    private void CreateLoopInstance(
        GameObject prefab,
        ref GameObject instance,
        ref AudioSource source,
        ref float baseVolume)
    {
        if (prefab == null)
            return;

        instance = Instantiate(prefab, transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        source = instance.GetComponent<AudioSource>();
        if (source == null)
            source = instance.GetComponentInChildren<AudioSource>();

        if (source == null)
        {
            Debug.LogWarning($"Loop prefab '{prefab.name}' does not have an AudioSource.");
            return;
        }

        baseVolume = source.volume;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    private void DestroyLoopInstance(ref GameObject instance, ref AudioSource source)
    {
        source = null;

        if (instance == null)
            return;

        Destroy(instance);
        instance = null;
    }
}
