using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required to find Buttons
using UnityEngine.EventSystems; // Required for Hover events
using System.Collections;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    [Header("UI Panels (Main Parents)")]
    public GameObject startPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject winPanel; 
    public GameObject retryPanel;

    [Header("Animation Containers (Content)")]
    [Tooltip("Assign the child object here (e.g., the Background Image). The Rotation/Scale animation applies to THIS object.")]
    public GameObject pauseContainer;
    public GameObject gameOverContainer;
    public GameObject winContainer; 
    public GameObject retryContainer;

    [Header("State Flags")]
    public bool isGameStarted = false;
    public bool isPaused = false;
    public bool isGameOver = false;
    public bool isGameWon = false; 
    public bool isRetryState = false;

    private bool isAnimating = false;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 0f; 

        isGameStarted = false;
        isPaused = false;
        isGameOver = false;
        isGameWon = false;
        isRetryState = false;
        isAnimating = false;

        // SETUP START PANEL (FORCE LEFT ANCHOR) ---
        if(startPanel) {
            startPanel.SetActive(true);
            RectTransform rect = startPanel.GetComponent<RectTransform>();
            
            // FORCE PIVOT TO LEFT-CENTER
            // This ensures scaling grows to the RIGHT, and position 0 is the LEFT edge.
            SetPivot(rect, new Vector2(0f, 0.5f));
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        // RESET CONTAINERS ---
        ResetContainer(pauseContainer);
        ResetContainer(gameOverContainer);
        ResetContainer(winContainer); // RESET WIN CONTAINER
        ResetContainer(retryContainer);

        if(pausePanel) pausePanel.SetActive(false);
        if(gameOverPanel) gameOverPanel.SetActive(false);
        if(winPanel) winPanel.SetActive(false); // HIDE WIN PANEL
        if(retryPanel) retryPanel.SetActive(false);

        // Find EVERY button in the scene (even inactive ones) and force the animation script on them.
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in allButtons)
        {
            // Check if the button is actually in the scene (and not a prefab in your project folder)
            if (btn.gameObject.scene.rootCount != 0) 
            {
                if (btn.gameObject.GetComponent<SimpleButtonAnim>() == null)
                {
                    btn.gameObject.AddComponent<SimpleButtonAnim>();
                }
            }
        }
    }

    private void ResetContainer(GameObject container)
    {
        if (container != null)
        {
            container.transform.localScale = Vector3.one;
            container.transform.localRotation = Quaternion.identity;
            container.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    // Helper to set pivot without jumping
    void SetPivot(RectTransform rect, Vector2 pivot)
    {
        if (rect == null) return;
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = rect.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
        rect.pivot = pivot;
        rect.localPosition -= deltaPosition;
    }

    void Update()
    {
        if (isAnimating) return; 

        // START MENU
        if (!isGameStarted)
        {
            // Force Y position to 0 to prevent drifting
            if (startPanel != null)
            {
                RectTransform rt = startPanel.GetComponent<RectTransform>();
                if (rt.anchoredPosition.y != 0)
                {
                    Vector2 fixedPos = rt.anchoredPosition;
                    fixedPos.y = 0;
                    rt.anchoredPosition = fixedPos;
                }
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(PageTurnAnimation());
            }
            return;
        }

        // GAMEOVER OR WIN -> RETRY
        // If Game is Over OR Won, allow clicking to go to Retry
        if ((isGameOver || isGameWon) && !isRetryState)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                SwitchToRetryMenu();
            }
            return;
        }

        // RETRY STATE
        if (isRetryState) return;

        // PAUSE (ESC)
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver && !isGameWon)
        {
            TogglePause();
        }
    }

    // ANIMATIONS
    // ANIMATION 1: LOCKED LEFT ANCHOR START
    IEnumerator PageTurnAnimation()
    {
        isAnimating = true;
        RectTransform rect = startPanel.GetComponent<RectTransform>();

        // Double check pivot is Left-Center (0, 0.5)
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero; 

        float duration = 0.65f; 
        float t = 0;
        
        // Move Target: Width of screen + extra
        float moveTarget = -Screen.width * 2.5f; 

        while (t < 1)
        {
            t += Time.unscaledDeltaTime / duration;

            float currentScaleX = 1f;
            float currentPosX = 0f;

            // Phase 1 (0% to 30%): Wind Up. STRETCH RIGHT ONLY.
            if (t < 0.3f)
            {
                float phase = t / 0.3f;
                phase = phase * (2 - phase); // Ease Out Quad

                // Scale X grows. Since pivot is LEFT, it grows to the Right.
                currentScaleX = Mathf.Lerp(1.0f, 1.4f, phase);
                currentPosX = 0f; 
            }
            // Phase 2 (30% to 100%): EXIT LEFT.
            else
            {
                float phase = (t - 0.3f) / 0.7f;
                float curve = phase * phase * phase; // Ease In Cubic

                // Continue stretching
                currentScaleX = Mathf.Lerp(1.4f, 2.0f, phase);
                
                // Move Position Left (Negative)
                currentPosX = Mathf.LerpUnclamped(0f, moveTarget, curve);
            }

            rect.localScale = new Vector3(currentScaleX, 1f, 1f);
            rect.anchoredPosition = new Vector2(currentPosX, 0f);

            yield return null;
        }

        startPanel.SetActive(false);
        isGameStarted = true;
        Time.timeScale = 1f; 
        isAnimating = false;
    }

    // ANIMATION 2: STICKY NOTE POP-IN
    IEnumerator StickyNoteEnter(GameObject mainPanel, GameObject animationTarget)
    {
        mainPanel.SetActive(true);
        GameObject targetObj = animationTarget != null ? animationTarget : mainPanel;
        
        RectTransform rect = targetObj.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        Vector3 startScale = Vector3.one * 0.3f;
        float randomTilt = Random.Range(-10f, 10f);
        
        rect.localScale = startScale;
        rect.localRotation = Quaternion.Euler(0, 0, randomTilt);

        float duration = 0.35f;
        float t = 0;

        while(t < 1)
        {
            t += Time.unscaledDeltaTime / duration;
            // Elastic Out
            float c4 = (2 * Mathf.PI) / 3;
            float curve = (t == 0) ? 0 : (t == 1) ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;

            rect.localScale = Vector3.Lerp(startScale, Vector3.one, curve);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    // LOGIC
    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused) {
            StartCoroutine(StickyNoteEnter(pausePanel, pauseContainer));
            Time.timeScale = 0f; 
        } else {
            pausePanel.SetActive(false);
            Time.timeScale = 1f; 
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver || isGameWon) return;
        isGameOver = true;
        Time.timeScale = 0f; 
        
        if(pausePanel) pausePanel.SetActive(false);
        if(startPanel) startPanel.SetActive(false);
        StartCoroutine(StickyNoteEnter(gameOverPanel, gameOverContainer));
    }

    public void TriggerWin()
    {
        if (isGameOver || isGameWon) return;
        isGameWon = true;
        Time.timeScale = 0f;

        if (pausePanel) pausePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);
        StartCoroutine(StickyNoteEnter(winPanel, winContainer));
    }

    public void SwitchToRetryMenu()
    {
        isRetryState = true;
        if(gameOverPanel) gameOverPanel.SetActive(false);
        if(winPanel) winPanel.SetActive(false); // Hide Win Panel too
        StartCoroutine(StickyNoteEnter(retryPanel, retryContainer));
    }

    // BUTTON LINKS
    public void OnResumePressed() { TogglePause(); }
    public void OnPauseButtonPressed() { TogglePause(); }
    public void OnRetryPressed() 
    { 
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnQuitPressed() { QuitGame(); }

    public void QuitGame()
    {
        // FIX: Fully qualified check prevents CS0103 error
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

//   HELPER CLASS: AUTOMATIC BUTTON ANIMATION
//   (Attached automatically by MenuController)
public class SimpleButtonAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    private Coroutine currentCoroutine;
    
    // SETTINGS
    private float hoverScale = 1.15f;
    private float clickScale = 0.95f;
    private float animSpeed = 15f; 

    void Awake()
    {
        originalScale = transform.localScale;
        if (originalScale.x == 0) originalScale = Vector3.one;
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale * hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale * clickScale));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Vector3 target = eventData.hovered.Contains(gameObject) ? originalScale * hoverScale : originalScale;
        StopAllCoroutines();
        StartCoroutine(AnimateScale(target));
    }

    IEnumerator AnimateScale(Vector3 target)
    {
        // Use unscaledDeltaTime so it works in Pause Menu
        while (Vector3.Distance(transform.localScale, target) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * animSpeed);
            yield return null;
        }
        transform.localScale = target;
    }
}