using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementController : MonoBehaviour
{
    [Serializable]
    public class PlaceableTowerEntry
    {
        public string displayName;
        public GameObject towerPrefab;
        public Sprite overrideIcon;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                if (towerPrefab == null)
                    return "Tower";

                TowerIdentity id = towerPrefab.GetComponent<TowerIdentity>();
                if (id != null && !string.IsNullOrWhiteSpace(id.DisplayName))
                    return id.DisplayName;

                return towerPrefab.name;
            }
        }

        public Sprite Icon
        {
            get
            {
                if (overrideIcon != null)
                    return overrideIcon;

                if (towerPrefab == null)
                    return null;

                TowerIdentity id = towerPrefab.GetComponent<TowerIdentity>();
                return id != null ? id.Icon : null;
            }
        }

        public int Cost
        {
            get
            {
                if (towerPrefab == null)
                    return 0;

                TowerCost cost = towerPrefab.GetComponent<TowerCost>();
                return cost != null ? Mathf.Max(0, cost.Cost) : 0;
            }
        }
    }

    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private InspectPanelUI inspectPanel;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlacementRadialMenu radialMenu;


    private PlayerControls controls;
    private EconomyManager economy;

    [Header("Raycast (CENTER RAY)")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask tileMask;

    [Header("Placeable Towers")]
    [SerializeField] private List<PlaceableTowerEntry> placeableTowers = new List<PlaceableTowerEntry>();

    [Header("Placement Rules")]
    [SerializeField] private bool towersBlockEnemies = true;
    [SerializeField] private bool enforcePath = true;

    [Header("Path Check")]
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);

    [Header("Ghost Preview")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material invalidGhostMaterial;
    [SerializeField] private float ghostYOffset = 0.05f;

    [Header("Radial State Prep")]
    [SerializeField] private bool radialOpen;
    [SerializeField] private float radialOpenHoldTime = 0.2f;
    [SerializeField] private float radialStickDeadzone = 0.35f;

    private GameObject ghost;
    private GridTile hoveredTile;

    private Renderer[] ghostRenderers;
    private Material validMatRuntime;
    private Material invalidMatRuntime;

    private GridTile lastTile;
    private int lastSeenPathVersion = -1;
    private bool lastWasValid;

    private int selectedTowerIndex = -1;

    private bool secondaryHeld;
    private bool holdTriggeredRadialOpen;
    private float secondaryHeldTime;

    public bool HasTowerSelected => selectedTowerIndex >= 0 && selectedTowerIndex < placeableTowers.Count;
    public bool IsRadialOpen => radialOpen;
    public int SelectedTowerIndex => selectedTowerIndex;

    public PlaceableTowerEntry SelectedTowerEntry
    {
        get
        {
            if (!HasTowerSelected)
                return null;

            return placeableTowers[selectedTowerIndex];
        }
    }

    private void Awake()
    {
        economy = FindFirstObjectByType<EconomyManager>();

        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<InspectPanelUI>();
        if (playerLook == null) playerLook = FindFirstObjectByType<PlayerLook>();
        if (radialMenu == null) radialMenu = FindFirstObjectByType<PlacementRadialMenu>();

        controls = new PlayerControls();

        CreateGhostIfPossible();
        HideGhostImmediate();

        if (radialMenu != null)
            radialMenu.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (economy != null)
            economy.OnMoneyChanged += HandleMoneyChanged;

        PauseState.OnPauseChanged += HandlePauseChanged;

        controls.Enable();
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
        controls.Player.SecondaryClick.started += OnSecondaryStarted;
        controls.Player.SecondaryClick.canceled += OnSecondaryCanceled;

        secondaryHeld = false;
        holdTriggeredRadialOpen = false;
        secondaryHeldTime = 0f;

        radialOpen = false;
        SetLookBlocked(false);
        RestoreGameplayCursorIfNeeded();
        HideGhostImmediate();
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnMoneyChanged -= HandleMoneyChanged;

        PauseState.OnPauseChanged -= HandlePauseChanged;

        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Player.SecondaryClick.started -= OnSecondaryStarted;
        controls.Player.SecondaryClick.canceled -= OnSecondaryCanceled;
        controls.Disable();

        CloseRadialInternal(false);
        SetLookBlocked(false);
        RestoreGameplayCursorIfNeeded();
        HideGhostImmediate();
        hoveredTile = null;
    }

    private void Update()
    {
        if (PauseState.IsPaused)
        {
            hoveredTile = null;
            HideGhostImmediate();
            return;
        }

        UpdateSecondaryHold();

        if (!radialOpen)
        {
            UpdateHoverTileCenterRay();
        }
        else
        {
            hoveredTile = null;
            UpdateRadialHighlight();
        }

        UpdateGhostVisualsAndPosition();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (radialOpen) return;
        if (!HasTowerSelected) return;
        if (hoveredTile == null) return;

        // If tower exists, inspector system will handle selection.
        if (hoveredTile.IsOccupied) return;

        TryPlace();
    }

    private void OnSecondaryStarted(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused)
            return;

        // If a tower is selected, a press of SecondaryClick should deselect immediately,
        // and not open the radial on the same press.
        if (HasTowerSelected)
        {
            ClearSelectedTower();
            secondaryHeld = false;
            holdTriggeredRadialOpen = false;
            secondaryHeldTime = 0f;
            return;
        }

        secondaryHeld = true;
        holdTriggeredRadialOpen = false;
        secondaryHeldTime = 0f;
    }

    private void OnSecondaryCanceled(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused)
            return;

        bool radialWasOpen = radialOpen;

        secondaryHeld = false;
        secondaryHeldTime = 0f;

        if (radialWasOpen)
        {
            holdTriggeredRadialOpen = false;
            ConfirmRadialSelection();
            return;
        }

        holdTriggeredRadialOpen = false;
    }

    private void UpdateSecondaryHold()
    {
        if (!secondaryHeld)
            return;

        if (HasTowerSelected)
            return;

        if (radialOpen)
            return;

        secondaryHeldTime += Time.unscaledDeltaTime;

        if (!holdTriggeredRadialOpen && secondaryHeldTime >= radialOpenHoldTime)
        {
            holdTriggeredRadialOpen = true;
            OpenRadialInternal();
        }
    }

    private void HandlePauseChanged(bool paused)
    {
        if (!paused)
            return;

        secondaryHeld = false;
        holdTriggeredRadialOpen = false;
        secondaryHeldTime = 0f;

        CloseRadialInternal(false);
        hoveredTile = null;
        HideGhostImmediate();
    }

    private void HandleMoneyChanged(int _)
    {
        if (ghost == null || !ghost.activeSelf) return;
        if (hoveredTile == null) return;
        if (!HasTowerSelected) return;

        bool nowValid = IsPlacementValid(hoveredTile);
        if (nowValid != lastWasValid)
        {
            lastWasValid = nowValid;
            ApplyGhostMaterial(lastWasValid);
        }
    }

    public void ClearSelectionAndHideGhost()
    {
        hoveredTile = null;
        lastTile = null;
        ClearSelectedTower();
        HideGhostImmediate();
    }

    public void SelectTowerByIndex(int index)
    {
        if (index < 0 || index >= placeableTowers.Count)
        {
            ClearSelectedTower();
            return;
        }

        if (placeableTowers[index] == null || placeableTowers[index].towerPrefab == null)
        {
            ClearSelectedTower();
            return;
        }

        selectedTowerIndex = index;
        RebuildGhostFromSelectedTower();
        lastTile = null;
        hoveredTile = null;
    }

    public void ClearSelectedTower()
    {
        selectedTowerIndex = -1;
        lastTile = null;
        hoveredTile = null;
        HideGhostImmediate();
    }

    public IReadOnlyList<PlaceableTowerEntry> GetPlaceableTowers()
    {
        return placeableTowers;
    }

    private void OpenRadialInternal()
    {
        if (PauseState.IsPaused)
            return;

        if (HasTowerSelected)
            return;

        radialOpen = true;

        if (inspectPanel != null)
            inspectPanel.ClearSelection();

        SetLookBlocked(true);
        UnlockCursorForUIIfNeeded();
        HideGhostImmediate();

        if (radialMenu != null)
        {
            radialMenu.BuildRadial();
            radialMenu.Show();
        }
    }

    private void CloseRadialInternal(bool restoreCursor)
    {
        radialOpen = false;

        if (radialMenu != null)
            radialMenu.Hide();

        SetLookBlocked(false);

        if (restoreCursor)
            RestoreGameplayCursorIfNeeded();
    }

    private void UpdateRadialHighlight()
    {
        if (!radialOpen || radialMenu == null)
            return;

        Vector2 direction = GetRadialInputDirection();
        radialMenu.UpdateHighlight(direction);
    }

    private Vector2 GetRadialInputDirection()
    {
        Vector2 stickLook = controls.Player.RadialSelection.ReadValue<Vector2>();

        if (stickLook.sqrMagnitude >= radialStickDeadzone * radialStickDeadzone)
            return stickLook.normalized;

        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 mouseDir = mousePos - screenCenter;

            if (mouseDir.sqrMagnitude > 0.01f)
                return mouseDir.normalized;
        }

        return Vector2.zero;
    }

    private void ConfirmRadialSelection()
    {
        if (!radialOpen)
            return;

        int highlighted = 0;

        if (radialMenu != null)
            highlighted = radialMenu.GetHighlightedIndex();

        CloseRadialInternal(true);

        // 0 is always Back
        if (highlighted == 0)
        {
            ClearSelectedTower();
            return;
        }

        int towerIndex = highlighted - 1;
        SelectTowerByIndex(towerIndex);
    }

    private void UpdateHoverTileCenterRay()
    {
        hoveredTile = null;

        if (!HasTowerSelected)
            return;

        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            hoveredTile = hit.collider.GetComponentInParent<GridTile>();
        }
    }

    private void UpdateGhostVisualsAndPosition()
    {
        if (!HasTowerSelected || radialOpen || PauseState.IsPaused)
        {
            HideGhostImmediate();
            return;
        }

        if (ghost == null)
        {
            CreateGhostIfPossible();
            if (ghost == null)
                return;
        }

        if (hoveredTile == null)
        {
            HideGhostImmediate();
            return;
        }

        // HARD BLOCKS (ghost should not appear at all)
        if (!hoveredTile.IsBuildable || hoveredTile.IsOccupied || hoveredTile.Terrain == TerrainType.Blocked)
        {
            HideGhostImmediate();
            lastTile = hoveredTile;
            lastSeenPathVersion = PathChangeBroadcaster.Version;
            return;
        }

        ghost.SetActive(true);

        Vector3 tilePos = hoveredTile.transform.position;
        ghost.transform.position = tilePos + Vector3.up * ghostYOffset;

        int currentPathVersion = PathChangeBroadcaster.Version;
        bool tileChanged = hoveredTile != lastTile;
        bool pathChanged = currentPathVersion != lastSeenPathVersion;

        if (tileChanged || pathChanged)
        {
            lastTile = hoveredTile;
            lastSeenPathVersion = currentPathVersion;

            bool validPlacement = IsPlacementValid(hoveredTile);
            ApplyGhostMaterial(validPlacement);
        }
    }

    private void HideGhostImmediate()
    {
        if (ghost != null)
            ghost.SetActive(false);
    }

    private void CreateGhostIfPossible()
    {
        if (ghost != null)
            Destroy(ghost);

        ghost = null;
        ghostRenderers = null;

        GameObject prefab = GetSelectedTowerPrefab();
        if (prefab == null)
            return;

        ghost = Instantiate(prefab);
        ghost.name = prefab.name + "_GhostPreview";

        DestroyComponentsForGhost(ghost);

        SetLayerRecursively(ghost, gameObject.layer);

        ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);

        if (ghostMaterial != null)
            validMatRuntime = new Material(ghostMaterial);

        if (invalidGhostMaterial != null)
            invalidMatRuntime = new Material(invalidGhostMaterial);

        ApplyGhostMaterial(true);
        ghost.SetActive(false);
    }

    private void RebuildGhostFromSelectedTower()
    {
        CreateGhostIfPossible();
        HideGhostImmediate();
    }

    private GameObject GetSelectedTowerPrefab()
    {
        if (!HasTowerSelected)
            return null;

        PlaceableTowerEntry entry = placeableTowers[selectedTowerIndex];
        return entry != null ? entry.towerPrefab : null;
    }

    private int GetSelectedTowerCost()
    {
        if (!HasTowerSelected)
            return 0;

        PlaceableTowerEntry entry = placeableTowers[selectedTowerIndex];
        return entry != null ? entry.Cost : 0;
    }

    private bool IsPlacementValid(GridTile tile)
    {
        if (tile == null) return false;
        if (!HasTowerSelected) return false;

        GameObject selectedPrefab = GetSelectedTowerPrefab();
        if (selectedPrefab == null) return false;

        if (!tile.IsBuildable) return false;
        if (tile.IsOccupied) return false;
        if (tile.Terrain == TerrainType.Blocked) return false;

        int cost = GetSelectedTowerCost();
        if (economy != null && economy.Money < cost)
            return false;

        if (enforcePath && towersBlockEnemies && !WouldStillHavePathIfPlacedHere(tile))
            return false;

        return true;
    }

    private bool ShouldHideGhostForTile(GridTile tile)
    {
        if (tile == null) return true;
        if (!HasTowerSelected) return true;

        if (!tile.IsBuildable) return true;
        if (tile.IsOccupied) return true;
        if (tile.Terrain == TerrainType.Blocked) return true;

        return false;
    }

    private void ApplyGhostMaterial(bool valid)
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0) return;

        Material m = valid ? validMatRuntime : invalidMatRuntime;
        if (m == null) m = validMatRuntime;
        if (m == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
            ghostRenderers[i].sharedMaterial = m;
    }

    private void TryPlace()
    {
        if (hoveredTile == null) return;

        GameObject selectedPrefab = GetSelectedTowerPrefab();
        if (selectedPrefab == null) return;

        if (hoveredTile.IsOccupied) return;
        if (!IsPlacementValid(hoveredTile)) return;

        int paidCost = 0;

        TowerCost costComp = selectedPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            paidCost = Mathf.Max(0, costComp.Cost);
            if (!economy.TrySpendMoney(paidCost))
                return;
        }

        Vector3 pos = hoveredTile.transform.position;
        GameObject placed = Instantiate(selectedPrefab, pos, Quaternion.identity);

        TowerValueLedger ledger = placed.GetComponent<TowerValueLedger>();
        if (ledger == null) ledger = placed.AddComponent<TowerValueLedger>();
        ledger.AddSpend(paidCost);

        hoveredTile.SetOccupiedTower(placed);

        if (towersBlockEnemies)
            hoveredTile.SetBlocksEnemies(true);

        PathChangeBroadcaster.Bump();

        if (inspectPanel != null)
        {
            var id = placed.GetComponentInChildren<TowerIdentity>();
            var up = placed.GetComponentInChildren<TowerUpgradeState>();
            if (id != null) inspectPanel.SetSelectedTower(id, up, hoveredTile);
            else inspectPanel.SetSelectedTile(hoveredTile);
        }

        lastTile = null;
    }

    private bool WouldStillHavePathIfPlacedHere(GridTile tile)
    {
        if (grid == null || pathfinder == null) return true;

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);
        if (startTile == null || goalTile == null) return true;

        if (tile.Terrain == TerrainType.Blocked) return false;

        bool originalBlocksEnemies = tile.BlocksEnemies;

        tile.SetBlocksEnemies(true);
        var path = pathfinder.FindPathAStar(startTile, goalTile);

        tile.SetBlocksEnemies(originalBlocksEnemies);

        return path != null && path.Count > 0;
    }

    private void DestroyComponentsForGhost(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
            Destroy(behaviours[i]);

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
            Destroy(rigidbodies[i]);
    }

    private void SetLookBlocked(bool blocked)
    {
        if (playerLook != null)
            playerLook.SetLookBlocked(blocked);
    }

    private void UnlockCursorForUIIfNeeded()
    {
        if (playerLook != null)
            playerLook.UnlockCursorForUI();
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RestoreGameplayCursorIfNeeded()
    {
        if (PauseState.IsPaused)
            return;

        if (playerLook != null)
            playerLook.RestoreGameplayCursor();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}