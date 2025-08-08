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
    public Color redColor = new Color(0.8f, 0.1f, 0.1f);
    public Color blueColor = new Color(0.1f, 0.1f, 0.8f);
    public Color greenColor = new Color(0.1f, 0.6f, 0.1f);
    public Color yellowColor = new Color(0.8f, 0.8f, 0.1f);
    
    void Start()
    {
        Debug.Log("=== CardController Start() ===");
        
        // Auto-assign components if not set
        if (cardBackground == null) 
        {
            cardBackground = GetComponent<Image>();
            Debug.Log("Card background found: " + (cardBackground != null));
        }
        
        // Get outline from the same GameObject as the card background
        if (cardOutline == null && cardBackground != null) 
        {
            cardOutline = cardBackground.GetComponent<Outline>();
            Debug.Log("Card outline found: " + (cardOutline != null));
        }
        
        // Find child components automatically if not assigned
        Debug.Log("Looking for child components...");
        Debug.Log("Child count: " + transform.childCount);
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log("Child " + i + ": " + child.name);
        }
        
        if (cardValueTop == null) 
        {
            cardValueTop = transform.Find("CardValueTop")?.GetComponent<TextMeshProUGUI>();
            Debug.Log("CardValueTop found: " + (cardValueTop != null));
        }
        
        if (cardValueBottom == null) 
        {
            cardValueBottom = transform.Find("CardValueBottom")?.GetComponent<TextMeshProUGUI>();
            Debug.Log("CardValueBottom found: " + (cardValueBottom != null));
        }
        
        if (cardValueCenter == null) 
        {
            cardValueCenter = transform.Find("CardValueCenter")?.GetComponent<TextMeshProUGUI>();
            Debug.Log("CardValueCenter found: " + (cardValueCenter != null));
        }
        
        if (innerCard == null) 
        {
            innerCard = transform.Find("InnerCard")?.GetComponent<Image>();
            Debug.Log("InnerCard found: " + (innerCard != null));
        }
        
        Debug.Log("=== CardController Start() Complete ===");
    }
    
    public void SetCard(string color, int value)
    {
        Debug.Log($"=== SetCard called: {GetCardDisplayValue(value)} of {color} ===");
        
        // Get display value and color
        string displayValue = GetCardDisplayValue(value);
        Color cardColor = GetCardColor(color);
        
        Debug.Log($"Display value: {displayValue}, Card Color: {cardColor}");
        
        // Set all text elements to the card color (they will show on the white circle)
        if (cardValueTop != null)
        {
            cardValueTop.text = displayValue;
            cardValueTop.color = cardColor;
            Debug.Log("Set cardValueTop: " + displayValue + " with color: " + cardColor);
        }
        else
        {
            Debug.LogWarning("cardValueTop is NULL!");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
            cardValueBottom.color = cardColor;
            Debug.Log("Set cardValueBottom: " + displayValue + " with color: " + cardColor);
        }
        else
        {
            Debug.LogWarning("cardValueBottom is NULL!");
        }
        
        // Set center card value 
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
            cardValueCenter.color = cardColor;
            Debug.Log("Set cardValueCenter: " + displayValue + " with color: " + cardColor);
        }
        else
        {
            Debug.LogWarning("cardValueCenter is NULL!");
        }
        
        // Keep the main card background white/default
        if (cardBackground != null)
        {
            cardBackground.color = Color.white; // Keep the outer card white
            Debug.Log("Set cardBackground to white");
        }
        
        // Set the inner card (black area) to the card color
        if (innerCard != null)
        {
            innerCard.color = cardColor;
            Debug.Log("Set innerCard color: " + cardColor);
        }
        else
        {
            Debug.LogWarning("innerCard is NULL!");
        }
        
        // Set outline color to match
        if (cardOutline != null)
        {
            cardOutline.effectColor = cardColor;
            Debug.Log("Set outline color: " + cardColor);
        }
        else
        {
            Debug.LogWarning("cardOutline is NULL!");
        }
        
        Debug.Log($"=== SetCard complete ===");
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
            case 1: return "A";
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
}