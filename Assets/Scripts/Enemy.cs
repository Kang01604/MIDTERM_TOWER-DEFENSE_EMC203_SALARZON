using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseSpeed = 2f;
    public float baseHP = 10f;
    public bool isImmuneToSlow = false;

    [Header("Animation")]
    public float wobbleAngle = 15f;
    public float wobbleSpeed = 10f;

    [Header("Live Stats")]
    public float speed;
    public float maxHP;
    public float currentHP;
    public int originWave;

    // UI REFERENCES
    [Header("UI")]
    public RectTransform hpBarRect;      // The Green Bar
    public RectTransform ghostHpBarRect; // The White Bar
    public Transform hpCanvasTransform;  // The Canvas (Drag the root Canvas here)

    // Visuals
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Color defaultColor = Color.white;
    private float animOffset;

    // Pathing
    private Transform[] pathPoints;
    private int curveIndex = 0;
    private float tParam = 0f;

    public bool IsActive => gameObject.activeInHierarchy;

    // Status Effects
    private float speedModifier = 1f;
    private Coroutine slowCoroutine;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        animOffset = Random.Range(0f, 100f);
    }

    public void Initialize(int wave, Transform[] path)
    {
        originWave = wave;
        pathPoints = path;
        curveIndex = 0;
        tParam = 0f;

        // Stats
        float statMultiplier = (wave >= 10) ? 3f : (wave >= 5) ? 2f : 1f;
        maxHP = baseHP * statMultiplier;
        currentHP = maxHP;
        
        speed = baseSpeed;
        speedModifier = 1f;
        SetColor(defaultColor);

        if (pathPoints != null && pathPoints.Length > 0)
            transform.position = pathPoints[0].position;

        // Reset Bars
        if (hpBarRect) hpBarRect.localScale = Vector3.one;
        if (ghostHpBarRect) ghostHpBarRect.localScale = Vector3.one;

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!IsActive) return;

        // Move & Animate
        MoveAlongBezier();
        
        // Wobble Effect (Rotates the Enemy)
        float zRotation = Mathf.Sin((Time.time + animOffset) * wobbleSpeed) * wobbleAngle;
        transform.rotation = Quaternion.Euler(0, 0, zRotation);

        // Ghost Bar Logic (Smooth Slide)
        if (ghostHpBarRect && hpBarRect)
        {
            float targetX = hpBarRect.localScale.x;
            float currentX = ghostHpBarRect.localScale.x;

            if (currentX > targetX)
            {
                float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * 2.0f);
                ghostHpBarRect.localScale = new Vector3(newX, 1f, 1f);
            }
        }
    }

    void LateUpdate()
    {
        if (hpCanvasTransform)
        {
            hpCanvasTransform.rotation = Quaternion.identity; 
        }
    }

    void MoveAlongBezier()
    {
        if (pathPoints == null || curveIndex + 2 >= pathPoints.Length) return;

        Vector3 p0 = pathPoints[curveIndex].position;
        Vector3 p1 = pathPoints[curveIndex + 1].position;
        Vector3 p2 = pathPoints[curveIndex + 2].position;

        float roughDist = Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2);
        float moveStep = (speed * speedModifier * Time.deltaTime) / roughDist;
        tParam += moveStep;

        transform.position = GameMath.GetBezierPoint(p0, p1, p2, tParam);

        if (tParam >= 1f)
        {
            tParam = 0f;
            curveIndex += 2;
            if (curveIndex + 2 >= pathPoints.Length)
            {
                GameManager.Instance.OnEnemyReachedEnd(this);
            }
        }
    }

    public void TakeDamage(float amount, Color hitColor)
    {
        // ADD THIS LINE: Play the shared enemy hurt sound
        AudioManager.Instance.PlayEnemyHit();

        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        // Update Scale
        if (hpBarRect)
        {
            float hpPct = currentHP / maxHP;
            hpBarRect.localScale = new Vector3(hpPct, 1f, 1f);
        }

        StartCoroutine(FlashColor(hitColor));
        if (currentHP <= 0) GameManager.Instance.OnEnemyKilled(this);
    }

    public int GetGoldReward() { return (originWave * 5) + 5; }

    public void ApplySlow(float pct, float duration)
    {
        if (!gameObject.activeInHierarchy || isImmuneToSlow) return;
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);
        slowCoroutine = StartCoroutine(SlowRoutine(pct, duration));
    }

    IEnumerator SlowRoutine(float pct, float duration)
    {
        speedModifier = pct;
        SetColor(Color.cyan);
        yield return new WaitForSeconds(duration);
        speedModifier = 1f;
        SetColor(defaultColor);
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

    IEnumerator FlashColor(Color c)
    {
        SetColor(c);
        yield return new WaitForSeconds(0.1f);
        if (speedModifier < 1f) SetColor(Color.cyan);
        else SetColor(defaultColor);
    }
}