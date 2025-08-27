using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Deck manager with card stacking system - cards animate to play area and stack on each other
/// </summary>
public class DeckManagerStacking : MonoBehaviour
{
    [Header("Card References")]
    public Image deckImage;         // The deck (shows card back)
    public GameObject cardPrefab;   // Your card prefab with front/back
    public Transform playArea;      // Empty GameObject where cards land and stack
    
    [Header("Animation Settings")]
    public float flipDuration = 1.5f;
    public float moveDistance = 200f;
    public Vector2 deckSpawnPosition = new Vector2(-255f, -268f);
    public Vector2 playAreaPosition = new Vector2(0f, 0f); // Where cards land
    
    [Header("Card Stacking")]
    public float stackOffset = 5f;     // How much each card offsets from the previous
    public float stackRotation = 10f;  // Random rotation range for natural look
    public int maxVisibleCards = 10;   // Max cards to keep visible in stack
    
    [Header("Sprites")]
    public Sprite cardBackSprite;
    
    [Header("Testing")]
    [SerializeField] private bool testOnStart = false;
    [SerializeField] private string testCardColor = "red";
    [SerializeField] private int testCardValue = 7;
    
    // Card stack management
    private List<GameObject> playedCards = new List<GameObject>();
    private int totalCardsPlayed = 0;
    private bool isAnimating = false;
    private bool isInTestMode = false;
    
    public event System.Action OnFlipComplete;
    
    public void Initialize()
    {
        Debug.Log("🃏 DeckManagerStacking Initialize() - Stacking System");
        
        // Find or create play area
        if (playArea == null)
        {
            CreatePlayArea();
        }
        
        // Find deck if not assigned
        if (deckImage == null)
        {
            GameObject deckObj = GameObject.Find("DeckStack");
            if (deckObj != null)
                deckImage = deckObj.GetComponent<Image>();
        }
        
        SetupInitialState();
        
        if (testOnStart)
        {
            StartCoroutine(TestAfterDelay());
        }
    }
    
    private void CreatePlayArea()
    {
        // Create empty GameObject as play area
        GameObject playAreaObj = new GameObject("PlayArea");
        playAreaObj.transform.SetParent(this.transform.parent);
        
        RectTransform playAreaRect = playAreaObj.AddComponent<RectTransform>();
        playAreaRect.anchoredPosition = playAreaPosition;
        
        playArea = playAreaObj.transform;
        Debug.Log($"🃏 Created play area at {playAreaPosition}");
    }
    
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TestPlayCard();
        }
        
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetCardDisplay();
        }
        
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ClearPlayArea();
        }
    }
    
    private void SetupInitialState()
    {
        if (deckImage != null && cardBackSprite != null)
        {
            deckImage.sprite = cardBackSprite;
            deckImage.color = Color.white;
            Debug.Log("🃏 Deck setup - ready for card stacking system");
        }
    }
    
    /// <summary>
    /// Play a card from deck to the stacking play area
    /// </summary>
    public void PlayCardFromDeck(string cardColor = "", int cardValue = 0)
    {
        if (isAnimating)
        {
            Debug.Log("🃏 Already animating, queuing card...");
            // Could implement a queue system here later
            return;
        }
        
        StartCoroutine(AnimateCardToPlayArea(cardColor, cardValue));
    }
    
    /// <summary>
    /// Start game flip animation (for initial card)
    /// </summary>
    public void StartGameFlip()
    {
        Debug.Log("🃏 Starting game with stacking system");
        PlayCardFromDeck(); // Play initial card using test values
    }
    
    /// <summary>
    /// Main card animation - from deck to stacked play area
    /// </summary>
    private IEnumerator AnimateCardToPlayArea(string cardColor, int cardValue)
    {
        isAnimating = true;
        Debug.Log("🃏 Starting card animation to play area stack");
        
        // Create card at deck position
        GameObject animatingCard = CreateCardAtDeck();
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
        
        // Use provided values or test values
        string finalColor = !string.IsNullOrEmpty(cardColor) ? cardColor : GetTestCardColor();
        int finalValue = cardValue > 0 ? cardValue : GetTestCardValue();
        
        Debug.Log($"🃏 Playing {finalValue} of {finalColor}");
        
        // Setup card (start face down)
        cardController.SetCard(finalColor, finalValue);
        cardController.SetupFaceDownCard();
        
        // Calculate stacking position
        Vector2 stackedPosition = CalculateStackPosition();
        
        // Animate: Move, flip, and stack
        yield return StartCoroutine(MoveFlipAndStack(animatingCard, cardController, stackedPosition, finalColor, finalValue));
        
        // Add to play area stack
        AddCardToPlayArea(animatingCard, finalColor, finalValue);
        
        isAnimating = false;
        OnFlipComplete?.Invoke();
    }
    
    private GameObject CreateCardAtDeck()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("🃏 No card prefab assigned!");
            return null;
        }
        
        Transform safeParent = playArea ?? this.transform;
        GameObject card = Instantiate(cardPrefab, safeParent);
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchoredPosition = deckSpawnPosition;
            
            if (deckImage != null)
            {
                RectTransform deckRect = deckImage.GetComponent<RectTransform>();
                cardRect.sizeDelta = deckRect.sizeDelta;
            }
        }
        
        // Sorting for animation
        Canvas canvas = card.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = card.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20 + totalCardsPlayed; // Higher for newer cards
        
        Debug.Log($"🃏 Card created at deck position for stacking");
        return card;
    }
    
    private Vector2 CalculateStackPosition()
    {
        // Base position is the play area
        Vector2 basePos = playAreaPosition;
        
        // Add slight offset for each card (stacking effect)
        float xOffset = Random.Range(-stackOffset, stackOffset);
        float yOffset = Random.Range(-stackOffset, stackOffset);
        
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
        
        yield return StartCoroutine(MoveCard(cardRect, startPos, midPos, phase1Duration));
        
        // Phase 2: Flip card
        Debug.Log("🃏 Flipping card to reveal");
        cardController.FlipToRevealCard(cardColor, cardValue);
        yield return new WaitForSeconds(flipDuration * 0.2f);
        
        // Phase 3: Move to final stacked position
        float phase3Duration = flipDuration * 0.4f;
        yield return StartCoroutine(MoveCard(cardRect, midPos, targetPos, phase3Duration));
        
        // Add slight rotation for natural stacked look
        float randomRotation = Random.Range(-stackRotation, stackRotation);
        cardRect.rotation = Quaternion.Euler(0, 0, randomRotation);
        
        Debug.Log("🃏 Card landed in play area stack");
    }
    
    private IEnumerator MoveCard(RectTransform cardRect, Vector2 fromPos, Vector2 toPos, float duration)
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
    
    private void AddCardToPlayArea(GameObject card, string cardColor, int cardValue)
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
        
        Debug.Log($"🃏 Card {cardValue} of {cardColor} added to play area. Stack size: {playedCards.Count}");
    }
    
    /// <summary>
    /// Get the top card from the play area stack
    /// </summary>
    public GameObject GetTopCard()
    {
        if (playedCards.Count > 0)
        {
            return playedCards[playedCards.Count - 1];
        }
        return null;
    }
    
    /// <summary>
    /// Get current stack size
    /// </summary>
    public int GetStackSize()
    {
        return playedCards.Count;
    }
    
    // ============== TESTING METHODS ==============
    
    [ContextMenu("Test Play Card")]
    public void TestPlayCard()
    {
        Debug.Log($"🃏 TEST: Playing card {testCardValue} of {testCardColor}");
        Debug.Log("🃏 TEST: SPACE = play card, R = reset, C = clear stack");
        PlayCardFromDeck(testCardColor, testCardValue);
    }
    
    private IEnumerator TestAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        TestPlayCard();
    }
    
    [ContextMenu("Clear Play Area")]
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
    
    [ContextMenu("Reset Card Display")]
    public void ResetCardDisplay()
    {
        Debug.Log("🃏 TEST: Resetting everything");
        
        ClearPlayArea();
        isAnimating = false;
        
        // Clean up any stray cards
        GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
        foreach (GameObject card in allCards)
        {
            if (card.name.Contains("Clone"))
            {
                Destroy(card);
            }
        }
        
        Debug.Log("🃏 TEST: Reset complete - SPACE to play card");
    }
    
    private string GetTestCardColor()
    {
        return testCardColor;
    }
    
    private int GetTestCardValue()
    {
        return testCardValue;
    }
}
