using UnityEngine;

public class TowerInspectPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    private Tower current;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Toggle(Tower tower)
    {
        if (panelRoot == null) return;

        // If clicking same tower, toggle closed
        if (current == tower && panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
            current = null;
            return;
        }

        current = tower;
        panelRoot.SetActive(true);

        // Later: populate UI with tower stats, upgrade buttons, sell, etc.
        Debug.Log($"[Inspect] Opened panel for tower: {tower.name}");
    }

    public void Close()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
        current = null;
    }
}
