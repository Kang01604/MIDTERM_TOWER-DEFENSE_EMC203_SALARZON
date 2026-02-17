using UnityEngine;
using System.Collections;

public class Tower : MonoBehaviour
{
    [Header("Settings")]
    public TowerType type;
    public int currentLevel = 1;
    public LineRenderer rangeLine; 
    
    [HideInInspector] 
    public int purchaseCost = 50; 

    // BURST STATE
    public bool isBurstActive = false;
    public float burstCooldownTimer = 0f;

    // Visuals
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Color currentBaseColor; 
    
    // ANIMATION DATA
    private Vector3 baseScale; // Stores the "Resting Size"
    private Coroutine recoilRoutine; // Track recoil to prevent overlapping glitches

    [System.Serializable]
    public struct LevelStats 
    {
        public float range;
        public float fireRate;      
        public float cooldownTime;  
        public float damage;
        public int projectileCount;
        public int maxAmmo;        
        public Color visualColor;
    }

    public LevelStats currentStats;
    private float fireTimer;
    private Coroutine fireRoutine; 

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();

        CalculateStats();
        
        if(rangeLine) rangeLine.enabled = false;

        if (type == TowerType.Fire)
        {
            if (fireRoutine != null) StopCoroutine(fireRoutine);
            fireRoutine = StartCoroutine(FlamethrowerRoutine());
        }
    }

    public int GetUpgradeCost() => purchaseCost + (50 * currentLevel);
    public int GetBurstCost() => 200 * currentLevel;

    public void ActivateBurst()
    {
        if (isBurstActive) return; 
        StartCoroutine(BurstRoutine());
        burstCooldownTimer = 20f;
    }

    IEnumerator BurstRoutine()
    {
        isBurstActive = true;
        SetColor(Color.red); 
        yield return new WaitForSeconds(10f);
        isBurstActive = false;
        SetColor(currentBaseColor); 
    }

    public void Upgrade()
    {
        if (currentLevel < 3)
        {
            currentLevel++;
            CalculateStats();
        }
    }

    void CalculateStats()
    {
        // Colors
        Color targetColor = Color.white;
        if (type == TowerType.Arrow) targetColor = Color.yellow;      
        else if (type == TowerType.Fire) targetColor = new Color(1f, 0.5f, 0f); 
        else if (type == TowerType.Frost) targetColor = Color.cyan;   

        // Tint Logic
        if (currentLevel == 1) currentBaseColor = Color.white;
        else if (currentLevel == 2) currentBaseColor = Color.Lerp(Color.white, targetColor, 0.5f);
        else currentBaseColor = targetColor;

        // Size Logic
        float baseSize = (type == TowerType.Frost) ? 2.5f : 1.75f;
        float sizeMultiplier = (currentLevel == 1) ? 1.0f : (currentLevel == 2) ? 1.1f : 1.25f;
        float finalScale = baseSize * sizeMultiplier;
        
        // Save Base Scale
        baseScale = new Vector3(finalScale, finalScale, 1f);
        transform.localScale = baseScale;

        // Stats Logic
        if (type == TowerType.Arrow)      
        { 
            float baseRange = 5.0f + ((currentLevel - 1) * 0.5f);
            float baseRate = (currentLevel == 3) ? 0.8f : 1.0f;
            currentStats = new LevelStats { range = baseRange, fireRate = baseRate, damage = 10f, projectileCount = currentLevel, visualColor = currentBaseColor };
        }
        else if (type == TowerType.Fire)  
        { 
            float cooldown = (currentLevel == 1) ? 10f : (currentLevel == 2) ? 7f : 4f;
            currentStats = new LevelStats { range = 3.5f, fireRate = 0.1f, cooldownTime = cooldown, damage = 3f, projectileCount = 1, maxAmmo = 30, visualColor = currentBaseColor };
        }
        else if (type == TowerType.Frost) 
        { 
            int count = (currentLevel == 1) ? 1 : (currentLevel == 2) ? 3 : 5;
            float rate = (currentLevel == 3) ? 0.4f : 0.5f;
            currentStats = new LevelStats { range = 3.5f + ((currentLevel - 1) * 0.25f), fireRate = rate, damage = 1f, projectileCount = count, visualColor = currentBaseColor };
        }

        if (!isBurstActive) SetColor(currentBaseColor);
        if(rangeLine) DrawCircle(rangeLine, currentStats.range);
    }

    void Update()
    {
        if (burstCooldownTimer > 0) burstCooldownTimer -= Time.deltaTime;
        if (type == TowerType.Fire) return;

        fireTimer += Time.deltaTime;
        float actualRate = currentStats.fireRate;
        if (isBurstActive)
        {
            if (type == TowerType.Arrow) actualRate = 0.1f; 
            if (type == TowerType.Frost) actualRate = currentStats.fireRate / 2f; 
        }

        if (fireTimer >= actualRate)
        {
            Enemy target = GetTarget();
            if (target != null) { Attack(target); fireTimer = 0; }
        }
    }

    // SMOOTH RECOIL LOGIC
    void TriggerRecoil()
    {
        if (recoilRoutine != null) StopCoroutine(recoilRoutine);
        recoilRoutine = StartCoroutine(SmoothRecoil());
    }

    IEnumerator SmoothRecoil()
    {
        Vector3 startSize = transform.localScale;
        Vector3 peakSize = new Vector3(baseScale.x, baseScale.y * 1.2f, baseScale.z); 

        float duration = 0.05f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startSize, peakSize, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = peakSize;

        duration = 0.15f;
        elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(peakSize, baseScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = baseScale;
    }

    IEnumerator FlamethrowerRoutine()
    {
        int currentAmmo = currentStats.maxAmmo;
        float pulseSpeed = 15f; 

        while (true)
        {
            if (!isBurstActive) SetColor(currentBaseColor);
            while (currentAmmo > 0 || isBurstActive)
            {
                Enemy target = GetTarget();
                if (target != null)
                {
                    FaceTarget(target.transform.position);
                    Vector3 spawnPos = transform.position; spawnPos.z = -2f;
                    float jitter = Random.Range(-10f, 10f);
                    Vector3 dir = (target.transform.position - transform.position).normalized;
                    Vector3 spreadDir = Quaternion.Euler(0, 0, jitter) * dir;

                    GameObject p = ObjectPool.Instance.Spawn(PoolType.Proj_Fire, spawnPos);
                    p.GetComponent<Projectile>().InitFire(spreadDir, currentStats.damage, 10f, currentStats.range);

                    // AUDIO INTEGRATION
                    AudioManager.Instance.PlayTowerShot(TowerType.Fire);

                    if (!isBurstActive) currentAmmo--;

                    float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; 
                    float stretchAmt = Mathf.Lerp(1.0f, 1.15f, wave);
                    
                    transform.localScale = new Vector3(baseScale.x, baseScale.y * stretchAmt, baseScale.z);

                    yield return new WaitForSeconds(currentStats.fireRate);
                }
                else 
                {
                    transform.localScale = Vector3.MoveTowards(transform.localScale, baseScale, Time.deltaTime * 2f);
                    yield return new WaitForSeconds(0.1f); 
                }
            }

            transform.localScale = baseScale;
            SetColor(Color.gray); 
            float cdTimer = 0;
            while (cdTimer < currentStats.cooldownTime)
            {
                if (isBurstActive) break; 
                yield return new WaitForSeconds(0.1f);
                cdTimer += 0.1f;
            }
            currentAmmo = currentStats.maxAmmo;
        }
    }

    Enemy GetTarget()
    {
        Enemy closest = null;
        float closestDist = currentStats.range;
        foreach (Enemy e in GameManager.Instance.activeEnemies)
        {
            if (!e.IsActive) continue;
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist <= closestDist) { closest = e; closestDist = dist; }
        }
        return closest;
    }

    void Attack(Enemy target)
    {
        TriggerRecoil();

        if (type == TowerType.Arrow) 
        {
            if (isBurstActive)
            {
                FaceTarget(target.transform.position);
                GameObject p = ObjectPool.Instance.Spawn(PoolType.Proj_Dart, transform.position + Vector3.back);
                p.GetComponent<Projectile>().InitArrow(target, currentStats.damage, 15f);
                
                // AUDIO INTEGRATION
                AudioManager.Instance.PlayTowerShot(TowerType.Arrow);
            }
            else StartCoroutine(DartBurstRoutine(target));
        }
        else if (type == TowerType.Frost) 
        { 
            FaceTarget(target.transform.position); 
            FireFrostShotgun(target); 
        }
    }

    IEnumerator DartBurstRoutine(Enemy target)
    {
        int shots = currentStats.projectileCount;
        for (int i = 0; i < shots; i++)
        {
            TriggerRecoil();

            if (target == null || !target.IsActive) break;
            FaceTarget(target.transform.position);
            Vector3 spawnPos = transform.position; spawnPos.z = -2f; 
            float offset = (i % 2 == 0) ? 0.15f : -0.15f; if (i == 0) offset = 0;
            Vector3 side = Vector3.Cross((target.transform.position - transform.position).normalized, Vector3.forward);
            
            GameObject p = ObjectPool.Instance.Spawn(PoolType.Proj_Dart, spawnPos + (side * offset));
            p.GetComponent<Projectile>().InitArrow(target, currentStats.damage, 15f);

            // AUDIO INTEGRATION
            AudioManager.Instance.PlayTowerShot(TowerType.Arrow);

            yield return new WaitForSeconds(0.15f);
        }
    }

    void FireFrostShotgun(Enemy target)
    {
        // AUDIO INTEGRATION
        AudioManager.Instance.PlayTowerShot(TowerType.Frost);

        Vector3 spawnPos = transform.position; spawnPos.z = -2f; 
        Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
        int count = currentStats.projectileCount;
        float angleStep = 10f; 
        float startAngle = -(angleStep * (count - 1)) / 2f;
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (i * angleStep);
            GameObject p = ObjectPool.Instance.Spawn(PoolType.Proj_Ice, spawnPos);
            Vector3 fanDir = Quaternion.Euler(0, 0, angle) * dirToTarget;
            p.GetComponent<Projectile>().InitIce(fanDir, currentStats.damage, 8f, currentStats.range);
        }
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void SetColor(Color c)
    {
        if (meshRenderer)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", c);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
    
    void DrawCircle(LineRenderer lr, float radius)
    {
        int segments = 50;
        lr.positionCount = segments;
        lr.useWorldSpace = false; 
        lr.loop = true;
        
        float adjustedRadius = radius / transform.localScale.x;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * 360f / segments);
            lr.SetPosition(i, new Vector3(Mathf.Sin(angle) * adjustedRadius, Mathf.Cos(angle) * adjustedRadius, 0));
        }
    }

    public void ToggleRangeVisual(bool status) { if(rangeLine) rangeLine.enabled = status; }
}