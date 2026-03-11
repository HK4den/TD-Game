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

    [Header("Mask (this is what shrinks)")]
    [SerializeField] private RectTransform maskRect;
    [SerializeField] private float fullMaskHeight = 300f;

    [Header("Fill (this stays full size)")]
    [SerializeField] private RectTransform fillRect;

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

        if (fillRect != null)
        {
            Vector2 fillSize = fillRect.sizeDelta;
            fillSize.y = fullMaskHeight;
            fillRect.sizeDelta = fillSize;
        }
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
        if (maskRect == null)
            return;

        float t = Mathf.Clamp01(normalized01);

        Vector2 maskSize = maskRect.sizeDelta;
        maskSize.y = fullMaskHeight * t;
        maskRect.sizeDelta = maskSize;
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