using UnityEngine;

public class Billboard : MonoBehaviour
{
    public enum RotationMode
    {
        Clockwise,
        Counterclockwise,
        Random
    }

    [Header("Billboard")]
    private Camera mainCam;

    [Header("Optional Visual Rotation")]
    [SerializeField] private bool rotateOverTime = false;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private RotationMode rotationMode = RotationMode.Clockwise;

    private float chosenRotationDirection = 1f;

    private void Start()
    {
        if (rotationMode == RotationMode.Random)
        {
            chosenRotationDirection = Random.value < 0.5f ? -1f : 1f;
        }
        else if (rotationMode == RotationMode.Clockwise)
        {
            chosenRotationDirection = -1f;
        }
        else
        {
            chosenRotationDirection = 1f;
        }
    }

    private void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // Face the camera first
        transform.forward = -mainCam.transform.forward;

        // Then apply optional roll rotation around the visual forward axis
        if (rotateOverTime)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * chosenRotationDirection * Time.deltaTime, Space.Self);
        }
    }
}