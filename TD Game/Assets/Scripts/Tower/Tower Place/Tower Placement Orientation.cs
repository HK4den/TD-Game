using UnityEngine;

public class TowerPlacementOrientation : MonoBehaviour
{
    public enum FacingRule
    {
        TowardPlayer = 0,
        AwayFromPlayer = 1
    }

    [Header("Placement Facing")]
    [SerializeField] private FacingRule facingRule = FacingRule.TowardPlayer;
    [SerializeField] private bool cardinalOnly = false;

    public FacingRule Rule => facingRule;
    public bool CardinalOnly => cardinalOnly;

    public Quaternion GetPlacementRotation(Vector3 towerPosition, Vector3 playerPosition, Quaternion fallbackRotation)
    {
        Vector3 direction = playerPosition - towerPosition;
        direction.y = 0f;

        if (facingRule == FacingRule.AwayFromPlayer)
            direction = -direction;

        if (direction.sqrMagnitude <= 0.0001f)
            return cardinalOnly ? SnapToCardinal(fallbackRotation) : fallbackRotation;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (cardinalOnly)
            rotation = SnapToCardinal(rotation);

        return rotation;
    }

    private Quaternion SnapToCardinal(Quaternion input)
    {
        float yaw = input.eulerAngles.y;
        float snapped = Mathf.Round(yaw / 90f) * 90f;
        return Quaternion.Euler(0f, snapped, 0f);
    }
}