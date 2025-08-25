using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Card data class
[System.Serializable]
public class CardData
{
    public string color;
    public int value;
    public string rank; // Alternative to value for face cards
    
    public CardData(string color, int value)
    {
        this.color = color;
        this.value = value;
    }
    
    public CardData(string color, string rank)
    {
        this.color = color;
        this.rank = rank;
        
        // Convert rank to value
        switch (rank.ToUpper())
        {
            case "A": this.value = 1; break;
            case "J": this.value = 11; break;
            case "Q": this.value = 12; break;
            case "K": this.value = 13; break;
            default: 
                if (int.TryParse(rank, out int val))
                    this.value = val;
                break;
        }
    }
}

public class CardController : MonoBehaviour
{
    [Header("Card Components")]
    public TextMeshProUGUI cardValueTop;
    public TextMeshProUGUI cardValueBottom;
    public TextMeshProUGUI cardValueCenter;  // Center number/letter
    public Image innerCard;  // The inner black card area that will change color
    public Outline cardOutline;
    public Image cardBackground;
    
    [Header("Color Settings")]
    public Color redColor = new Color(0.6f, 0.08f, 0.08f);
    public Color blueColor = new Color(0.08f, 0.08f, 0.6f);
    public Color greenColor = new Color(0.08f, 0.4f, 0.08f);
    public Color yellowColor = new Color(0.6f, 0.6f, 0.08f);
    
    [Header("Special 8 Card Settings")]
    public Sprite eightCardSprite; // Drag your custom image here
    public Color eightCardTextColor = Color.black; // Black text for 8s
    
    [Header("Card Flip Animation")]
    public GameObject faceUpElements; // Assign this in inspector - parent containing all face-up elements
    public GameObject logoElements;   // Assign this in inspector - parent containing logo
    public Image logoImage;          // Your game logo image
    public Outline topCardOutline;   // Assign this in inspector - TopCard's outline component

    private bool isFaceDown = false;
    private bool isFlipping = false;
    
    void Start()
    {
        // Ensure all components are properly initialized
        InitializeComponents();
    }
    
    /// <summary>
    /// Initialize all card components - can be called multiple times safely
    /// </summary>
    void InitializeComponents()
    {
        // Auto-assign components if not set
        if (cardBackground == null) 
        {
            cardBackground = GetComponent<Image>();
        }
        
        // CRITICAL FIX: Enhanced outline detection with multiple fallback methods
        if (cardOutline == null)
        {
            // Method 1: Try to get outline from the same GameObject as card background
            if (cardBackground != null) 
            {
                cardOutline = cardBackground.GetComponent<Outline>();
            }
            
            // Method 2: Try to get outline from this GameObject
            if (cardOutline == null)
            {
                cardOutline = GetComponent<Outline>();
            }
            
            // Method 3: Search child objects for outline
            if (cardOutline == null)
            {
                cardOutline = GetComponentInChildren<Outline>();
            }
            
            // Method 4: Search parent objects for outline
            if (cardOutline == null)
            {
                cardOutline = GetComponentInParent<Outline>();
            }
            
            // Method 5: Add outline component if none found
            if (cardOutline == null && cardBackground != null)
            {
                cardOutline = cardBackground.gameObject.AddComponent<Outline>();
                
                // Set default outline properties
                cardOutline.effectColor = Color.black;
                cardOutline.effectDistance = new Vector2(2, 2);
                cardOutline.useGraphicAlpha = false;
            }
        }
        
        // Find child components automatically if not assigned
        if (cardValueTop == null) 
        {
            cardValueTop = transform.Find("CardValueTop")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (cardValueBottom == null) 
        {
            cardValueBottom = transform.Find("CardValueBottom")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (cardValueCenter == null) 
        {
            cardValueCenter = transform.Find("CardValueCenter")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (innerCard == null) 
        {
            innerCard = transform.Find("InnerCard")?.GetComponent<Image>();
        }
        
        // Try to find logo image if not assigned
        if (logoImage == null)
        {
            logoImage = transform.Find("Logo")?.GetComponent<Image>() ?? 
                       transform.Find("LogoImage")?.GetComponent<Image>() ??
                       transform.Find("GameLogo")?.GetComponent<Image>();
                       
            if (logoImage != null)
            {
                Debug.Log($"🃏 Found logo image: {logoImage.name}");
            }
        }
        
        // Try to find faceUpElements and logoElements if not assigned
        if (faceUpElements == null)
        {
            Transform faceUpTransform = transform.Find("FaceUpElements") ?? transform.Find("CardElements");
            if (faceUpTransform != null)
            {
                faceUpElements = faceUpTransform.gameObject;
                Debug.Log($"🃏 Found faceUpElements: {faceUpElements.name}");
            }
        }
        
        if (logoElements == null)
        {
            Transform logoTransform = transform.Find("LogoElements") ?? transform.Find("Logo");
            if (logoTransform != null)
            {
                logoElements = logoTransform.gameObject;
                Debug.Log($"🃏 Found logoElements: {logoElements.name}");
            }
        }
    }
    
    public void SetCard(string color, int value)
    {
        SetCard(color, value, false); // Default to not forcing color for 8s
    }
    
    public void SetCard(string color, int value, bool forceColorFor8s)
    {
        Debug.Log($"🃏 CARDCONTROLLER SETCARD: {color} {value} (force8={forceColorFor8s})");
        
        // CRITICAL FIX: Ensure components are initialized before setting card
        InitializeComponents();
        
        // Get display value
        string displayValue = GetCardDisplayValue(value);
        Debug.Log($"🃏 Display value: {displayValue}");
        
        // Special handling for 8s (wild cards) - unless we're forcing the color
        if (value == 8 && !forceColorFor8s)
        {
            Debug.Log("🃏 Setting as 8 card (wild)");
            SetEightCard(displayValue);
            return;
        }
        
        // Regular card handling (or forced color for 8s)
        Color cardColor = GetCardColor(color);
        Debug.Log($"🃏 Card color: {cardColor}");
        
        // Set all text elements to the card color (they will show on the white circle)
        if (cardValueTop != null)
        {
            cardValueTop.text = displayValue;
            cardValueTop.color = cardColor;
            Debug.Log($"🃏 Set cardValueTop: {displayValue} with color {cardColor}");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
            cardValueBottom.color = cardColor;
            Debug.Log($"🃏 Set cardValueBottom: {displayValue} with color {cardColor}");
        }
        
        // Set center card value 
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
            cardValueCenter.color = cardColor;
            Debug.Log($"🃏 Set cardValueCenter: {displayValue} with color {cardColor}");
        }
        
        // Keep the main card background white/default
        if (cardBackground != null)
        {
            cardBackground.color = Color.white; // Keep the outer card white
            Debug.Log("🃏 Set cardBackground to white");
        }
        
        // Set the inner card (black area) to the card color
        if (innerCard != null)
        {
            // For forced 8 card colors, use aggressive reset
            if (value == 8 && forceColorFor8s)
            {
                Debug.Log("🃏 Using aggressive reset for forced 8 card color");
                ForceResetImageComponent(innerCard, cardColor);
            }
            else
            {
                // CRITICAL: Always reset sprite first for regular cards
                innerCard.sprite = null;
                innerCard.color = cardColor;
                
                // ADDITIONAL FIX: Force the Image to use Image Type: Simple to ensure solid color shows
                innerCard.type = Image.Type.Simple;
                innerCard.preserveAspect = false;
                
                // CRITICAL: Ensure full opacity for vibrant colors
                Color tempColor = innerCard.color;
                tempColor.a = 1.0f; // Full alpha
                innerCard.color = tempColor;
                
                // Force UI refresh to ensure color displays properly
                innerCard.enabled = false;
                innerCard.enabled = true;
                
                Debug.Log($"🃏 Set innerCard color to: {innerCard.color}");
            }
        }
        else
        {
            Debug.LogError("🃏 innerCard is null!");
        }
        
        // CRITICAL FIX: Set outline color to match with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = cardColor;
            Debug.Log($"🃏 Set cardOutline color to: {cardColor}");
        }
        else
        {
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = cardColor;
                Debug.Log($"🃏 Set cardOutline color to: {cardColor} (after re-init)");
            }
            else
            {
                Debug.LogError("🃏 cardOutline is null even after re-init!");
            }
        }
        
        Debug.Log($"✅ CARDCONTROLLER SETCARD COMPLETE: {displayValue} of {color}");
    }
    
    // Special method for setting up 8 cards with custom appearance
    void SetEightCard(string displayValue)
    {
        // CRITICAL FIX: Ensure components are initialized
        InitializeComponents();
        
        // Set all text elements to black
        if (cardValueTop != null)
        {
            cardValueTop.text = displayValue;
            cardValueTop.color = eightCardTextColor; // Black text
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
            cardValueBottom.color = eightCardTextColor; // Black text
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
            cardValueCenter.color = eightCardTextColor; // Black text
        }
        
        // Keep the main card background white
        if (cardBackground != null)
        {
            cardBackground.color = Color.white;
        }
        
        // Set the inner card to show custom image
        if (innerCard != null)
        {
            if (eightCardSprite != null)
            {
                // Use custom sprite for 8 cards
                innerCard.sprite = eightCardSprite;
                innerCard.color = Color.white; // White tint to show image normally
                innerCard.type = Image.Type.Simple;
                innerCard.preserveAspect = false;
            }
            else
            {
                // Fallback to black if no sprite assigned
                innerCard.sprite = null;
                innerCard.color = Color.black;
                innerCard.type = Image.Type.Simple;
                innerCard.preserveAspect = false;
            }
        }
        
        // CRITICAL FIX: Set outline to black with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.black;
        }
        else
        {
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = Color.black;
            }
        }
    }
    
    // Method to update an 8 card with a chosen color after color selection
    public void SetEightCardWithColor(string chosenColor) // Public method for GameManager
    {
        // CRITICAL FIX: Ensure components are initialized
        InitializeComponents();
        
        Color cardColor = GetCardColor(chosenColor);
        
        // Set all text elements to the chosen color
        if (cardValueTop != null)
        {
            cardValueTop.text = "8";
            cardValueTop.color = cardColor;
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = "8";
            cardValueBottom.color = cardColor;
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = "8";
            cardValueCenter.color = cardColor;
        }
        
        // Keep the main card background white
        if (cardBackground != null)
        {
            cardBackground.color = Color.white;
        }
        
        // Set the inner card to the chosen color (remove spiral, show solid color)
        if (innerCard != null)
        {
            innerCard.sprite = null; // Remove the spiral sprite
            innerCard.color = cardColor; // Set to chosen color
        }
        
        // Set outline to match chosen color with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = cardColor;
        }
        else
        {
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = cardColor;
            }
        }
    }
    
    // Helper method to aggressively reset an Image component
    void ForceResetImageComponent(Image imageComponent, Color targetColor)
    {
        // Disable the component temporarily
        imageComponent.enabled = false;
        
        // Clear sprite completely
        imageComponent.sprite = null;
        imageComponent.overrideSprite = null;
        
        // Reset image settings
        imageComponent.type = Image.Type.Simple;
        imageComponent.preserveAspect = false;
        imageComponent.fillCenter = true;
        
        // Set color
        imageComponent.color = targetColor;
        
        // ADDITIONAL: Force Unity to refresh the component
        Canvas.ForceUpdateCanvases();
        
        // Re-enable
        imageComponent.enabled = true;
    }
    
    public void SetCard(CardData cardData)
    {
        if (cardData != null)
        {
            SetCard(cardData.color, cardData.value);
        }
    }
    
    // Convert card value to display string
    string GetCardDisplayValue(int value)
    {
        switch (value)
        {
            case 1: return "1";  // Keep 1 as 1, not A
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            default: return value.ToString();
        }
    }
    
    // Get card color based on color name
    Color GetCardColor(string color)
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
                return redColor;
        }
    }
    
    /// <summary>
    /// Reset the card to its initial state - called when resetting the game
    /// </summary>
    public void ResetCard()
    {
        Debug.Log("🔄 RESETTING CARD CONTROLLER");
        
        // Clear all text elements
        if (cardValueTop != null)
        {
            cardValueTop.text = "";
            cardValueTop.color = Color.black;
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = "";
            cardValueBottom.color = Color.black;
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = "";
            cardValueCenter.color = Color.black;
        }
        
        // Reset background to white/default
        if (cardBackground != null)
        {
            cardBackground.color = Color.white;
        }
        
        // Reset inner card to ready state (not invisible)
        if (innerCard != null)
        {
            innerCard.sprite = null;
            innerCard.color = Color.white; // Keep it visible and ready for new card
            innerCard.type = Image.Type.Simple;
            innerCard.preserveAspect = false;
            
            // CRITICAL: Ensure proper alpha for visibility
            Color tempColor = innerCard.color;
            tempColor.a = 1.0f; // Full opacity
            innerCard.color = tempColor;
            
            // Force refresh the UI component
            innerCard.enabled = false;
            innerCard.enabled = true;
        }
        
        // Reset outline to default
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.black;
            cardOutline.effectDistance = new Vector2(2, 2);
        }
        
        // Reset flip animation state
        isFaceDown = false;
        isFlipping = false;
        
        // Show face-up elements and hide logo elements (normal card state)
        if (faceUpElements != null)
            faceUpElements.SetActive(true);
        
        if (logoElements != null)
            logoElements.SetActive(false);
        
        // Ensure the card is visible
        gameObject.SetActive(true);
        
        Debug.Log("✅ CARD CONTROLLER RESET COMPLETE");
    }
    
    // === CARD FLIP ANIMATION METHODS ===
    
    /// <summary>
    /// Set up the card to show face-down (with logo)
    /// </summary>
    public void SetupFaceDownCard()
    {
        Debug.Log("🃏 CARDCONTROLLER: Setting up face-down card with logo");
        
        // CRITICAL FIX: Ensure components are initialized (matching SetEightCard method)
        InitializeComponents();
        
        isFaceDown = true;
        
        // CRITICAL: Hide all numbers and text elements
        if (cardValueTop != null)
        {
            cardValueTop.text = "";
            cardValueTop.color = Color.clear; // Make completely transparent
            Debug.Log("🃏 Cleared cardValueTop");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = "";
            cardValueBottom.color = Color.clear; // Make completely transparent
            Debug.Log("🃏 Cleared cardValueBottom");
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = "";
            cardValueCenter.color = Color.clear; // Make completely transparent
            Debug.Log("🃏 Cleared cardValueCenter");
        }
        
        // Set inner card to BLACK (no color showing)
        if (innerCard != null)
        {
            innerCard.sprite = null;
            innerCard.color = Color.black;
            innerCard.type = Image.Type.Simple;
            innerCard.preserveAspect = false;
            Debug.Log("🃏 Set innerCard to black");
        }
        else
        {
            Debug.LogError("🃏 innerCard is null!");
        }
        
        // Hide face-up elements
        if (faceUpElements != null)
        {
            faceUpElements.SetActive(false);
            Debug.Log("🃏 Hid faceUpElements");
        }
        else
        {
            // If faceUpElements is null, hide all text components manually
            Debug.Log("🃏 faceUpElements is null - hiding text manually");
            if (cardValueTop != null) cardValueTop.gameObject.SetActive(false);
            if (cardValueBottom != null) cardValueBottom.gameObject.SetActive(false);
            if (cardValueCenter != null) cardValueCenter.gameObject.SetActive(false);
        }
        
        // Show logo elements
        if (logoElements != null)
        {
            logoElements.SetActive(true);
            Debug.Log("🃏 Showed logoElements");
        }
        else
        {
            // Try to find and show the logo image directly
            if (logoImage != null)
            {
                logoImage.gameObject.SetActive(true);
                Debug.Log("🃏 Showed logoImage directly");
            }
            else
            {
                // Try to find logo by name
                Transform logoTransform = transform.Find("Logo") ?? transform.Find("LogoImage") ?? transform.Find("GameLogo");
                if (logoTransform != null)
                {
                    logoTransform.gameObject.SetActive(true);
                    Debug.Log("🃏 Found and showed logo by name");
                }
                else
                {
                    Debug.Log("🃏 logoElements is null - logo will stay visible if it exists");
                }
            }
        }
        
        // Set card background to black/dark
        if (cardBackground != null)
        {
            cardBackground.color = Color.black;
            Debug.Log("🃏 Set cardBackground to black");
        }
        
        // CRITICAL FIX: Set outline to BLACK with enhanced reliability (matching SetEightCard method)
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.black;
            Debug.Log("🃏 Set cardOutline to black");
        }
        else
        {
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = Color.black;
                Debug.Log("🃏 Set cardOutline to black (after re-init)");
            }
            else
            {
                Debug.LogError("🃏 cardOutline is null even after re-init!");
            }
        }
        
        Debug.Log("✅ CARDCONTROLLER: Face-down card setup complete - should be black with logo");
    }
    
    /// <summary>
    /// Flip animation to reveal the actual card
    /// </summary>
    public void FlipToRevealCard(string color, int value, float flipDuration = 0.8f)
    {
        if (isFlipping) return;
        
        Debug.Log($"🎬 Starting flip animation to reveal {value} of {color}");
        StartCoroutine(FlipCardCoroutine(color, value, flipDuration));
    }
    
    /// <summary>
    /// The actual flip animation coroutine
    /// </summary>
    private System.Collections.IEnumerator FlipCardCoroutine(string color, int value, float duration)
    {
        isFlipping = true;
        
        float halfDuration = duration * 0.5f;
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        
        // First half - shrink horizontally (hide face down)
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            
            Vector3 scale = originalScale;
            scale.x = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = scale;
            
            yield return null;
        }
        
        // Switch from face down to face up at middle of animation
        if (logoElements != null)
        {
            logoElements.SetActive(false);
            Debug.Log("🃏 FLIP: Hid logoElements");
        }
        else
        {
            // Try to find and hide the logo image directly
            if (logoImage != null)
            {
                logoImage.gameObject.SetActive(false);
                Debug.Log("🃏 FLIP: Hid logoImage directly");
            }
            else
            {
                // Try to find logo by name and hide it
                Transform logoTransform = transform.Find("Logo") ?? transform.Find("LogoImage") ?? transform.Find("GameLogo");
                if (logoTransform != null)
                {
                    logoTransform.gameObject.SetActive(false);
                    Debug.Log("🃏 FLIP: Found and hid logo by name");
                }
                else
                {
                    Debug.Log("🃏 FLIP: logoElements is null - can't hide logo, searching for Image components with logo");
                    
                    // Last resort: find all Image components and look for one that might be the logo
                    Image[] images = GetComponentsInChildren<Image>();
                    foreach (Image img in images)
                    {
                        if (img != cardBackground && img != innerCard && img.sprite != null)
                        {
                            // This might be the logo image
                            img.gameObject.SetActive(false);
                            Debug.Log($"🃏 FLIP: Hid potential logo image: {img.name}");
                        }
                    }
                }
            }
        }
        
        if (faceUpElements != null)
        {
            faceUpElements.SetActive(true);
            Debug.Log("🃏 FLIP: Showed faceUpElements");
        }
        else
        {
            // If faceUpElements is null, show all text components manually
            Debug.Log("🃏 FLIP: faceUpElements is null - showing text manually");
            if (cardValueTop != null) cardValueTop.gameObject.SetActive(true);
            if (cardValueBottom != null) cardValueBottom.gameObject.SetActive(true);
            if (cardValueCenter != null) cardValueCenter.gameObject.SetActive(true);
        };
        
        // Set the actual card using your existing method
        SetCard(color, value);
        
        // Second half - expand horizontally (show face up)
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / halfDuration;
            
            Vector3 scale = originalScale;
            scale.x = Mathf.Lerp(0f, 1f, progress);
            transform.localScale = scale;
            
            yield return null;
        }
        
        // Ensure final scale is correct
        transform.localScale = originalScale;
        isFaceDown = false;
        isFlipping = false;
        
        Debug.Log("🎬 Flip animation complete!");
    }
    
    /// <summary>
    /// Test method for the flip animation
    /// </summary>
    [ContextMenu("Test Card Flip Animation")]
    public void TestCardFlipAnimation()
    {
        SetupFaceDownCard();
        
        // Wait 2 seconds then flip to a test card (yellow 3)
        StartCoroutine(TestFlipAfterDelay());
    }
    
    private System.Collections.IEnumerator TestFlipAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        FlipToRevealCard("yellow", 3); // Test with yellow 3 like you wanted
    }
}