using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroCutsceneScroller : MonoBehaviour
{
    [Header("UI Scroll")]
    [SerializeField] private RectTransform imageToScroll;
    [SerializeField] private Vector2 startAnchoredPosition;
    [SerializeField] private Vector2 endAnchoredPosition;
    [SerializeField] private float scrollDuration = 10f;

    [Header("Cutscene Timing")]
    [SerializeField] private float sceneLoadDelay = 10f;
    [SerializeField] private string nextSceneName;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playAudioOnStart = true;

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;

    private PlayerControls controls;
    private float timer;
    private float scrollTimer;
    private bool isLeavingScene;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Jump.performed += OnJumpPressed;
    }

    private void OnDisable()
    {
        controls.Player.Jump.performed -= OnJumpPressed;
        controls.Player.Disable();
    }

    private void Start()
    {
        timer = sceneLoadDelay;
        scrollTimer = 0f;

        if (imageToScroll != null)
        {
            imageToScroll.anchoredPosition = startAnchoredPosition;
        }

        if (audioSource != null && playAudioOnStart)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (isLeavingScene)
            return;

        HandleScroll();
        HandleTimer();
    }

    private void HandleScroll()
    {
        if (imageToScroll == null)
            return;

        if (scrollDuration <= 0f)
        {
            imageToScroll.anchoredPosition = endAnchoredPosition;
            return;
        }

        scrollTimer += Time.deltaTime;
        float t = Mathf.Clamp01(scrollTimer / scrollDuration);

        imageToScroll.anchoredPosition = Vector2.Lerp(startAnchoredPosition, endAnchoredPosition, t);
    }

    private void HandleTimer()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            LoadNextScene();
        }
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (!allowSkip || isLeavingScene)
            return;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLeavingScene)
            return;

        isLeavingScene = true;

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        SceneManager.LoadScene(nextSceneName);
    }
}