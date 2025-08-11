using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Simple card data class
[System.Serializable]
public class CardData
{
    public string color;
    public int value;
    
    public CardData(string color, int value)
    {
        this.color = color;
        this.value = value;
    }
}

/// <summary>
/// Simplified CardController for Crazy 8s - handles basic card display and 8-card animations
/// </summary>
public class CardController : MonoBehaviour
{
    [Header("Card Components")]
    public TextMeshProUGUI cardValueTop;
    public TextMeshProUGUI cardValueBottom;
    public TextMeshProUGUI cardValueCenter;
    public Image innerCard;
    public Image cardBackground;
    
    [Header("Color Settings")]
    public Color redColor = new Color(0.8f, 0.1f, 0.1f);
    public Color blueColor = new Color(0.1f, 0.1f, 0.8f);
    public Color greenColor = new Color(0.1f, 0.6f, 0.1f);
    public Color yellowColor = new Color(0.9f, 0.9f, 0.1f);
    public Color grayColor = Color.gray;
    
    [Header("8 Card Sprite")]
    public Sprite eightCardSprite; // The spiral/eight image for 8 cards
    
    void Start()
    {
        Debug.Log("Simple CardController initialized");
        
        // Try to auto-find 8 card sprite if not assigned
        if (eightCardSprite == null)
        {
            eightCardSprite = Resources.Load<Sprite>("SpiralColor");
        }
    }
    
    /// <summary>
    /// Simple method to set card appearance
    /// </summary>
    public void SetCard(string color, int value, bool forceRefresh = false)
    {
        Debug.Log($"SetCard: {color} {value}");
        
        // Set color
        SetCardColor(color);
        
        // Set value display
        string displayValue = GetCardDisplayValue(value);
        SetCardTexts(displayValue);
        
        // Set 8 sprite for 8 cards
        if (value == 8)
        {
            SetEightCardSprite();
        }
        
        if (forceRefresh)
        {
            ForceUIRefresh();
        }
    }
    
    /// <summary>
    /// Get the display value for a card (1 stays as 1, not A)
    /// </summary>
    private string GetCardDisplayValue(int value)
    {
        switch (value)
        {
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            default: return value.ToString(); // 1 stays as "1", not "A"
        }
    }
    
    /// <summary>
    /// Set all card text elements
    /// </summary>
    private void SetCardTexts(string displayValue)
    {
        if (cardValueTop != null)
        {
            cardValueTop.text = displayValue;
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
        }
        
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
        }
    }
    
    /// <summary>
    /// Set card color
    /// </summary>
    private void SetCardColor(string colorName)
    {
        Color color = GetColorFromName(colorName);
        
        // Set text colors
        if (cardValueTop != null) cardValueTop.color = color;
        if (cardValueBottom != null) cardValueBottom.color = color;
        if (cardValueCenter != null) cardValueCenter.color = color;
        
        // Set inner card color (for background)
        if (innerCard != null)
        {
            innerCard.color = color;
        }
    }
    
    /// <summary>
    /// Get Color from string name
    /// </summary>
    private Color GetColorFromName(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red": return redColor;
            case "blue": return blueColor;
            case "green": return greenColor;
            case "yellow": return yellowColor;
            case "gray": return grayColor;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Set the 8 card sprite (spiral image)
    /// </summary>
    private void SetEightCardSprite()
    {
        if (eightCardSprite != null && cardBackground != null)
        {
            cardBackground.sprite = eightCardSprite;
        }
    }
    
    /// <summary>
    /// Force UI refresh for immediate visual updates
    /// </summary>
    private void ForceUIRefresh()
    {
        Canvas.ForceUpdateCanvases();
        
        if (cardValueTop != null) cardValueTop.SetAllDirty();
        if (cardValueBottom != null) cardValueBottom.SetAllDirty();
        if (cardValueCenter != null) cardValueCenter.SetAllDirty();
        if (innerCard != null) innerCard.SetAllDirty();
        if (cardBackground != null) cardBackground.SetAllDirty();
    }
    
    /// <summary>
    /// Debug method to check current card state
    /// </summary>
    public void DiagnoseCurrentCardState()
    {
        Debug.Log("=== CARD STATE DIAGNOSIS ===");
        if (cardValueCenter != null)
        {
            Debug.Log($"Center text: '{cardValueCenter.text}' Color: {cardValueCenter.color}");
        }
        if (innerCard != null)
        {
            Debug.Log($"Inner card color: {innerCard.color}");
        }
        Debug.Log("=== END DIAGNOSIS ===");
    }
}
