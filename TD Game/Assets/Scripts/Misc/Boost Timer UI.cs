using UnityEngine;
using UnityEngine.UI;

public class BoostTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Panel Root")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Panel Positions")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 hiddenAnchoredPos;
    [SerializeField] private Vector2 shownAnchoredPos;
    [SerializeField] private float moveDuration = 0.15f;

    [Header("Boost Fill")]
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private float fillFullHeight = 300f;

    [Header("Boost Fill Shader")]
    [SerializeField] private Image fillImage;
    [SerializeField] private string aspectPropertyName = "_AspectCompensation";
    [SerializeField] private float minAspectCompensation = 0.0001f;

    private float moveT;
    private Vector2 moveFrom;
    private Vector2 moveTo;

    private bool wasVisibleLastFrame;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (panelRect == null)
            panelRect = panelRoot;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (fillImage == null && fillRect != null)
            fillImage = fillRect.GetComponent<Image>();

        // Make sure this UI gets its own runtime material instance
        // so we don't modify the shared material asset/project-wide.
        if (fillImage != null && fillImage.material != null)
            runtimeMaterial = fillImage.material = new Material(fillImage.material);

        ForceMoveTo(hiddenAnchoredPos);
        SetBoostFill(0f);
        wasVisibleLastFrame = false;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    private void Update()
    {
        if (playerMovement == null || panelRect == null)
            return;

        bool shouldBeVisible = playerMovement.IsBoostActive;

        if (shouldBeVisible != wasVisibleLastFrame)
        {
            StartMove(shouldBeVisible ? shownAnchoredPos : hiddenAnchoredPos);
            wasVisibleLastFrame = shouldBeVisible;
        }

        if (moveT < 1f)
        {
            moveT += (moveDuration <= 0f) ? 1f : (Time.unscaledDeltaTime / moveDuration);
            float t = Mathf.Clamp01(moveT);
            panelRect.anchoredPosition = Vector2.Lerp(moveFrom, moveTo, t);
        }

        if (shouldBeVisible)
            SetBoostFill(playerMovement.BoostNormalized);
        else
            SetBoostFill(0f);
    }

    public void SetBoostFill(float normalized01)
    {
        if (fillRect == null)
            return;

        float t = Mathf.Clamp01(normalized01);

        Vector2 size = fillRect.sizeDelta;
        size.y = fillFullHeight * t;
        fillRect.sizeDelta = size;

        UpdateShaderAspectCompensation();
    }

    public void ClearBoostFill()
    {
        SetBoostFill(0f);
    }

    private void UpdateShaderAspectCompensation()
    {
        if (runtimeMaterial == null || fillRect == null)
            return;

        Rect rect = fillRect.rect;

        float width = Mathf.Max(rect.width, 0.0001f);
        float height = Mathf.Max(rect.height, 0.0001f);

        float aspectCompensation = Mathf.Max(height / width, minAspectCompensation);

        runtimeMaterial.SetFloat(aspectPropertyName, aspectCompensation);
    }

    private void StartMove(Vector2 target)
    {
        if (panelRect == null) return;

        moveFrom = panelRect.anchoredPosition;
        moveTo = target;
        moveT = 0f;
    }

    private void ForceMoveTo(Vector2 pos)
    {
        if (panelRect == null) return;

        panelRect.anchoredPosition = pos;
        moveFrom = pos;
        moveTo = pos;
        moveT = 1f;
    }
}