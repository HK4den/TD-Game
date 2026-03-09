using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoostCloud : MonoBehaviour
{
    private const float BOOST_SPEED = 12f;

    [Header("Boost")]
    [SerializeField] private float boostDuration = 3f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
            return;

        bool applied = playerMovement.TryStartSpeedOverride(BOOST_SPEED, boostDuration);

        // If applied is false, this cloud gave less or equal time than
        // the player already had left, so it does nothing.
        if (!applied)
            return;

        // Later:
        // - trigger particle burst
        // - play sound
        // - pulse visuals
    }
}