using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class BuildButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public TowerType towerType; 
    public bool isRemoveButton = false; 
    public float scaleSize = 1.15f;
    public float animSpeed = 10f;

    [Header("Wobble Settings")]
    public float wobbleAngle = 10f; 
    public float wobbleSpeed = 20f; 

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine currentCoroutine;
    private Button btn;

    void Start()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        
        // Get the button and disable Unity's built-in color changes
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        if (CanAfford())
        {
            // HAVE MONEY: Scale Up
            currentCoroutine = StartCoroutine(AnimateScale(originalScale * scaleSize));
        }
        else
        {
            // BROKE: Wobble "No"
            currentCoroutine = StartCoroutine(AnimateWobble());
        }

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.ShowCostTooltip(towerType, GetComponent<RectTransform>(), isRemoveButton);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        
        // Ensure button is re-enabled just in case
        if (btn != null) btn.enabled = true;

        currentCoroutine = StartCoroutine(ResetAnimation());

        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.HideCostTooltip();
        }
    }

    // BLOCK CLICK LOGIC
    public void OnPointerDown(PointerEventData eventData)
    {
        // If we can't afford it, we KILL the Button component momentarily.
        // This stops the "OnClick" event from firing in the Inspector.
        if (!CanAfford() && btn != null)
        {
            btn.enabled = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Re-enable the button so we can interact with it again later
        if (btn != null) btn.enabled = true;
    }

    // Helper

    bool CanAfford()
    {
        if (isRemoveButton) return true;
        
        // Check current global cost
        int cost = GameManager.Instance.GetNextTowerCost();
        return GameManager.Instance.coins >= cost;
    }

    // Animations

    IEnumerator AnimateScale(Vector3 target)
    {
        transform.localRotation = originalRotation; 
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * animSpeed);
            yield return null;
        }
        transform.localScale = target;
    }

    IEnumerator AnimateWobble()
    {
        transform.localScale = originalScale; 
        float time = 0f;
        while (true) 
        {
            time += Time.unscaledDeltaTime * wobbleSpeed;
            float z = Mathf.Sin(time) * wobbleAngle;
            transform.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }
    }

    IEnumerator ResetAnimation()
    {
        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f || Quaternion.Angle(transform.localRotation, originalRotation) > 0.1f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.unscaledDeltaTime * animSpeed);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.unscaledDeltaTime * animSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
    }
}