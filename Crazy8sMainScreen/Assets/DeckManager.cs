using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Simple deck manager that flips a card from deck to TopCard area on game start
/// Uses NEW Input System (UnityEngine.InputSystem) - NO legacy Input class
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("Card References")]
    public Image deckImage;         // The deck (shows card back)
    public Image topCardImage;      // The main card area (target) - DEPRECATED: Using stacking system
    public GameObject cardPrefab;   // Your card prefab with front/back
    public Transform playArea;      // NEW: Empty GameObject where cards land and stack
    
    [Header("Animation Settings")]
    public float flipDuration = 1.5f;
    public float moveDistance = 200f; // How far to move right before flipping
    public Vector2 deckSpawnPosition = new Vector2(-255f, -268f); // Exact spawn position for cards
    public Vector2 playAreaPosition = new Vector2(0f, 0f); // NEW: Where cards land in stack
    
    [Header("Card Stacking")]
    public float stackOffset = 5f;     // NEW: How much each card offsets from the previous
    public float stackRotation = 10f;  // NEW: Random rotation range for natural look
    public int maxVisibleCards = 10;   // NEW: Max cards to keep visible in stack
    
    [Header("Sprites")]
    public Sprite cardBackSprite;
    
    [Header("Testing")]
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private string testCardColor = "red";
    [SerializeField] private int testCardValue = 7;
    
    // Animation state
    private bool isAnimating = false;
    private bool isInTestMode = false;
    
    // NEW: Card stack management
    private System.Collections.Generic.List<GameObject> playedCards = new System.Collections.Generic.List<GameObject>();
    private int totalCardsPlayed = 0;
    
    // Events
    public event System.Action OnFlipComplete;
    
    public void Initialize()
    {
        Debug.Log("🃏 DeckManager Initialize()");
        
        // Find components if not assigned
        if (deckImage == null)
        {
            GameObject deckObj = GameObject.Find("DeckStack");
            if (deckObj != null)
                deckImage = deckObj.GetComponent<Image>();
        }
        
        if (topCardImage == null)
        {
            GameObject topCardObj = GameObject.Find("TopCard");
            if (topCardObj != null)
                topCardImage = topCardObj.GetComponent<Image>();
        }
        
        SetupInitialState();
        
        // Test on start if enabled
        if (testOnStart)
        {
            StartCoroutine(TestAfterDelay());
        }
    }
    
    private IEnumerator TestAfterDelay()
    {
        yield return new WaitForSeconds(1f); // Wait 1 second then test
        TestFlipAnimation();
    }
    
    void Update()
    {
        // Keyboard shortcuts for testing using new Input System
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TestFlipAnimation();
        }
        
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetCardDisplay();
        }
    }
    
    private void SetupInitialState()
    {
        // Deck shows card back
        if (deckImage != null && cardBackSprite != null)
        {
            deckImage.sprite = cardBackSprite;
            deckImage.color = Color.white;
            Debug.Log("🃏 Deck setup with card back");
        }
        
        // TopCard starts hidden
        if (topCardImage != null)
        {
            topCardImage.color = new Color(1f, 1f, 1f, 0f); // Transparent
            Debug.Log("🃏 TopCard hidden - ready for flip");
        }
    }
    
    public void StartGameFlip()
    {
        if (isAnimating)
        {
            Debug.Log("🃏 Already animating, skipping");
            return;
        }
        
        if (deckImage == null || topCardImage == null)
        {
            Debug.LogError("🃏 Missing deck or topCard image!");
            return;
        }
        
        Debug.Log("🃏 Starting simple deck flip animation");
        isAnimating = true;
        StartCoroutine(FlipCardToTopCard());
    }
    
    /// <summary>
    /// NEW: Play a card from deck to the stacking play area
    /// </summary>
    public void PlayCardFromDeck(string cardColor = "", int cardValue = 0)
    {
        if (isAnimating)
        {
            Debug.Log("🃏 Already animating, queuing card...");
            // Could implement a queue system here later
            return;
        }
        
        // Use provided values or fall back to test values
        string finalColor = !string.IsNullOrEmpty(cardColor) ? cardColor : testCardColor;
        int finalValue = cardValue > 0 ? cardValue : testCardValue;
        
        Debug.Log($"🃏 PlayCardFromDeck called: {finalValue} of {finalColor}");
        
        StartCoroutine(AnimateCardToStack(finalColor, finalValue));
    }
    
    /// <summary>
    /// NEW: Animate card to stacking play area
    /// </summary>
    private IEnumerator AnimateCardToStack(string cardColor, int cardValue)
    {
        isAnimating = true;
        Debug.Log($"🃏 Starting card animation to stack: {cardValue} of {cardColor}");
        
        // Create card at deck position
        GameObject animatingCard = InstantiateCardOnDeck();
        if (animatingCard == null)
        {
            isAnimating = false;
            yield break;
        }
        
        // Setup card data
        CardController cardController = animatingCard.GetComponent<CardController>();
        if (cardController == null)
        {
            Debug.LogError("🃏 Card prefab needs CardController!");
            Destroy(animatingCard);
            isAnimating = false;
            yield break;
        }
        
        // Setup card (start face down)
        cardController.SetCard(cardColor, cardValue);
        cardController.SetupFaceDownCard();
        
        // Calculate stacking position
        Vector2 stackedPosition = CalculateStackPosition();
        
        // Animate: Move, flip, and stack
        yield return StartCoroutine(MoveFlipAndStack(animatingCard, cardController, stackedPosition, cardColor, cardValue));
        
        // Add to play area stack
        AddCardToStack(animatingCard, cardColor, cardValue);
        
        isAnimating = false;
        OnFlipComplete?.Invoke();
    }
    
    private IEnumerator FlipCardToTopCard()
    {
        Debug.Log("🃏 Starting card prefab flip animation");
        
        // HIDE the TopCard immediately so only the flipped card shows
        topCardImage.color = Color.clear;
        
        // Instantiate the card prefab on top of deck
        GameObject animatingCard = InstantiateCardOnDeck();
        if (animatingCard == null)
        {
            Debug.LogError("🃏 Failed to create card from prefab");
            isAnimating = false;
            yield break;
        }
        
        // Get card controller to set it up
        CardController cardController = animatingCard.GetComponent<CardController>();
        if (cardController == null)
        {
            Debug.LogError("🃏 Card prefab missing CardController!");
            Destroy(animatingCard);
            isAnimating = false;
            yield break;
        }
        
        // Setup card with starting card data
        string cardColor = GetStartingCardColor();
        int cardValue = GetStartingCardValue();
        
        Debug.Log($"🃏 DEBUG: Setting card to {cardValue} of {cardColor}");
        
        // Set the card data but start face down
        cardController.SetCard(cardColor, cardValue);
        cardController.SetupFaceDownCard(); // Start showing back
        
        Debug.Log($"🃏 Card prefab created showing back, will reveal {cardValue} of {cardColor}");
        
        // Animate: Move right and flip
        yield return StartCoroutine(MoveAndFlipCard(animatingCard, cardController));
        
        // Complete the animation
        CompleteCardAnimation(animatingCard, cardColor, cardValue);
    }
    
    private GameObject InstantiateCardOnDeck()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("🃏 No card prefab assigned!");
            return null;
        }
        
        // Find a safe parent for the animating card (avoid interfering with player displays)
        Transform safeParent = deckImage?.transform.parent ?? this.transform;
        
        // Instantiate card at deck position with a safe parent
        GameObject card = Instantiate(cardPrefab, safeParent);
        
        // Position it at the specified deck spawn position
        RectTransform cardRect = card.GetComponent<RectTransform>();
        
        if (cardRect != null)
        {
            // Set position without copying deck anchors (which might interfere with layout)
            cardRect.anchoredPosition = deckSpawnPosition;
            
            // Only match size, not anchors (to avoid layout conflicts)
            if (deckImage != null)
            {
                RectTransform deckRect = deckImage.GetComponent<RectTransform>();
                cardRect.sizeDelta = deckRect.sizeDelta;
                // DON'T copy anchors - they might conflict with player displays
            }
            
            Debug.Log($"🃏 Card prefab positioned at {deckSpawnPosition}");
            
            // DEBUG: Log TopCard position for comparison
            if (topCardImage != null)
            {
                RectTransform topCardRect = topCardImage.GetComponent<RectTransform>();
                Debug.Log($"🃏 TopCard is at position {topCardRect.anchoredPosition}");
            }
        }
        
        // Use a more conservative sorting approach
        Canvas canvas = card.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = card.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10; // Lower value, less likely to interfere with other UI
        
        Debug.Log("🃏 Card prefab instantiated on deck");
        return card;
    }
    
    private IEnumerator MoveAndFlipCard(GameObject card, CardController cardController)
    {
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform topCardRect = topCardImage.GetComponent<RectTransform>();
        
        Vector2 startPos = deckSpawnPosition; // Use the specified deck position
        Vector2 endPos = topCardRect.anchoredPosition;
        
        // Phase 1: Move right while face down
        float phase1Duration = flipDuration * 0.4f; // 40% of animation
        float elapsedTime = 0f;
        
        Vector2 midPos = startPos + Vector2.right * moveDistance;
        
        Debug.Log($"🃏 Phase 1: Moving from deck {startPos} to {midPos}");
        
        while (elapsedTime < phase1Duration)
        {
            float progress = elapsedTime / phase1Duration;
            cardRect.anchoredPosition = Vector2.Lerp(startPos, midPos, progress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Phase 2: Flip the card (face down to face up)
        Debug.Log("🃏 Phase 2: Flipping card to reveal front");
        yield return StartCoroutine(FlipCardAnimation(cardController));
        
        // Phase 3: Move to final TopCard position
        float phase3Duration = flipDuration * 0.4f; // 40% of animation
        elapsedTime = 0f;
        
        Debug.Log($"🃏 Phase 3: Moving from {midPos} to TopCard {endPos}");
        
        while (elapsedTime < phase3Duration)
        {
            float progress = elapsedTime / phase3Duration;
            cardRect.anchoredPosition = Vector2.Lerp(midPos, endPos, progress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final position
        cardRect.anchoredPosition = endPos;
        Debug.Log("🃏 Card movement complete");
    }
    
    private IEnumerator FlipCardAnimation(CardController cardController)
    {
        float flipAnimDuration = flipDuration * 0.2f; // 20% of total animation
        
        // Use the card controller's flip method if available
        if (cardController != null)
        {
            // Get the card data for the flip
            string cardColor = GetStartingCardColor();
            int cardValue = GetStartingCardValue();
            
            Debug.Log($"🃏 DEBUG: Flipping card to show {cardValue} of {cardColor}");
            
            // Trigger the flip animation built into your CardController
            cardController.FlipToRevealCard(cardColor, cardValue);
            
            // Wait for flip to complete
            yield return new WaitForSeconds(flipAnimDuration);
        }
        
        Debug.Log("🃏 Card flip animation complete");
    }
    
    private void CompleteCardAnimation(GameObject animatingCard, string cardColor, int cardValue)
    {
        Debug.Log($"🃏 Completing card animation - setting TopCard to {cardValue} of {cardColor}");
        
        // Update the actual TopCard with the same data
        CardController topCardController = topCardImage.GetComponent<CardController>();
        if (topCardController != null)
        {
            topCardController.SetCard(cardColor, cardValue);
            Debug.Log($"🃏 TopCard updated to {cardValue} of {cardColor}");
        }
        else
        {
            Debug.LogWarning("🃏 TopCard has no CardController - color may not be set correctly");
        }
        
        // Make TopCard visible again (was hidden during animation)
        topCardImage.color = Color.white;
        
        // Destroy the animating card since TopCard now shows the result
        if (animatingCard != null)
        {
            Destroy(animatingCard);
            Debug.Log("🃏 Animating card destroyed, TopCard now shows the card");
        }
        
        isAnimating = false;
        OnFlipComplete?.Invoke();
        Debug.Log("🃏 Card prefab animation fully complete!");
    }
    
    private string GetStartingCardColor()
    {
        // During testing, use test values - NO INPUT CLASS USAGE
        if (testOnStart || isInTestMode)
        {
            return testCardColor;
        }
        
        // Get from GameStateManager - NO INPUT CLASS USAGE
        GameStateManager gameStateManager = FindFirstObjectByType<GameStateManager>();
        if (gameStateManager != null)
        {
            string currentCard = gameStateManager.GetCurrentTopCard();
            if (!string.IsNullOrEmpty(currentCard) && currentCard.Contains("_"))
            {
                return currentCard.Split('_')[1];
            }
        }
        return "red"; // Default fallback
    }
    
    private int GetStartingCardValue()
    {
        // During testing, use test values - NO INPUT CLASS USAGE  
        if (testOnStart || isInTestMode)
        {
            return testCardValue;
        }
        
        // Get from GameStateManager - NO INPUT CLASS USAGE
        GameStateManager gameStateManager = FindFirstObjectByType<GameStateManager>();
        if (gameStateManager != null)
        {
            string currentCard = gameStateManager.GetCurrentTopCard();
            if (!string.IsNullOrEmpty(currentCard) && currentCard.Contains("_"))
            {
                string valuePart = currentCard.Split('_')[0];
                if (int.TryParse(valuePart, out int value))
                {
                    return value;
                }
            }
        }
        return 7; // Default fallback
    }
    
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    // ============== TESTING METHODS ==============
    
    /// <summary>
    /// Test the flip animation with the specified test card
    /// Call this from inspector or with keyboard shortcut SPACE
    /// </summary>
    [ContextMenu("Test Flip Animation")]
    public void TestFlipAnimation()
    {
        if (isAnimating)
        {
            Debug.Log("🃏 TEST: Animation already in progress, skipping test");
            return;
        }
        
        Debug.Log($"🃏 TEST: Starting flip animation test with {testCardValue} of {testCardColor}");
        Debug.Log("🃏 TEST: Press SPACE to test again, R to reset");
        
        // Temporarily set test card data
        string originalColor = "";
        int originalValue = 0;
        
        // Store original game state if available
        GameStateManager gameStateManager = FindFirstObjectByType<GameStateManager>();
        if (gameStateManager != null)
        {
            string currentCard = gameStateManager.GetCurrentTopCard();
            if (!string.IsNullOrEmpty(currentCard) && currentCard.Contains("_"))
            {
                originalColor = currentCard.Split('_')[1];
                int.TryParse(currentCard.Split('_')[0], out originalValue);
            }
        }
        
        StartCoroutine(TestFlipAnimationCoroutine(originalColor, originalValue));
    }
    
    private IEnumerator TestFlipAnimationCoroutine(string originalColor, int originalValue)
    {
        // Run the flip animation with test data
        yield return StartCoroutine(FlipCardToTopCard());
        
        Debug.Log($"🃏 TEST: Animation complete! Showed {testCardValue} of {testCardColor}");
        Debug.Log("🃏 TEST: Press SPACE to test again, R to reset TopCard");
    }
    
    /// <summary>
    /// Reset the card display to initial state
    /// Call with keyboard shortcut R
    /// </summary>
    [ContextMenu("Reset Card Display")]
    public void ResetCardDisplay()
    {
        Debug.Log("🃏 TEST: Resetting everything");
        
        // NEW: Clear the play area stack
        ClearPlayArea();
        
        // Clean up any lingering animating cards that might interfere with UI
        GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
        foreach (GameObject card in allCards)
        {
            if (card.name.Contains("Clone")) // Instantiated cards have "Clone" in name
            {
                Destroy(card);
            }
        }
        
        // Hide TopCard (legacy support)
        if (topCardImage != null)
        {
            topCardImage.color = new Color(1f, 1f, 1f, 0f); // Transparent
        }
        
        // Show deck
        if (deckImage != null && cardBackSprite != null)
        {
            deckImage.sprite = cardBackSprite;
            deckImage.color = Color.white;
        }
        
        // Reset animation state
        isAnimating = false;
        
        Debug.Log("🃏 TEST: Reset complete - Stack cleared, Deck visible");
        Debug.Log("🃏 TEST: Press SPACE to test flip animation");
        Debug.Log("🃏 TEST: Reset complete - Stack cleared, Deck visible");
        Debug.Log("🃏 TEST: Press SPACE to test flip animation");
    }
    
    /// <summary>
    /// Override the starting card data for testing (used by TestFlipAnimation)
    /// </summary>
    private string GetStartingCardColorForTest()
    {
        // Use test data instead of game state during testing
        return testCardColor;
    }
    
    private int GetStartingCardValueForTest()
    {
        // Use test data instead of game state during testing  
        return testCardValue;
    }
    
    // ============== NEW STACKING SYSTEM METHODS ==============
    
    private Vector2 CalculateStackPosition()
    {
        // Base position - use playArea if set, otherwise use (0,0)
        Vector2 basePos = playAreaPosition;
        
        if (playArea != null)
        {
            RectTransform playAreaRect = playArea.GetComponent<RectTransform>();
            if (playAreaRect != null)
            {
                basePos = playAreaRect.anchoredPosition;
            }
        }
        
        // Add slight offset for each card (stacking effect)
        float xOffset = UnityEngine.Random.Range(-stackOffset, stackOffset);
        float yOffset = UnityEngine.Random.Range(-stackOffset, stackOffset);
        
        // Each new card slightly offset from the last
        Vector2 stackedPos = basePos + new Vector2(xOffset, yOffset);
        
        Debug.Log($"🃏 Calculated stack position: {stackedPos} (card #{totalCardsPlayed + 1})");
        return stackedPos;
    }
    
    private IEnumerator MoveFlipAndStack(GameObject card, CardController cardController, Vector2 targetPos, string cardColor, int cardValue)
    {
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Vector2 startPos = deckSpawnPosition;
        
        // Phase 1: Move toward play area (face down)
        float phase1Duration = flipDuration * 0.4f;
        Vector2 midPos = startPos + Vector2.right * moveDistance;
        
        yield return StartCoroutine(MoveCardToPosition(cardRect, startPos, midPos, phase1Duration));
        
        // Phase 2: Flip card
        Debug.Log("🃏 Flipping card to reveal");
        cardController.FlipToRevealCard(cardColor, cardValue);
        yield return new WaitForSeconds(flipDuration * 0.2f);
        
        // Phase 3: Move to final stacked position
        float phase3Duration = flipDuration * 0.4f;
        yield return StartCoroutine(MoveCardToPosition(cardRect, midPos, targetPos, phase3Duration));
        
        // Add slight rotation for natural stacked look
        float randomRotation = UnityEngine.Random.Range(-stackRotation, stackRotation);
        cardRect.rotation = Quaternion.Euler(0, 0, randomRotation);
        
        Debug.Log("🃏 Card landed in play area stack");
    }
    
    private IEnumerator MoveCardToPosition(RectTransform cardRect, Vector2 fromPos, Vector2 toPos, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            cardRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        cardRect.anchoredPosition = toPos;
    }
    
    private void AddCardToStack(GameObject card, string cardColor, int cardValue)
    {
        // Add to our tracking list
        playedCards.Add(card);
        totalCardsPlayed++;
        
        // Clean up old cards if stack gets too big
        if (playedCards.Count > maxVisibleCards)
        {
            GameObject oldCard = playedCards[0];
            playedCards.RemoveAt(0);
            
            if (oldCard != null)
            {
                Destroy(oldCard);
                Debug.Log("🃏 Removed old card from stack to keep it manageable");
            }
        }
        
        Debug.Log($"🃏 Card {cardValue} of {cardColor} added to stack. Stack size: {playedCards.Count}");
    }
    
    /// <summary>
    /// NEW: Get the top card from the play area stack
    /// </summary>
    public GameObject GetTopCardInStack()
    {
        if (playedCards.Count > 0)
        {
            return playedCards[playedCards.Count - 1];
        }
        return null;
    }
    
    /// <summary>
    /// NEW: Get current stack size
    /// </summary>
    public int GetStackSize()
    {
        return playedCards.Count;
    }
    
    /// <summary>
    /// NEW: Clear the entire play area stack
    /// </summary>
    public void ClearPlayArea()
    {
        Debug.Log("🃏 Clearing play area stack");
        
        foreach (GameObject card in playedCards)
        {
            if (card != null)
                Destroy(card);
        }
        
        playedCards.Clear();
        totalCardsPlayed = 0;
        
        Debug.Log("🃏 Play area cleared");
    }
}
