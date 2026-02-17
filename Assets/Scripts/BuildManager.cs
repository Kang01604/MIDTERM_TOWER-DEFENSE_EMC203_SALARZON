using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("References")]
    public Image darkOverlay; 
    public LineRenderer rangePreviewLine; 
    public Plot[] allPlots;
    
    // Tooltip References
    public TextMeshProUGUI costPreviewText; 
    public RectTransform costPreviewRect;   

    [Header("Prefabs")]
    public Tower dartPrefab;
    public Tower bombPrefab;
    public Tower slowPrefab;
    
    // Highlight Prefab
    public GameObject removeHighlightPrefab; // Assign UI Circle Prefab here

    private TowerType selectedType;
    private bool isBuildMode = false;
    private bool isRemoveMode = false;
    
    private GameObject ghostObject; 
    private MaterialPropertyBlock ghostPropBlock;

    // Shader Reference
    private Material overlayMat;

    // List to track active highlights
    private List<GameObject> activeHighlights = new List<GameObject>();

    void Awake() 
    { 
        Instance = this;
        ghostPropBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        allPlots = FindObjectsByType<Plot>(FindObjectsSortMode.None);
        
        if (darkOverlay != null)
        {
            // Clone the material so we don't change the actual asset
            overlayMat = Instantiate(darkOverlay.material);
            darkOverlay.material = overlayMat;
            darkOverlay.raycastTarget = false; 
        }

        SetOverlay(false);
        
        // Hide Cost Text initially
        if (costPreviewText) costPreviewText.gameObject.SetActive(false);

        if(rangePreviewLine) 
        {
            rangePreviewLine.enabled = false;
            rangePreviewLine.transform.position = Vector3.zero;
        }
    }

    // BUTTON CLICK EVENTS
    public void OnClickDart() => StartBuildMode(TowerType.Arrow);
    public void OnClickBomb() => StartBuildMode(TowerType.Fire);
    public void OnClickSlow() => StartBuildMode(TowerType.Frost);

    public void OnClickRemove()
    {
        if (isRemoveMode) 
        {
            CancelInteraction();
        }
        else
        {
            StartRemoveMode();
        }
    }

    void StartBuildMode(TowerType type)
    {
        if (isBuildMode && selectedType == type) { CancelInteraction(); return; }

        ClearHighlights(); // Clean up any remove highlights

        selectedType = type;
        isBuildMode = true;
        isRemoveMode = false;
        
        if(TowerUI.Instance) TowerUI.Instance.Deselect();
        SetOverlay(true);
        
        CreateGhost(GetPrefab(type));

        if(rangePreviewLine)
        {
            rangePreviewLine.enabled = true;
            UpdatePreviewCircle(Vector3.zero, GetRange(type));
        }
    }

    void StartRemoveMode()
    {
        isBuildMode = false;
        isRemoveMode = true;

        if(TowerUI.Instance) TowerUI.Instance.Deselect();
        if (ghostObject != null) Destroy(ghostObject);
        if (rangePreviewLine) rangePreviewLine.enabled = false;

        SetOverlay(true);
        RefreshRemoveHighlights(); // Spawn rings on towers
    }

    void CreateGhost(Tower prefab)
    {
        if (ghostObject != null) Destroy(ghostObject);

        ghostObject = Instantiate(prefab.gameObject);
        Destroy(ghostObject.GetComponent<Tower>());
        Collider col = ghostObject.GetComponent<Collider>();
        if(col) Destroy(col);

        MeshRenderer mr = ghostObject.GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.GetPropertyBlock(ghostPropBlock);
            Color ghostColor = mr.sharedMaterial.color;
            ghostColor.a = 0.5f; 
            ghostColor += new Color(0.2f, 0.2f, 0.2f, 0f); 
            ghostPropBlock.SetColor("_BaseColor", ghostColor);
            mr.SetPropertyBlock(ghostPropBlock);
        }
    }

    public void CancelInteraction()
    {
        isBuildMode = false;
        isRemoveMode = false;
        SetOverlay(false);
        ClearHighlights(); // Remove rings
        
        if (ghostObject != null) Destroy(ghostObject);
        if (rangePreviewLine) rangePreviewLine.enabled = false;
    }

    void SetOverlay(bool active)
    {
        if (darkOverlay)
        {
            // We enable/disable the object logic
            darkOverlay.gameObject.SetActive(active);
            
            // If active, we ensure the alpha is up (the shader handles the hole)
            if (active)
            {
                Color c = darkOverlay.color;
                c.a = 0.5f; // Set base darkness
                darkOverlay.color = c;
            }
        }
        // Original Time Scale Logic
        Time.timeScale = active ? 0.1f : 1.0f;
    }

    void Update()
    {
        // Calculate Mouse World Pos
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Shader Spotlight Logic
        if (darkOverlay != null && darkOverlay.gameObject.activeSelf)
        {
            UpdateSpotlight();
        }

        if (isBuildMode)
        {
            // Update Ghost
            if (ghostObject) ghostObject.transform.position = mouseWorldPos;
            
            // Update Range Line
            if (rangePreviewLine && rangePreviewLine.enabled)
                UpdatePreviewCircle(mouseWorldPos, GetRange(selectedType));
        }
        else if (isRemoveMode)
        {
            // Keep highlights attached to towers (in case camera moves)
            UpdateHighlightPositions();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            // Original CheckClick Logic
            CheckClick(mouseWorldPos);
        }
    }

    // Spotlight Logic
    void UpdateSpotlight()
    {
        Vector2 mousePos = Input.mousePosition;
        float pixelRadius = 0f;

        if (isBuildMode)
        {
            // Calculate Radius based on Tower Range
            float worldRange = GetRange(selectedType);
            
            Vector3 centerWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
            Vector3 edgeWorld = centerWorld + (Vector3.right * worldRange);
            
            Vector2 centerScreen = mousePos;
            Vector2 edgeScreen = Camera.main.WorldToScreenPoint(edgeWorld);
            
            pixelRadius = Vector2.Distance(centerScreen, edgeScreen);
        }
        else if (isRemoveMode)
        {
            // No mouse hole in remove mode (solid dark)
            pixelRadius = 0f; 
        }

        // Send to Shader
        if (overlayMat)
        {
            overlayMat.SetVector("_Center", mousePos);
            overlayMat.SetFloat("_Radius", pixelRadius);
        }
    }

    // Highlight Logic
    void RefreshRemoveHighlights()
    {
        ClearHighlights();

        if (removeHighlightPrefab == null || darkOverlay == null) return;

        foreach (Plot p in allPlots)
        {
            if (p.IsOccupied())
            {
                // Instantiate Highlight on the Canvas (child of Overlay parent)
                GameObject hl = Instantiate(removeHighlightPrefab, darkOverlay.transform.parent);
                hl.transform.SetAsLastSibling(); // Ensure it draws ON TOP of the dark overlay
                
                // Store reference to Plot for updating position
                HighlightData data = hl.AddComponent<HighlightData>();
                data.target = p.transform;

                activeHighlights.Add(hl);
            }
        }
        UpdateHighlightPositions();
    }

    void UpdateHighlightPositions()
    {
        foreach (GameObject hl in activeHighlights)
        {
            if (hl != null)
            {
                HighlightData data = hl.GetComponent<HighlightData>();
                if (data != null && data.target != null)
                {
                    hl.transform.position = Camera.main.WorldToScreenPoint(data.target.position);
                }
            }
        }
    }

    void ClearHighlights()
    {
        foreach (GameObject hl in activeHighlights)
        {
            if (hl != null) Destroy(hl);
        }
        activeHighlights.Clear();
    }

    // Tooltip Logic
    public void ShowCostTooltip(TowerType? type, RectTransform buttonRect, bool isRemoveBtn = false)
    {
        if (costPreviewText == null) return;

        if (isRemoveBtn)
        {
            costPreviewText.text = "REMOVE TOWER";
            costPreviewText.gameObject.SetActive(true);
        }
        else if (type.HasValue)
        {
            int cost = GameManager.Instance.GetNextTowerCost();
            costPreviewText.text = $"COST: {cost}";
            costPreviewText.gameObject.SetActive(true);
        }

        // Position text
        if (costPreviewRect && costPreviewText.gameObject.activeSelf)
        {
            Vector3 btnPos = buttonRect.position;
            costPreviewRect.position = btnPos + new Vector3(0, 105, 0); 
        }
    }

    public void HideCostTooltip()
    {
        if(costPreviewText) costPreviewText.gameObject.SetActive(false);
    }

    void UpdatePreviewCircle(Vector3 center, float radius)
    {
        int segments = 50;
        rangePreviewLine.positionCount = segments;
        rangePreviewLine.useWorldSpace = true; 
        rangePreviewLine.loop = true;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * 360f / segments);
            rangePreviewLine.SetPosition(i, center + new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0));
        }
    }

    float GetRange(TowerType t)
    {
        return (t == TowerType.Arrow) ? 4f : (t == TowerType.Fire) ? 3f : 3.5f;
    }

    void CheckClick(Vector2 point)
    {
        Plot clickedPlot = null;
        foreach(Plot p in allPlots)
        {
            if (p.IsPointInside(point))
            {
                clickedPlot = p;
                break;
            }
        }

        if (clickedPlot != null)
        {
            if (isBuildMode)
            {
                int currentCost = GameManager.Instance.GetNextTowerCost();

                if (!clickedPlot.IsOccupied() && GameManager.Instance.coins >= currentCost)
                {
                    // Deduct Gold
                    GameManager.Instance.coins -= currentCost;
                    GameManager.Instance.coinText.text = GameManager.Instance.coins.ToString(); 
                    
                    // Increment Inflation Counter
                    GameManager.Instance.towersBuilt++;

                    // Build and Pass Cost
                    clickedPlot.BuildTower(GetPrefab(selectedType), currentCost);
                    
                    CancelInteraction();
                }
            }
            else if (isRemoveMode)
            {
                if (clickedPlot.IsOccupied())
                {
                    clickedPlot.RemoveTower();
                    // Decrement Economy Counter
                    GameManager.Instance.DecrementTowerCount();
                    
                    CancelInteraction();
                }
            }
            else 
            {
                if (clickedPlot.IsOccupied())
                {
                    TowerUI.Instance.SelectTower(clickedPlot.occupiedTower);
                }
                else
                {
                    TowerUI.Instance.Deselect();
                }
            }
        }
        else
        {
            if (isBuildMode || isRemoveMode) CancelInteraction();
            else TowerUI.Instance.Deselect();
        }
    }

    Tower GetPrefab(TowerType t)
    {
        switch(t) {
            case TowerType.Arrow: return dartPrefab;
            case TowerType.Fire: return bombPrefab;
            default: return slowPrefab;
        }
    }
}

public class HighlightData : MonoBehaviour
{
    public Transform target;
}