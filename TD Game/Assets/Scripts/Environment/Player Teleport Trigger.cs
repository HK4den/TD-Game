using UnityEngine;

public class PlayerTeleportTrigger : MonoBehaviour
{
    [Header("Who Can Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Teleport Target")]
    [SerializeField] private Transform destination;
    [SerializeField] private float forcedYRotation = 0f;

    [Header("Optional")]
    [SerializeField] private bool useDestinationRotationY = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (destination == null)
        {
            Debug.LogWarning($"[{name}] Teleport destination is missing.");
            return;
        }

        CharacterController controller = other.GetComponent<CharacterController>();
        Transform playerTransform = other.transform;

        if (controller != null)
        {
            // CharacterController can fight manual transform position changes,
            // so disable it first, move the player, then re-enable it.
            controller.enabled = false;
        }

        playerTransform.position = destination.position;

        float yRot = useDestinationRotationY ? destination.eulerAngles.y : forcedYRotation;
        playerTransform.rotation = Quaternion.Euler(0f, yRot, 0f);

        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}