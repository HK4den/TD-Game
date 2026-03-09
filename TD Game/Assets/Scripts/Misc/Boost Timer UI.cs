using UnityEngine;

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

    private float moveT;
    private Vector2 moveFrom;
    private Vector2 moveTo;

    private bool wasVisibleLastFrame;

    private void Awake()
    {
        if (panelRect == null)
            panelRect = panelRoot;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        ForceMoveTo(hiddenAnchoredPos);
        SetBoostFill(0f);
        wasVisibleLastFrame = false;
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
    }

    public void ClearBoostFill()
    {
        SetBoostFill(0f);
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