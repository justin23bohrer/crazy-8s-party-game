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
    
    void Start()
    {
        // Debug.Log("=== CardController Start() ===");
        
        // Ensure all components are properly initialized
        InitializeComponents();
        
        // Debug.Log("=== CardController Start() Complete ===");
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
            // Debug.Log("Card background found: " + (cardBackground != null));
        }
        
        // CRITICAL FIX: Enhanced outline detection with multiple fallback methods
        if (cardOutline == null)
        {
            // Method 1: Try to get outline from the same GameObject as card background
            if (cardBackground != null) 
            {
                cardOutline = cardBackground.GetComponent<Outline>();
                // Debug.Log("Outline search method 1 (cardBackground): " + (cardOutline != null));
            }
            
            // Method 2: Try to get outline from this GameObject
            if (cardOutline == null)
            {
                cardOutline = GetComponent<Outline>();
                // Debug.Log("Outline search method 2 (this GameObject): " + (cardOutline != null));
            }
            
            // Method 3: Search child objects for outline
            if (cardOutline == null)
            {
                cardOutline = GetComponentInChildren<Outline>();
                // Debug.Log("Outline search method 3 (children): " + (cardOutline != null));
            }
            
            // Method 4: Search parent objects for outline
            if (cardOutline == null)
            {
                cardOutline = GetComponentInParent<Outline>();
                // Debug.Log("Outline search method 4 (parents): " + (cardOutline != null));
            }
            
            // Method 5: Add outline component if none found
            if (cardOutline == null && cardBackground != null)
            {
                // Debug.Log("No outline found - adding Outline component to card background");
                cardOutline = cardBackground.gameObject.AddComponent<Outline>();
                
                // Set default outline properties
                cardOutline.effectColor = Color.black;
                cardOutline.effectDistance = new Vector2(2, 2);
                cardOutline.useGraphicAlpha = false;
                
                // Debug.Log("✅ Added new Outline component with default settings");
            }
        }
        
        // Debug.Log("Final outline status: " + (cardOutline != null));
        
        // Find child components automatically if not assigned
        // Debug.Log("Looking for child components...");
        // Debug.Log("Child count: " + transform.childCount);
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Debug.Log("Child " + i + ": " + child.name);
        }
        
        if (cardValueTop == null) 
        {
            cardValueTop = transform.Find("CardValueTop")?.GetComponent<TextMeshProUGUI>();
            // Debug.Log("CardValueTop found: " + (cardValueTop != null));
        }
        
        if (cardValueBottom == null) 
        {
            cardValueBottom = transform.Find("CardValueBottom")?.GetComponent<TextMeshProUGUI>();
            // Debug.Log("CardValueBottom found: " + (cardValueBottom != null));
        }
        
        if (cardValueCenter == null) 
        {
            cardValueCenter = transform.Find("CardValueCenter")?.GetComponent<TextMeshProUGUI>();
            // Debug.Log("CardValueCenter found: " + (cardValueCenter != null));
        }
        
        if (innerCard == null) 
        {
            innerCard = transform.Find("InnerCard")?.GetComponent<Image>();
            // Debug.Log("InnerCard found: " + (innerCard != null));
        }
        
        // Debug.Log("=== CardController Start() Complete ===");
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
            Debug.Log($"🃏 Set cardValueTop: {displayValue}");
        }
        else
        {
            Debug.LogWarning($"❌ cardValueTop is NULL!");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
            cardValueBottom.color = cardColor;
            Debug.Log($"🃏 Set cardValueBottom: {displayValue}");
        }
        else
        {
            Debug.LogWarning($"❌ cardValueBottom is NULL!");
        }
        
        // Set center card value 
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
            cardValueCenter.color = cardColor;
            Debug.Log($"🃏 Set cardValueCenter: {displayValue}");
        }
        else
        {
            Debug.LogWarning($"❌ cardValueCenter is NULL!");
        }
        
        // Keep the main card background white/default
        if (cardBackground != null)
        {
            cardBackground.color = Color.white; // Keep the outer card white
            Debug.Log($"🃏 Set cardBackground to white");
        }
        else
        {
            Debug.LogWarning($"❌ cardBackground is NULL!");
        }
        
        // Set the inner card (black area) to the card color
        if (innerCard != null)
        {
            // For forced 8 card colors, use aggressive reset
            if (value == 8 && forceColorFor8s)
            {
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
                
                // Debug.Log($"Set innerCard: sprite=null, color={cardColor}, type=Simple");
                // Debug.Log($"InnerCard final state: sprite={(innerCard.sprite == null ? "NULL" : innerCard.sprite.name)}, color={innerCard.color}, type={innerCard.type}");
            }
            Debug.Log($"🃏 Set innerCard color: {cardColor}");
        }
        else
        {
            Debug.LogWarning($"❌ innerCard is NULL!");
        }
        
        // CRITICAL FIX: Set outline color to match with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = cardColor;
            Debug.Log($"🃏 Set cardOutline color: {cardColor}");
        }
        else
        {
            Debug.LogWarning($"❌ cardOutline is NULL!");
            
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = cardColor;
                Debug.Log($"🃏 Found outline on retry and set color: {cardColor}");
            }
            else
            {
                Debug.LogError($"❌ Still no outline found after retry!");
            }
        }
        
        Debug.Log($"✅ CARDCONTROLLER SETCARD COMPLETE: {displayValue} of {color}");
    }
    
    // Special method for setting up 8 cards with custom appearance
    void SetEightCard(string displayValue)
    {
        // Debug.Log("=== Setting up special 8 card ===");
        
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
                // Debug.Log("Set 8 card custom sprite");
            }
            else
            {
                // Fallback to black if no sprite assigned
                innerCard.sprite = null;
                innerCard.color = Color.black;
                innerCard.type = Image.Type.Simple;
                innerCard.preserveAspect = false;
                // Debug.LogWarning("No eightCardSprite assigned, using black background");
            }
        }
        
        // CRITICAL FIX: Set outline to black with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.black;
            // Debug.Log("✅ Set 8-card outline to black");
        }
        else
        {
            // Debug.LogWarning("⚠️ cardOutline is NULL for 8-card! Attempting to find outline...");
            
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = Color.black;
                // Debug.Log("✅ Found outline on retry and set to black for 8-card");
            }
            else
            {
                // Debug.LogError("❌ Still no outline found for 8-card after retry!");
            }
        }
        
        // Debug.Log("=== Special 8 card setup complete ===");
    }
    
    // Method to update an 8 card with a chosen color after color selection
    public void SetEightCardWithColor(string chosenColor) // Public method for GameManager
    {
        // Debug.Log($"=== Updating 8 card with chosen color: {chosenColor} ===");
        
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
            // Debug.Log("Set 8 card inner color to: " + cardColor);
        }
        
        // Set outline to match chosen color with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = cardColor;
            // Debug.Log("✅ Set 8-card outline to chosen color: " + cardColor);
        }
        else
        {
            // Debug.LogWarning("⚠️ cardOutline is NULL for 8-card color update! Attempting to find outline...");
            
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = cardColor;
                // Debug.Log("✅ Found outline on retry and set to chosen color: " + cardColor);
            }
            else
            {
                // Debug.LogError("❌ Still no outline found for 8-card color update after retry!");
            }
        }
        
        // Debug.Log($"=== 8 card updated to {chosenColor} color ===");
    }
    
    // Public method to completely reset and force a card appearance - for debugging
    [ContextMenu("Force Reset To Yellow 8")]
    public void ForceResetToYellow8()
    {
        // Debug.Log("=== FORCE RESETTING CARD TO YELLOW 8 ===");
        
        // Force everything to yellow manually
        Color yellowColor = new Color(0.6f, 0.6f, 0.08f);
        
        if (cardValueTop != null)
        {
            cardValueTop.text = "8";
            cardValueTop.color = yellowColor;
            // Debug.Log("FORCED cardValueTop to yellow 8");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = "8";
            cardValueBottom.color = yellowColor;
            // Debug.Log("FORCED cardValueBottom to yellow 8");
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = "8";
            cardValueCenter.color = yellowColor;
            // Debug.Log("FORCED cardValueCenter to yellow 8");
        }
        
        if (cardBackground != null)
        {
            cardBackground.color = Color.white;
            // Debug.Log("FORCED cardBackground to white");
        }
        
        if (innerCard != null)
        {
            // AGGRESSIVE sprite clearing
            ForceResetImageComponent(innerCard, yellowColor);
            // Debug.Log($"FORCED innerCard reset to yellow");
        }
        
        // CRITICAL FIX: Set outline with enhanced reliability
        if (cardOutline != null)
        {
            cardOutline.effectColor = yellowColor;
            // Debug.Log("✅ FORCED outline to yellow");
        }
        else
        {
            // Debug.LogWarning("⚠️ cardOutline is NULL during force reset! Attempting to find outline...");
            
            // Try to find outline again as a fallback
            InitializeComponents();
            
            if (cardOutline != null)
            {
                cardOutline.effectColor = yellowColor;
                // Debug.Log("✅ Found outline on retry and FORCED to yellow");
            }
            else
            {
                // Debug.LogError("❌ Still no outline found during force reset after retry!");
            }
        }
        
        // Debug.Log("=== FORCE RESET COMPLETE ===");
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
        
        // Debug.Log($"ForceResetImageComponent: sprite={(imageComponent.sprite == null ? "NULL" : imageComponent.sprite.name)}, color={imageComponent.color}, enabled={imageComponent.enabled}");
    }
    
    // Diagnostic method to check current card state
    [ContextMenu("Diagnose Current Card State")]
    public void DiagnoseCurrentCardState()
    {
        // Debug.Log("=== CARD STATE DIAGNOSIS ===");
        // Debug.Log($"eightCardSprite assigned: {eightCardSprite != null}");
        if (eightCardSprite != null)
        {
            // Debug.Log($"eightCardSprite name: {eightCardSprite.name}");
        }
        
        if (innerCard != null)
        {
            // Debug.Log($"innerCard.sprite: {(innerCard.sprite == null ? "NULL" : innerCard.sprite.name)}");
            // Debug.Log($"innerCard.color: {innerCard.color}");
            // Debug.Log($"innerCard.type: {innerCard.type}");
            // Debug.Log($"innerCard.enabled: {innerCard.enabled}");
            // Debug.Log($"innerCard.preserveAspect: {innerCard.preserveAspect}");
        }
        else
        {
            // Debug.Log("innerCard is NULL");
        }
        
        // Debug.Log("=== END DIAGNOSIS ===");
    }
    
    // Add test methods for easy testing
    [ContextMenu("Test Red Card")]
    void TestRedCard()
    {
        SetCard("red", 1); // Ace of Red
    }
    
    [ContextMenu("Test Blue Card")]
    void TestBlueCard()
    {
        SetCard("blue", 8); // 8 of Blue (wild card)
    }
    
    [ContextMenu("Test Green Card")]
    void TestGreenCard()
    {
        SetCard("green", 7); // 7 of Green
    }
    
    [ContextMenu("Test Yellow Card")]
    void TestYellowCard()
    {
        SetCard("yellow", 5); // 5 of Yellow
    }
    
    [ContextMenu("Test Special 8 Card")]
    void TestSpecialEightCard()
    {
        SetCard("red", 8); // Test the special 8 card appearance
    }
    
    [ContextMenu("Test 8 Card with Yellow Color")]
    void TestEightCardWithYellow()
    {
        SetCard("yellow", 8, true); // Test 8 card forced to yellow
    }
    
    [ContextMenu("Test 8 Card with Red Color")]
    void TestEightCardWithRed()
    {
        SetCard("red", 8, true); // Test 8 card forced to red
    }
    
    [ContextMenu("Debug Outline Status")]
    void DebugOutlineStatus()
    {
        // Debug.Log("=== DEBUGGING OUTLINE STATUS ===");
        // Debug.Log($"cardOutline assigned: {cardOutline != null}");
        
        if (cardOutline != null)
        {
            // Debug.Log($"Outline enabled: {cardOutline.enabled}");
            // Debug.Log($"Outline effect color: {cardOutline.effectColor}");
            // Debug.Log($"Outline effect distance: {cardOutline.effectDistance}");
            // Debug.Log($"Outline GameObject: {cardOutline.gameObject.name}");
        }
        
        // Debug.Log($"cardBackground assigned: {cardBackground != null}");
        if (cardBackground != null)
        {
            Outline backgroundOutline = cardBackground.GetComponent<Outline>();
            // Debug.Log($"Background has outline: {backgroundOutline != null}");
            if (backgroundOutline != null)
            {
                // Debug.Log($"Background outline color: {backgroundOutline.effectColor}");
            }
        }
        
        // Search for any outline components
        Outline[] allOutlines = GetComponentsInChildren<Outline>(true);
        // Debug.Log($"Total outline components found: {allOutlines.Length}");
        for (int i = 0; i < allOutlines.Length; i++)
        {
            // Debug.Log($"Outline {i}: {allOutlines[i].gameObject.name}, color: {allOutlines[i].effectColor}");
        }
        
        // Debug.Log("=== OUTLINE DEBUG COMPLETE ===");
    }
    
    [ContextMenu("Force Reinitialize Components")]
    void ForceReinitializeComponents()
    {
        // Debug.Log("=== FORCING COMPONENT REINITIALIZATION ===");
        InitializeComponents();
        DebugOutlineStatus();
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
    
    public void ShowCardBack()
    {
        // Hide all text and reset inner card, show card back
        if (cardValueTop != null) cardValueTop.text = "";
        if (cardValueBottom != null) cardValueBottom.text = "";
        if (cardValueCenter != null) cardValueCenter.text = "";
        if (innerCard != null) innerCard.color = Color.black; // Reset to black
        
        // Set card back appearance
        if (cardBackground != null)
        {
            cardBackground.color = new Color(0.2f, 0.3f, 0.8f); // Blue card back
        }
        
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.blue;
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
            
            Debug.Log("🃏 Reset innerCard to white with full opacity");
        }
        
        // Reset outline to default
        if (cardOutline != null)
        {
            cardOutline.effectColor = Color.black;
            cardOutline.effectDistance = new Vector2(2, 2);
        }
        
        // Ensure the card is visible
        gameObject.SetActive(true);
        
        Debug.Log("✅ CARD CONTROLLER RESET COMPLETE");
    }
}
