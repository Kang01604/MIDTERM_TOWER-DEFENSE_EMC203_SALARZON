using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int currentWave = 0;
    public int coins = 100;
    private int displayedCoins; // Tracked for the incremental UI animation
    public int health = 10;
    public int maxHealth = 10; 
    public bool isGameActive = true;

    // --- Scale-based Bars & Visuals ---
    [Header("Base Visuals")]
    public MeshRenderer baseRenderer;   
    public RectTransform hpBarRect;     
    public RectTransform ghostHpBarRect;
    
    public Color healthyColor = Color.white;
    public Color criticalColor = Color.red;
    private MaterialPropertyBlock basePropBlock;

    // --- ECONOMY TRACKING ---
    public int towersBuilt = 0; 

    [Header("Configuration")]
    public RectTransform coinUITarget;  
    public TextMeshProUGUI coinText; 
    public TextMeshProUGUI waveText; 
    public Camera mainCam;

    [Header("Path System")]
    public Transform[] waypoints; 

    // GLOBAL ENEMY LIST
    public List<Enemy> activeEnemies = new List<Enemy>();

    private Vector3 originalCoinScale;
    private Color originalTextColor;
    private Coroutine impactCoroutine;
    private Coroutine coinCountCoroutine;

    void Awake()
    {
        Instance = this;
        basePropBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        displayedCoins = coins; // Initialize display value
        coinText.text = coins.ToString();
        maxHealth = health; 
        
        if(hpBarRect) hpBarRect.localScale = Vector3.one;
        if(ghostHpBarRect) ghostHpBarRect.localScale = Vector3.one;

        if (coinUITarget) originalCoinScale = coinUITarget.localScale;
        if (coinText) originalTextColor = coinText.color;
        if (waveText) waveText.text = "WAVE: 0";

        if (waypoints.Length == 0)
        {
            Debug.LogError("No Waypoints assigned!");
            return;
        }

        StartCoroutine(WaveRoutine());
    }

    void Update()
    {
        if (ghostHpBarRect && hpBarRect)
        {
            float targetX = hpBarRect.localScale.x;
            float currentX = ghostHpBarRect.localScale.x;

            if (currentX > targetX)
            {
                float newX = Mathf.Lerp(currentX, targetX, Time.unscaledDeltaTime * 2.0f); // Changed to unscaled for UI safety
                ghostHpBarRect.localScale = new Vector3(newX, 1f, 1f);
            }
        }
    }

    public int GetNextTowerCost()
    {
        return 50 * (towersBuilt + 1);
    }
    
    public void DecrementTowerCount()
    {
        if (towersBuilt > 0) towersBuilt--;
    }

    IEnumerator WaveRoutine()
    {
        // WAIT FOR START MENU TO CLOSE
        yield return new WaitUntil(() => MenuController.Instance != null && MenuController.Instance.isGameStarted);
        
        yield return new WaitForSeconds(2f); 

        for (int w = 1; w <= 10; w++)
        {
            currentWave = w;
            if (waveText) waveText.text = "Wave: " + w.ToString();

            int enemyCount = Mathf.RoundToInt(5 * Mathf.Pow(1.3f, w));
            float spawnDelay = Mathf.Max(0.3f, 1.5f - (w * 0.1f)); 

            for (int i = 0; i < enemyCount; i++)
            {
                if (!isGameActive) break;
                
                // Pause spawning if game is paused manually (double check)
                while (Time.timeScale == 0) yield return null;

                PoolType typeToSpawn = PoolType.Enemy_Grunt; 
                int roll = UnityEngine.Random.Range(0, 100); 

                if (w < 3) typeToSpawn = PoolType.Enemy_Grunt;
                else if (w < 6)
                {
                    if (roll < 30) typeToSpawn = PoolType.Enemy_Fast;
                    else typeToSpawn = PoolType.Enemy_Grunt;
                }
                else
                {
                    if (roll < 20) typeToSpawn = PoolType.Enemy_Tank;
                    else if (roll < 50) typeToSpawn = PoolType.Enemy_Fast;
                    else typeToSpawn = PoolType.Enemy_Grunt;
                }

                GameObject obj = ObjectPool.Instance.Spawn(typeToSpawn, waypoints[0].position);
                if (obj != null)
                {
                    Enemy e = obj.GetComponent<Enemy>();
                    e.Initialize(w, waypoints);
                    if (!activeEnemies.Contains(e)) activeEnemies.Add(e);
                }
                
                yield return new WaitForSeconds(spawnDelay);
            }

            yield return new WaitForSeconds(6f);
        }

        //  WIN CONDITION CHECK
        // Loops have finished (Wave 10 Spawned).
        // Now wait for all active enemies to be dead.
        while (activeEnemies.Count > 0)
        {
            yield return null;
        }

        // If HP is still above 0 and game is active, WE WIN.
        if (health > 0 && isGameActive)
        {
            isGameActive = false; // Stop internal game logic
            if (MenuController.Instance != null)
            {
                MenuController.Instance.TriggerWin();
            }
        }
    }

    public void OnEnemyKilled(Enemy e)
    {
        activeEnemies.Remove(e);
        e.gameObject.SetActive(false);
        SpawnCoinEffect(e.transform.position, e.GetGoldReward());
    }

    public void OnEnemyReachedEnd(Enemy e)
    {
        activeEnemies.Remove(e);
        e.gameObject.SetActive(false);
        
        health--;

        // Trigger the Base Damage Sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBaseDamageSound();
        }

        float hpPct = (float)health / maxHealth;
        if (hpBarRect) hpBarRect.localScale = new Vector3(hpPct, 1f, 1f);
        
        if (baseRenderer)
        {
            baseRenderer.GetPropertyBlock(basePropBlock);
            Color newColor = Color.Lerp(criticalColor, healthyColor, hpPct);
            basePropBlock.SetColor("_BaseColor", newColor);
            baseRenderer.SetPropertyBlock(basePropBlock);
        }

        StartCoroutine(ShakeCamera());
        
        if (health <= 0)
        {
            isGameActive = false;
            if (MenuController.Instance != null)
            {
                MenuController.Instance.TriggerGameOver();
            }
        }
    }

    void SpawnCoinEffect(Vector3 startPos, int goldAmount)
    {
        GameObject coin = ObjectPool.Instance.Spawn(PoolType.CoinEffect, startPos);
        StartCoroutine(AnimateCoin(coin, startPos, goldAmount));
    }

    IEnumerator AnimateCoin(GameObject coin, Vector3 startPos, int amount)
    {
        Vector3 endPos = mainCam.ScreenToWorldPoint(coinUITarget.position);
        endPos.z = 0; 

        Vector3 controlPoint = (startPos + endPos) / 2;
        controlPoint += (Vector3)UnityEngine.Random.insideUnitCircle * 2f;

        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * 1.5f; // Use unscaled just in case
            coin.transform.position = GameMath.GetBezierPoint(startPos, controlPoint, endPos, t);
            yield return null;
        }

        coin.SetActive(false);
        coins += amount; // Add to the logical total

        // Start the incremental UI update instead of jumping instantly
        if (coinCountCoroutine != null) StopCoroutine(coinCountCoroutine);
        coinCountCoroutine = StartCoroutine(UpdateCoinTextIncremental());

        // Play the collection SFX exactly when the coin hits the UI
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinCollectSound();
        }

        if (impactCoroutine != null) StopCoroutine(impactCoroutine);
        impactCoroutine = StartCoroutine(CoinImpactAnim());
    }

    // Coroutine to handle the one-by-one incrementing
    // Originally was not in the script (coins originally go up without the animation)
    // Added for this "coin UI will go up slowly via lerp when the coin reaches the UI element (e.g. 10 coins = 0.. 1.. 2.. 3.. 4 ... 10 coins)"
    IEnumerator UpdateCoinTextIncremental()
    {
        while (displayedCoins < coins)
        {
            displayedCoins++;
            coinText.text = displayedCoins.ToString();
            
            // Adjust this delay to control how fast the "one by one" count happens
            yield return new WaitForSecondsRealtime(0.05f);
        }

        // Ensure they match exactly at the end
        displayedCoins = coins;
        coinText.text = coins.ToString();
    }

    IEnumerator CoinImpactAnim()
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 punchScale = originalCoinScale * 1.3f; 
        Color glowColor = Color.yellow; 

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Changed to unscaled
            float t = elapsed / duration;
            if(coinUITarget) coinUITarget.localScale = Vector3.Lerp(originalCoinScale, punchScale, t);
            if(coinText) coinText.color = Color.Lerp(originalTextColor, glowColor, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Changed to unscaled
            float t = elapsed / duration;
            if (coinUITarget) coinUITarget.localScale = Vector3.Lerp(punchScale, originalCoinScale, t);
            if (coinText) coinText.color = Color.Lerp(glowColor, originalTextColor, t);
            yield return null;
        }

        if (coinUITarget) coinUITarget.localScale = originalCoinScale;
        if (coinText) coinText.color = originalTextColor;
    }

    // PREVENT INFINITE CAMERA SHAKE
    IEnumerator ShakeCamera()
    {
        Vector3 original = new Vector3(0,0,-10);
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-0.2f, 0.2f);
            float y = UnityEngine.Random.Range(-0.2f, 0.2f);
            mainCam.transform.position = original + new Vector3(x,y,0);
            
            // USED UNSCALED DELTA TIME
            // This ensures the counter goes up even if Time.timeScale is 0 (GameOver)
            elapsed += Time.unscaledDeltaTime;
            
            yield return null;
        }
        mainCam.transform.position = original;
    }


    // DEBUG: VISUALIZE PATHS IN EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 3) return;

        for (int i = 0; i < waypoints.Length - 2; i += 2)
        {
            if(waypoints[i] == null || waypoints[i+1] == null || waypoints[i+2] == null) continue;

            Vector3 p0 = waypoints[i].position;
            // Removed p1 local variable since it's only used for Gizmos.DrawLine
            Vector3 p1 = waypoints[i+1].position; 
            Vector3 p2 = waypoints[i+2].position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);

            Gizmos.color = Color.green;
            Vector3 prevPos = p0;

            for (float t = 0; t <= 1; t += 0.05f)
            {
                Vector3 newPos = GameMath.GetBezierPoint(p0, p1, p2, t);
                Gizmos.DrawLine(prevPos, newPos);
                prevPos = newPos;
            }
        }
        
        Gizmos.color = Color.blue;
        foreach(var t in waypoints)
        {
            if(t != null) Gizmos.DrawWireSphere(t.position, 0.3f);
        }
    }
}
