using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Enemy target;
    private Vector3 moveDir;
    private float speed;
    private float damage;
    private float maxRange;
    private Vector3 startPos;
    
    // Special Effects
    private float slowPct;
    private float slowDur;
    
    private bool isHoming;
    private Color hitColor;

    // SETUP FUNCTIONS 

    public void InitArrow(Enemy t, float dmg, float spd)
    {
        target = t;
        damage = dmg;
        speed = spd;
        isHoming = true;
        slowPct = 0; 
        hitColor = Color.yellow;
        if(t != null) FaceDirection(t.transform.position - transform.position);
    }

    public void InitFire(Vector3 dir, float dmg, float spd, float range)
    {
        SetupDirectional(dir, dmg, spd, range);
        hitColor = Color.red; 
        slowPct = 0; 
    }

    public void InitIce(Vector3 dir, float dmg, float spd, float range)
    {
        SetupDirectional(dir, dmg, spd, range);
        hitColor = Color.cyan;
        slowPct = 0.5f; 
        slowDur = 2.0f;
    }

    void SetupDirectional(Vector3 dir, float dmg, float spd, float range)
    {
        // Flatten direction to 2D so projectiles don't fly into the background
        dir.z = 0; 
        moveDir = dir.normalized;
        
        damage = dmg;
        speed = spd;
        maxRange = range;
        startPos = transform.position;
        isHoming = false;
        FaceDirection(moveDir);
    }

    void FaceDirection(Vector3 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    // MOVEMENT LOOP
    void Update()
    {
        float moveDist = speed * Time.deltaTime;

        if (isHoming)
        {
            if (target == null || !target.IsActive)
            {
                gameObject.SetActive(false); 
                return;
            }

            // Homing moves in 3D, so it naturally closes the Z-gap
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveDist);
            FaceDirection(target.transform.position - transform.position);
            
            if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
            {
                HitEnemy(target);
            }
        }
        else
        {
            // Linear Movement (Fire/Ice)
            transform.position += moveDir * moveDist;
            
            // Range Check
            if (Vector3.Distance(startPos, transform.position) > maxRange)
            {
                gameObject.SetActive(false);
                return;
            }

            // Math Collision Check
            CheckCollision();
        }
    }

    void CheckCollision()
    {
        // Loop through all enemies
        foreach (Enemy e in GameManager.Instance.activeEnemies)
        {
            if (e == null || !e.IsActive) continue;

            // Calculate distance ONLY on X and Y. Ignore Z.
            float distX = transform.position.x - e.transform.position.x;
            float distY = transform.position.y - e.transform.position.y;
            
            // Pythagoras: a^2 + b^2 = c^2
            // 0.25f is the squared radius (approx 0.5 units wide hit area)
            if ((distX * distX) + (distY * distY) < 0.25f) 
            {
                HitEnemy(e);
                return; // Die after hitting one enemy
            }
        }
    }

    void HitEnemy(Enemy e)
    {
        e.TakeDamage(damage, hitColor);
        if (slowPct > 0) e.ApplySlow(slowPct, slowDur);
        
        // --- AUDIO CONNECTION ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHit();
        }

        gameObject.SetActive(false); // Return to pool
    }
}