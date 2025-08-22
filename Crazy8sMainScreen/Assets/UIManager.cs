using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages all UI updates including text displays, background colors, and visual effects
/// Handles the visual representation of game state changes
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Game UI")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI currentColorText;
    public TextMeshProUGUI deckCountText;
    public TextMeshProUGUI winnerText;
    
    [Header("Background Effects")]
    public Image colorChangerBackground;
    private Color originalBackgroundColor;
    
    [Header("Card Colors")]
    public Color redColor = new Color(0.6f, 0.08f, 0.08f, 100f/255f);
    public Color blueColor = new Color(0.08f, 0.08f, 0.6f, 100f/255f);
    public Color greenColor = new Color(0.08f, 0.4f, 0.08f, 100f/255f);
    public Color yellowColor = new Color(0.6f, 0.6f, 0.08f, 100f/255f);
    
    [Header("Transition Settings")]
    public float colorTransitionDuration = 1.5f;
    
    private Coroutine currentColorTransition = null;
    
    public void Initialize()
    {
        // Auto-find components if not assigned
        if (colorChangerBackground == null)
        {
            GameObject bgObject = GameObject.Find("ColorChangerBackground");
            if (bgObject != null)
            {
                colorChangerBackground = bgObject.GetComponent<Image>();
            }
        }
        
        // Store original background color
        if (colorChangerBackground != null)
        {
            originalBackgroundColor = colorChangerBackground.color;
        }
        
        // Initialize UI text
        ResetUI();
    }
    
    public void UpdateGameUI(string currentPlayer, string currentColor, int deckCount)
    {
        if (currentPlayerText != null && !string.IsNullOrEmpty(currentPlayer))
        {
            currentPlayerText.text = "Current Player: " + currentPlayer;
        }
        
        if (currentColorText != null && !string.IsNullOrEmpty(currentColor))
        {
            currentColorText.text = "Current Color: " + currentColor;
        }
        
        if (deckCountText != null)
        {
            deckCountText.text = "Cards Left: " + deckCount;
        }
    }
    
    public void UpdateCurrentPlayer(string playerName)
    {
        if (currentPlayerText != null && !string.IsNullOrEmpty(playerName))
        {
            currentPlayerText.text = "Current Player: " + playerName;
        }
    }
    
    public void UpdateCurrentColor(string color)
    {
        if (currentColorText != null && !string.IsNullOrEmpty(color))
        {
            currentColorText.text = "Current Color: " + color;
        }
    }
    
    public void UpdateDeckCount(int count)
    {
        if (deckCountText != null)
        {
            deckCountText.text = "Cards Left: " + count;
        }
    }
    
    public void UpdateWinnerText(string winner)
    {
        if (winnerText != null && !string.IsNullOrEmpty(winner))
        {
            winnerText.text = winner + " Wins!";
        }
    }
    
    public void ChangeBackgroundToCardColor(string cardColor)
    {
        if (colorChangerBackground == null) return;
        
        Color targetColor = GetBackgroundColor(cardColor);
        
        // Stop current transition
        if (currentColorTransition != null)
        {
            StopCoroutine(currentColorTransition);
        }
        
        // Start new smooth transition
        currentColorTransition = StartCoroutine(GradualColorTransition(targetColor));
    }
    
    private IEnumerator GradualColorTransition(Color targetColor)
    {
        if (colorChangerBackground == null) yield break;
        
        Color startColor = colorChangerBackground.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < colorTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / colorTransitionDuration;
            
            // Smooth easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            colorChangerBackground.color = Color.Lerp(startColor, targetColor, easedProgress);
            
            yield return null;
        }
        
        // Ensure final color is exact
        colorChangerBackground.color = targetColor;
        currentColorTransition = null;
    }
    
    public void ResetBackgroundColor()
    {
        if (colorChangerBackground != null)
        {
            // Stop current transition
            if (currentColorTransition != null)
            {
                StopCoroutine(currentColorTransition);
                currentColorTransition = null;
            }
            
            colorChangerBackground.color = originalBackgroundColor;
        }
    }
    
    public void ResetUI()
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = "Waiting for players...";
        }
        
        if (currentColorText != null)
        {
            currentColorText.text = "Current Color: --";
        }
        
        if (deckCountText != null)
        {
            deckCountText.text = "Cards Left: --";
        }
        
        if (winnerText != null)
        {
            winnerText.text = "";
        }
        
        ResetBackgroundColor();
    }
    
    public Color GetBackgroundColor(string color)
    {
        switch (color.ToLower())
        {
            case "red":
                return redColor;
            case "blue": 
                return blueColor;
            case "green":
                return greenColor;
            case "yellow":
                return yellowColor;
            default: 
                return originalBackgroundColor;
        }
    }
    
    // Smooth color transition for winner animations
    public void TransitionToWinnerColor(string winnerColor, float duration)
    {
        if (colorChangerBackground == null) return;
        
        Color targetColor = GetBackgroundColor(winnerColor);
        
        // Stop current transition
        if (currentColorTransition != null)
        {
            StopCoroutine(currentColorTransition);
        }
        
        // Start winner color transition
        currentColorTransition = StartCoroutine(WinnerColorTransition(targetColor, duration));
    }
    
    private IEnumerator WinnerColorTransition(Color targetColor, float duration)
    {
        if (colorChangerBackground == null) yield break;
        
        Color startColor = colorChangerBackground.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Cinematic easing for winner sequence
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out cubic
            
            colorChangerBackground.color = Color.Lerp(startColor, targetColor, easedProgress);
            
            yield return null;
        }
        
        colorChangerBackground.color = targetColor;
        currentColorTransition = null;
    }
    
    // Public methods for other managers
    public bool IsColorTransitionInProgress()
    {
        return currentColorTransition != null;
    }
    
    public void StopCurrentColorTransition()
    {
        if (currentColorTransition != null)
        {
            StopCoroutine(currentColorTransition);
            currentColorTransition = null;
        }
    }
}
