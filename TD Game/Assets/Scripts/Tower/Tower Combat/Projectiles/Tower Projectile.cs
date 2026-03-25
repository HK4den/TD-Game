using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerProjectile : MonoBehaviour
{
    public enum EffectMode
    {
        Damage = 0,
        Heal = 1
    }

    [Header("Runtime")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float effectAmount = 1f;
    [SerializeField] private int remainingPierce = 1;
    [SerializeField] private float maxLifetime = 8f;
    [SerializeField] private EffectMode effectMode = EffectMode.Damage;

    private Vector3 moveDirection = Vector3.forward;
    private float lifetimeTimer;

    private Collider ownCollider;
    private Rigidbody rb;

    private readonly HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Initialize(
        Vector3 direction,
        float moveSpeed,
        int pierce,
        float amount,
        EffectMode mode,
        float lifetime)
    {
        moveDirection = direction.sqrMagnitude <= 0.0001f ? Vector3.forward : direction.normalized;
        speed = Mathf.Max(0.01f, moveSpeed);
        remainingPierce = Mathf.Max(1, pierce);
        effectAmount = Mathf.Max(0.001f, amount);
        effectMode = mode;
        maxLifetime = Mathf.Max(0.1f, lifetime);

        lifetimeTimer = 0f;
        alreadyHit.Clear();

        transform.forward = moveDirection;
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        transform.position += moveDirection * speed * Time.deltaTime;

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (other == null)
            return;

        if (other == ownCollider)
            return;

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
            return;

        if (!health.IsAlive)
            return;

        if (alreadyHit.Contains(health))
            return;

        alreadyHit.Add(health);

        switch (effectMode)
        {
            case EffectMode.Heal:
                health.Heal(effectAmount);
                break;

            case EffectMode.Damage:
            default:
                health.TakeDamage(effectAmount);
                break;
        }

        remainingPierce--;

        if (remainingPierce <= 0)
            Destroy(gameObject);
    }
}