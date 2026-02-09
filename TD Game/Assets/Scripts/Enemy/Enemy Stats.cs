using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;

    public float MoveSpeed => moveSpeed;
}
