using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoostPad : MonoBehaviour
{
    private const float BOOST_SPEED = 12f;
    private const float BOOST_DURATION = 3f;
    private const float BOOST_FOV = 92f;
    private const float TRIGGER_COOLDOWN = 0.25f;

    private bool isOnCooldown;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOnCooldown)
            return;

        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
            return;

        PlayerLook playerLook = playerMovement.GetComponent<PlayerLook>();
        if (playerLook == null)
            playerLook = playerMovement.GetComponentInChildren<PlayerLook>();

        playerMovement.StartSpeedOverride(BOOST_SPEED, BOOST_DURATION);

        if (playerLook != null)
            playerLook.StartFOVOverride(BOOST_FOV, BOOST_DURATION);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(TRIGGER_COOLDOWN);
        isOnCooldown = false;
    }
}