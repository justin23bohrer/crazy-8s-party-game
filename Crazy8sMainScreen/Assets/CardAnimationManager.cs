using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles all card animations including flip animations and 8-card spiral effects
/// Manages card state and visual transitions
/// </summary>
public class CardAnimationManager : MonoBehaviour
{
    [Header("Card References")]
    public Image topCardImage;
    public MonoBehaviour spiralAnimationController;
    
    [Header("Animation Settings")]
    public float cardFlipDuration = 0.8f;
    public float delayBeforeFlip = 2.0f;
    
    // Animation state tracking
    private bool isCardFlipAnimationInProgress = false;
    private bool isWaitingForEightCardAnimation = false;
    private string chosenEightCardColor = null;
    private string currentEightCardFinalColor = null;
    private string lastTopCardValue = null;
    private string lastTopCardColor = null;
    
    // Animation timeout safety
    private Coroutine animationTimeoutCoroutine;
    private float animationTimeoutDuration = 5f;
    
    // Events
    public event System.Action OnCardFlipComplete;
    public event System.Action OnSpiralAnimationComplete;
    
    public void Initialize()
    {
        Debug.Log("🎬 CardAnimationManager Initialize() called");
        
        // Auto-find components if not assigned
        if (topCardImage == null)
        {
            GameObject topCardObj = GameObject.Find("TopCard");
            if (topCardObj != null)
            {
                topCardImage = topCardObj.GetComponent<Image>();
                Debug.Log($"🎬 Found TopCard GameObject and Image component: {topCardImage != null}");
            }
            else
            {
                Debug.LogError("🎬 TopCard GameObject not found!");
            }
        }
        
        if (spiralAnimationController == null)
        {
            spiralAnimationController = FindFirstObjectByType<SpiralAnimationController>();
            Debug.Log($"🎬 Found SpiralAnimationController: {spiralAnimationController != null}");
        }
        
        Debug.Log($"🎬 CardAnimationManager initialized - topCardImage: {topCardImage != null}");
    }
    
    public void StartGameWithCardFlip()
    {
        Debug.Log("🎬 StartGameWithCardFlip() called");
        
        if (topCardImage == null) 
        {
            Debug.LogError("🎬 topCardImage is null - cannot start card flip!");
            return;
        }
        
        CardController cardController = topCardImage.GetComponent<CardController>();
        if (cardController == null) 
        {
            Debug.LogError("🎬 CardController not found on topCardImage - cannot start card flip!");
            return;
        }
        
        Debug.Log("🎬 Starting card flip animation...");
        
        // Set flag to prevent other updates during flip
        isCardFlipAnimationInProgress = true;
        
        // First show face-down card
        cardController.SetupFaceDownCard();
        
        // Wait then flip to reveal the starting card
        StartCoroutine(RevealStartingCard(cardController, delayBeforeFlip));
    }
    
    private IEnumerator RevealStartingCard(CardController cardController, float delay)
    {
        Debug.Log($"🎬 RevealStartingCard started - waiting {delay} seconds...");
        yield return new WaitForSeconds(delay);
        
        // Get the actual starting card from game state
        string startingCardColor = GetCurrentTopCardColor();
        int startingCardValue = GetCurrentTopCardValue();
        
        Debug.Log($"🎬 Retrieved starting card: {startingCardValue} of {startingCardColor}");
        
        // Defaults if no game state available
        if (string.IsNullOrEmpty(startingCardColor)) startingCardColor = "red";
        if (startingCardValue <= 0) startingCardValue = 7;
        
        Debug.Log($"🎬 Final starting card: {startingCardValue} of {startingCardColor}");
        
        // Trigger the flip animation
        Debug.Log("🎬 Starting flip animation...");
        cardController.FlipToRevealCard(startingCardColor, startingCardValue);
        
        // Wait for animation to complete
        Debug.Log($"🎬 Waiting {cardFlipDuration + 0.2f} seconds for flip to complete...");
        yield return new WaitForSeconds(cardFlipDuration + 0.2f);
        
        // Clear the flip animation flag
        isCardFlipAnimationInProgress = false;
        Debug.Log("🎬 Card flip animation completed!");
        OnCardFlipComplete?.Invoke();
    }
    
    public void UpdateTopCard(string cardValue)
    {
        UpdateTopCard(cardValue, false);
    }
    
    public void UpdateTopCard(string cardValue, bool isWinningEight = false)
    {
        Debug.Log($"🎬 UpdateTopCard called with: '{cardValue}' (flipInProgress: {isCardFlipAnimationInProgress}, winningEight: {isWinningEight})");
        
        // Skip updating during flip animation
        if (isCardFlipAnimationInProgress)
        {
            Debug.Log("🎬 Skipping UpdateTopCard - flip animation in progress");
            return;
        }

        if (topCardImage == null || string.IsNullOrEmpty(cardValue)) 
        {
            Debug.LogError($"🎬 Cannot update card - topCardImage: {topCardImage != null}, cardValue: '{cardValue}'");
            return;
        }

        CardController cardController = topCardImage.GetComponent<CardController>();
        if (cardController == null)
        {
            Debug.Log("🎬 Adding CardController component to topCardImage");
            cardController = topCardImage.gameObject.AddComponent<CardController>();
        }

        CardData cardData = ParseCardValue(cardValue);
        if (cardData == null) 
        {
            Debug.LogError($"🎬 Failed to parse card value: '{cardValue}'");
            return;
        }
        
        Debug.Log($"🎬 Parsed card: {cardData.color} {cardData.value}");
        
        // Track card changes
        bool topCardChanged = lastTopCardValue != cardData.value.ToString() || 
                             lastTopCardColor != cardData.color;
        
        if (topCardChanged)
        {
            currentEightCardFinalColor = null;
        }
        
        lastTopCardValue = cardData.value.ToString();
        lastTopCardColor = cardData.color;
        
        Debug.Log($"🎬 Setting card: {cardData.color} {cardData.value}");
        
        // Handle different card types
        if (cardData.value != 8)
        {
            ResetForNewCard();
            cardController.SetCard(cardData.color, cardData.value);
        }
        else
        {
            HandleEightCard(cardController, cardData, isWinningEight);
        }
    }
    
    private void HandleEightCard(CardController cardController, CardData cardData, bool isWinningEight = false)
    {
        if (isWinningEight)
        {
            // Winning 8 - just show as regular 8 card without animation
            Debug.Log("🏆 Winning 8 detected - showing as regular card without spiral animation");
            cardController.SetCard(cardData.color, 8);
            return;
        }
        
        if (currentEightCardFinalColor != null)
        {
            // 8 card already has final color
            cardController.SetCard(currentEightCardFinalColor, 8, true);
        }
        else if (chosenEightCardColor != null && !isWaitingForEightCardAnimation)
        {
            // Start spiral animation for color change
            cardController.SetCard(cardData.color, 8);
            StartCoroutine(DelayedEightCardAnimation(cardController, chosenEightCardColor));
        }
        else if (chosenEightCardColor == null)
        {
            // Just show regular 8 card
            cardController.SetCard(cardData.color, 8);
        }
        else
        {
            // Animation in progress, keep current state
            cardController.SetCard(cardData.color, 8);
        }
    }
    
    private IEnumerator DelayedEightCardAnimation(CardController cardController, string targetColor)
    {
        isWaitingForEightCardAnimation = true;
        
        yield return new WaitForSeconds(1f);
        
        if (spiralAnimationController != null)
        {
            // Trigger spiral animation
            var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
            if (method != null)
            {
                method.Invoke(spiralAnimationController, new object[] { cardController, targetColor });
                currentEightCardFinalColor = targetColor;
            }
        }
        
        // Start timeout safety
        if (animationTimeoutCoroutine != null)
        {
            StopCoroutine(animationTimeoutCoroutine);
        }
        animationTimeoutCoroutine = StartCoroutine(AnimationTimeoutHandler());
    }
    
    private IEnumerator AnimationTimeoutHandler()
    {
        yield return new WaitForSeconds(animationTimeoutDuration);
        
        if (isWaitingForEightCardAnimation)
        {
            ResetAnimationState();
        }
    }
    
    public void HandleColorChosen(string colorChosenJson)
    {
        // Extract color from JSON
        string color = ExtractJsonValue(colorChosenJson, "color");
        
        if (!string.IsNullOrEmpty(color))
        {
            chosenEightCardColor = color;
            
            // If we have an 8 card on top, start animation
            if (topCardImage != null)
            {
                CardController cardController = topCardImage.GetComponent<CardController>();
                if (cardController != null && lastTopCardValue == "8")
                {
                    StartCoroutine(DelayedEightCardAnimation(cardController, color));
                }
            }
        }
    }
    
    public void OnSpiralAnimationCompleted()
    {
        ResetAnimationState();
        OnSpiralAnimationComplete?.Invoke();
    }
    
    public void ResetAnimationState()
    {
        isWaitingForEightCardAnimation = false;
        chosenEightCardColor = null;
        
        if (animationTimeoutCoroutine != null)
        {
            StopCoroutine(animationTimeoutCoroutine);
            animationTimeoutCoroutine = null;
        }
    }
    
    public void ResetForNewCard()
    {
        ResetAnimationState();
        currentEightCardFinalColor = null;
    }
    
    public void ResetAnimations()
    {
        ResetAnimationState();
        isCardFlipAnimationInProgress = false;
        currentEightCardFinalColor = null;
        lastTopCardValue = null;
        lastTopCardColor = null;
    }
    
    // Helper methods
    private string GetCurrentTopCardColor()
    {
        if (!string.IsNullOrEmpty(lastTopCardColor))
        {
            return lastTopCardColor;
        }
        return "red"; // Default
    }
    
    private int GetCurrentTopCardValue()
    {
        if (!string.IsNullOrEmpty(lastTopCardValue) && int.TryParse(lastTopCardValue, out int value))
        {
            return value;
        }
        return 7; // Default
    }
    
    private CardData ParseCardValue(string cardString)
    {
        if (string.IsNullOrEmpty(cardString)) return null;
        
        // Parse format like "J of red", "8 of blue"
        string[] parts = cardString.Split(new string[] { " of " }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return null;
        
        string valueStr = parts[0].Trim();
        string color = parts[1].Trim().ToLower();
        
        // Validate color
        if (color != "red" && color != "blue" && color != "green" && color != "yellow")
        {
            return null;
        }
        
        // Parse value
        int value;
        switch (valueStr.ToUpper())
        {
            case "A": value = 1; break;
            case "J": value = 11; break;
            case "Q": value = 12; break;
            case "K": value = 13; break;
            default:
                if (!int.TryParse(valueStr, out value))
                {
                    return null;
                }
                break;
        }
        
        if (value < 1 || value > 13) return null;
        
        return new CardData(color, value);
    }
    
    private string ExtractJsonValue(string json, string key)
    {
        string searchKey = "\"" + key + "\":";
        int keyIndex = json.IndexOf(searchKey);
        
        if (keyIndex == -1) return "";
        
        int valueStart = keyIndex + searchKey.Length;
        
        // Skip whitespace
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
        {
            valueStart++;
        }
        
        if (valueStart >= json.Length) return "";
        
        // Handle string values
        if (json[valueStart] == '"')
        {
            valueStart++;
            int valueEnd = json.IndexOf('"', valueStart);
            if (valueEnd == -1) return "";
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
        
        return "";
    }
    
    // Public getters for other managers
    public bool IsFlipAnimationInProgress() => isCardFlipAnimationInProgress;
    public bool IsWaitingForEightCardAnimation() => isWaitingForEightCardAnimation;
    public string GetCurrentEightCardFinalColor() => currentEightCardFinalColor;
}
