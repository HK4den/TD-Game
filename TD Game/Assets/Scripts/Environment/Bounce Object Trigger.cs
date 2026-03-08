using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BounceObjectTrigger : MonoBehaviour
{
    [SerializeField] private BounceObject bounceObject;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bounceObject == null)
            return;

        bounceObject.TryBounce(other);
    }
}