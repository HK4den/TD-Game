using UnityEngine;

public class TowerShooter : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float range = 4.5f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Firing")]
    [SerializeField] private float fireRate = 2f; // shots/sec
    [SerializeField] private float damage = 5f;

    [Header("Optional")]
    [SerializeField] private Transform muzzle; // if null, uses tower position

    private float nextFireTime;

    private void Update()
    {
        if (Time.time < nextFireTime) return;

        EnemyHealth target = FindNearestEnemy();
        if (target == null) return;

        // Fire
        target.TakeDamage(damage);
        nextFireTime = Time.time + (1f / fireRate);
    }

    private EnemyHealth FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyMask, QueryTriggerInteraction.Ignore);
        EnemyHealth best = null;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyHealth eh = hits[i].GetComponentInParent<EnemyHealth>();
            if (eh == null) continue;

            float d = (eh.transform.position - transform.position).sqrMagnitude;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = eh;
            }
        }

        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
