using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all winner animation sequences including background transitions,
/// player movement, text animations, and fade effects
/// </summary>
public class WinnerAnimationManager : MonoBehaviour
{
    [Header("Winner UI Elements")]
    public TextMeshProUGUI bigWinnerText;
    public TextMeshProUGUI instructionText;
    
    [Header("Animation Settings")]
    public float winnerAnimationDuration = 8f;
    public float fadeDuration = 2f;
    public float movementDuration = 2.5f;
    public float backgroundTransitionDuration = 3.5f;
    
    private UIManager uiManager;
    private PlayerManager playerManager;
    private bool isAnimating = false;
    
    // Events
    public event System.Action OnWinnerAnimationComplete;
    
    public void Initialize()
    {
        // Auto-find required managers
        uiManager = FindFirstObjectByType<UIManager>();
        playerManager = FindFirstObjectByType<PlayerManager>();
        
        // Initialize winner UI elements (start hidden)
        if (bigWinnerText != null)
        {
            bigWinnerText.gameObject.SetActive(false);
        }
        
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
    }
    
    public void TriggerWinnerSequence(string winnerName)
    {
        if (isAnimating || string.IsNullOrEmpty(winnerName))
        {
            return;
        }
        
        string winnerColor = GetPlayerColor(winnerName);
        if (string.IsNullOrEmpty(winnerColor))
        {
            winnerColor = "blue"; // Default color
        }
        
        StartCoroutine(OrchestratedWinnerSequence(winnerName, winnerColor));
    }
    
    private IEnumerator OrchestratedWinnerSequence(string winnerName, string winnerColor)
    {
        isAnimating = true;
        
        // Step 1: Start background transition immediately
        if (uiManager != null)
        {
            uiManager.TransitionToWinnerColor(winnerColor, backgroundTransitionDuration);
        }
        yield return new WaitForSeconds(0.3f);
        
        // Step 2: Fade out non-winner elements
        yield return StartCoroutine(FadeOutNonWinnerElements(winnerName));
        yield return new WaitForSeconds(1.0f);
        
        // Step 3: Move winner to center
        yield return StartCoroutine(MoveWinnerToCenter(winnerName));
        yield return new WaitForSeconds(1.0f);
        
        // Step 4: Show winner text with animation
        yield return StartCoroutine(AnimateWinnerText());
        yield return new WaitForSeconds(1.5f);
        
        // Step 5: Show instruction text
        yield return StartCoroutine(AnimateInstructionText());
        yield return new WaitForSeconds(2.0f);
        
        isAnimating = false;
        OnWinnerAnimationComplete?.Invoke();
    }
    
    private IEnumerator FadeOutNonWinnerElements(string winnerName)
    {
        if (playerManager == null) yield break;
        
        List<GameObject> playerDisplays = playerManager.GetAllPlayerDisplays();
        GameObject playArea = GameObject.Find("PlayArea");
        
        List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
        
        // Add canvas groups for non-winner players
        foreach (GameObject playerDisplay in playerDisplays)
        {
            if (playerDisplay != null)
            {
                string playerNameInDisplay = playerManager.GetPlayerNameFromDisplay(playerDisplay);
                if (playerNameInDisplay != winnerName)
                {
                    CanvasGroup cg = playerDisplay.GetComponent<CanvasGroup>();
                    if (cg == null)
                    {
                        cg = playerDisplay.AddComponent<CanvasGroup>();
                    }
                    canvasGroups.Add(cg);
                }
            }
        }
        
        // Add play area (card stack) canvas group
        if (playArea != null)
        {
            CanvasGroup playAreaCG = playArea.GetComponent<CanvasGroup>();
            if (playAreaCG == null)
            {
                playAreaCG = playArea.AddComponent<CanvasGroup>();
            }
            canvasGroups.Add(playAreaCG);
        }
        
        // Fade out all non-winner elements
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            
            foreach (CanvasGroup cg in canvasGroups)
            {
                if (cg != null)
                {
                    cg.alpha = alpha;
                }
            }
            
            yield return null;
        }
    }
    
    private IEnumerator MoveWinnerToCenter(string winnerName)
    {
        if (playerManager == null) yield break;
        
        GameObject winnerDisplay = playerManager.FindPlayerDisplay(winnerName);
        if (winnerDisplay == null) yield break;
        
        RectTransform rectTransform = winnerDisplay.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 targetPosition = new Vector2(841f, -489f); // Correct center position
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = startScale * 1.2f; // Scale up for prominence
        
        float elapsedTime = 0f;
        
        while (elapsedTime < movementDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / movementDuration;
            
            // Smooth easing
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out cubic
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            
            yield return null;
        }
        
        // Ensure final position and scale
        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
    }
    
    private IEnumerator AnimateWinnerText()
    {
        if (bigWinnerText == null) yield break;
        
        bigWinnerText.gameObject.SetActive(true);
        bigWinnerText.text = "WINNER!";
        
        RectTransform textRect = bigWinnerText.GetComponent<RectTransform>();
        if (textRect == null) yield break;
        
        // Start at scale 0
        textRect.localScale = Vector3.zero;
        
        float duration = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Bouncy scale animation
            float scale;
            if (progress < 0.6f)
            {
                // Overshoot
                scale = Mathf.Lerp(0f, 1.2f, progress / 0.6f);
            }
            else
            {
                // Settle back
                scale = Mathf.Lerp(1.2f, 1f, (progress - 0.6f) / 0.4f);
            }
            
            textRect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        textRect.localScale = Vector3.one;
    }
    
    private IEnumerator AnimateInstructionText()
    {
        if (instructionText == null) yield break;
        
        instructionText.gameObject.SetActive(true);
        instructionText.text = "Look at your phone to play same room or create new room";
        
        CanvasGroup canvasGroup = instructionText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = instructionText.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start transparent
        canvasGroup.alpha = 0f;
        
        float duration = 1.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    public void ResetAnimations()
    {
        isAnimating = false;
        
        if (bigWinnerText != null)
        {
            bigWinnerText.gameObject.SetActive(false);
            bigWinnerText.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            CanvasGroup cg = instructionText.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
            }
        }
    }
    
    private string GetPlayerColor(string playerName)
    {
        if (playerManager != null)
        {
            return playerManager.GetPlayerColor(playerName);
        }
        return "blue"; // Default
    }
    
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    /// <summary>
    /// Reset all winner animation elements to their initial state
    /// Called when restarting the game
    /// </summary>
    public void ResetWinnerAnimation()
    {
        Debug.Log("🔄 Resetting winner animation elements");
        
        // Stop any running animation
        isAnimating = false;
        StopAllCoroutines();
        
        // Hide and reset winner text
        if (bigWinnerText != null)
        {
            bigWinnerText.gameObject.SetActive(false);
            bigWinnerText.transform.localScale = Vector3.zero;
            Debug.Log("✅ Reset bigWinnerText");
        }
        
        // Hide and reset instruction text
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            
            // Reset alpha if it has a CanvasGroup
            CanvasGroup canvasGroup = instructionText.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            Debug.Log("✅ Reset instructionText");
        }
        
        // Reset play area (card stack) visibility - it gets faded out during winner animation
        GameObject playArea = GameObject.Find("PlayArea");
        if (playArea != null)
        {
            CanvasGroup playAreaCG = playArea.GetComponent<CanvasGroup>();
            if (playAreaCG != null)
            {
                playAreaCG.alpha = 1f;
                Debug.Log("✅ Reset PlayArea (card stack) visibility");
            }
        }
        
        // IMPORTANT: Force clear player displays to ensure they get recreated in proper positions
        if (playerManager != null)
        {
            Debug.Log("🔄 Force clearing player displays for restart");
            // Clear both PlayerManager and PlayerPositionManager data
            playerManager.ResetPlayers();
            
            // Also force clear the position manager's tracking to ensure recreation
            var playerPositionManager = FindFirstObjectByType<PlayerPositionManager>();
            if (playerPositionManager != null)
            {
                playerPositionManager.ClearPlayerDisplays();
                Debug.Log("✅ Force cleared PlayerPositionManager displays");
            }
        }
        
        // Reset background color through UIManager
        // if (uiManager != null)
        // {
        //     uiManager.ResetToOriginalBackground();
        //     Debug.Log("✅ Reset background color");
        // }
        
        Debug.Log("🎮 Winner animation reset complete");
    }
}
