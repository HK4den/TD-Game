using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Optional component on a held visual prefab. The shared lower/raise motion runs
/// in HeldItemView; these callbacks animate the item itself (for example, a book).
/// </summary>
public class HeldItemAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string equippedTrigger = "Equipped";
    [SerializeField] private string unequippedTrigger = "Unequipped";
    [SerializeField] private UnityEvent onEquipped;
    [SerializeField] private UnityEvent onUnequipped;

    public void OnEquipped()
    {
        if (animator != null && !string.IsNullOrEmpty(equippedTrigger))
            animator.SetTrigger(equippedTrigger);
        onEquipped?.Invoke();
    }

    public void OnUnequipped()
    {
        if (animator != null && !string.IsNullOrEmpty(unequippedTrigger))
            animator.SetTrigger(unequippedTrigger);
        onUnequipped?.Invoke();
    }
}
