using UnityEngine;

public class Plot : MonoBehaviour
{
    [Header("Math Settings")]
    public float plotSize = 1.2f; 

    [Header("State")]
    public Tower occupiedTower;
    
    public bool IsPointInside(Vector2 point)
    {
        Vector2 myPos = transform.position;
        if (point.x < myPos.x - plotSize/2 || point.x > myPos.x + plotSize/2) return false;
        if (point.y < myPos.y - plotSize/2 || point.y > myPos.y + plotSize/2) return false;
        return true;
    }

    public bool IsOccupied() => occupiedTower != null;

    // Accept Cost
    public void BuildTower(Tower towerPrefab, int costPaid)
    {
        if (IsOccupied()) return;

        Vector3 pos = transform.position;
        pos.z = -0.1f; 

        GameObject obj = Instantiate(towerPrefab.gameObject, pos, Quaternion.identity);
        occupiedTower = obj.GetComponent<Tower>();
        
        // Save the cost so the tower knows how much to charge for upgrades later
        occupiedTower.purchaseCost = costPaid;
    }

    public void RemoveTower()
    {
        if (occupiedTower != null)
        {
            Destroy(occupiedTower.gameObject);
            occupiedTower = null;
        }
    }
}