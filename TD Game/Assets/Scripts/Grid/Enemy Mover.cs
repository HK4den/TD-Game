using System.Collections.Generic;
using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float arriveDistance = 0.05f;

    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private int index = 0;

    public void SetPath(IReadOnlyList<Vector3> points)
    {
        pathPoints.Clear();
        if (points == null || points.Count == 0) return;

        pathPoints.AddRange(points);
        index = 0;

        // Snap to first point (optional, but makes testing clean)
        transform.position = pathPoints[0];
    }

    private void Update()
    {
        if (pathPoints.Count == 0 || index >= pathPoints.Count) return;

        Vector3 target = pathPoints[index];
        Vector3 toTarget = target - transform.position;

        // Move
        float step = speed * Time.deltaTime;
        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            index++;
            return;
        }

        transform.position += toTarget.normalized * step;

        // Optional face movement direction
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.forward = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
    }
}
