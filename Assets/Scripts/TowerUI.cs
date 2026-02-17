using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Required for IEnumerator

public class TowerUI : MonoBehaviour
{
    public static TowerUI Instance;

    [Header("UI Elements")]
    public GameObject uiPanel;
    public Button upgradeButton;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statsText;
    
    // --- BURST UI ---
    [Header("Burst Elements")]
    public Button burstButton; 
    public TextMeshProUGUI burstCostText;
    
    [Header("Settings")]
    public Vector2 uiOffset = new Vector2(120, 120); 

    private Tower selectedTower;
    private Camera mainCam;
    private float lastClickTime;
    
    // Animation Tracking
    private Coroutine currentAnim;
    // Store the scale set in the inspector (e.g., 0.75)
    private Vector3 defaultScale;

    void Awake()
    {
        Instance = this;

        // Capture the desired scale BEFORE hiding the panel ---
        if (uiPanel != null)
        {
            defaultScale = uiPanel.transform.localScale;
        }
        // ---------------------------------------------------------------

        uiPanel.SetActive(false);
        mainCam = Camera.main;
        
        // Auto-wire Upgrade
        if (upgradeButton == null) upgradeButton = uiPanel.GetComponentInChildren<Button>();
        if (costText == null && upgradeButton != null) costText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClick);
        }

        // Auto-wire Burst
        if (burstButton != null)
        {
            burstButton.onClick.RemoveAllListeners();
            burstButton.onClick.AddListener(OnBurstClick);
        }
    }

    void Update()
    {
        if (selectedTower != null && uiPanel.activeSelf)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(selectedTower.transform.position);
            uiPanel.transform.position = screenPos + (Vector3)uiOffset;

            // --- UPGRADE BUTTON CHECK ---
            if (selectedTower.currentLevel < 3)
            {
                int cost = selectedTower.GetUpgradeCost();
                bool canAfford = GameManager.Instance.coins >= cost;
                
                if (upgradeButton.interactable != canAfford)
                {
                    upgradeButton.interactable = canAfford;
                }
            }

            // --- BURST BUTTON CHECK ---
            if (burstButton != null)
            {
                int bCost = selectedTower.GetBurstCost();
                
                bool isOnCooldown = selectedTower.burstCooldownTimer > 0;
                bool canAffordBurst = GameManager.Instance.coins >= bCost;
                bool isReady = !selectedTower.isBurstActive && !isOnCooldown;
                
                burstButton.interactable = canAffordBurst && isReady;
                
                if (burstCostText)
                {
                    if (selectedTower.isBurstActive)
                    {
                        burstCostText.text = "ACTIVE!";
                    }
                    else if (isOnCooldown)
                    {
                        burstCostText.text = Mathf.Ceil(selectedTower.burstCooldownTimer) + "s";
                    }
                    else
                    {
                        burstCostText.text = "BURST $" + bCost;
                    }
                }
            }
        }
    }

    public void SelectTower(Tower t)
    {
        if (selectedTower != null && selectedTower != t)
        {
            selectedTower.ToggleRangeVisual(false);
        }

        selectedTower = t;
        selectedTower.ToggleRangeVisual(true);
        UpdateUI();
        
        // ANIMATION INTEGRATION
        if (currentAnim != null) StopCoroutine(currentAnim);
        
        // Force position update immediately
        Vector3 screenPos = mainCam.WorldToScreenPoint(selectedTower.transform.position);
        uiPanel.transform.position = screenPos + (Vector3)uiOffset;
        
        // Start the Pop-In
        currentAnim = StartCoroutine(PanelPopIn());
    }

    public void Deselect()
    {
        if (selectedTower != null)
        {
            selectedTower.ToggleRangeVisual(false);
            selectedTower = null;
        }
        uiPanel.SetActive(false);
    }

    // UPDATED ANIMATION LOGIC
    IEnumerator PanelPopIn()
    {
        uiPanel.SetActive(true);
        RectTransform rect = uiPanel.GetComponent<RectTransform>();
        
        // Start at 30% of the DEFAULT scale, not Vector3.one
        Vector3 startScale = defaultScale * 0.3f;
        
        float randomTilt = Random.Range(-10f, 10f);
        
        rect.localScale = startScale;
        rect.localRotation = Quaternion.Euler(0, 0, randomTilt);

        float duration = 0.35f;
        float t = 0;

        while(t < 1)
        {
            t += Time.unscaledDeltaTime / duration;
            
            // Elastic Out Math
            float c4 = (2 * Mathf.PI) / 3;
            float curve = (t == 0) ? 0 : (t == 1) ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;

            // Lerp towards defaultScale
            rect.localScale = Vector3.Lerp(startScale, defaultScale, curve);
            yield return null;
        }
        // Ensure final scale is defaultScale
        rect.localScale = defaultScale;
    }

    void UpdateUI()
    {
        if (selectedTower == null) return;

        // Upgrade Text
        if (selectedTower.currentLevel >= 3)
        {
            costText.text = "MAX";
            upgradeButton.interactable = false;
        }
        else
        {
            int cost = selectedTower.GetUpgradeCost();
            costText.text = "UPG $" + cost;
            upgradeButton.interactable = GameManager.Instance.coins >= cost;
        }
        
        if (statsText) statsText.text = $"{selectedTower.type} (Lvl {selectedTower.currentLevel})";
    }

    public void OnUpgradeClick()
    {
        if (Time.time - lastClickTime < 0.2f) return;
        lastClickTime = Time.time;

        if (selectedTower == null) return;

        int cost = selectedTower.GetUpgradeCost();

        if (GameManager.Instance.coins >= cost && selectedTower.currentLevel < 3)
        {
            GameManager.Instance.coins -= cost;
            GameManager.Instance.coinText.text = GameManager.Instance.coins.ToString();
            
            selectedTower.Upgrade();
            UpdateUI(); 
        }
    }

    public void OnBurstClick()
    {
        if (Time.time - lastClickTime < 0.2f) return;
        lastClickTime = Time.time;

        if (selectedTower == null) return;
        
        int cost = selectedTower.GetBurstCost();
        
        // Final Check before spending money
        if (selectedTower.burstCooldownTimer > 0 || selectedTower.isBurstActive) return;

        if (GameManager.Instance.coins >= cost)
        {
            GameManager.Instance.coins -= cost;
            GameManager.Instance.coinText.text = GameManager.Instance.coins.ToString();
            
            selectedTower.ActivateBurst();
        }
    }
}