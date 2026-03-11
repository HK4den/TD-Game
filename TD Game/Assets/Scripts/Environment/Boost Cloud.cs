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
        TryBoostPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBoostPlayer(other);
    }

    private void TryBoostPlayer(Collider other)
    {
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
            return;

        playerMovement.SustainSpeedOverride(BOOST_SPEED, boostDuration);

        // Later:
        // - trigger particle burst
        // - play sound
        // - pulse visuals
    }
}