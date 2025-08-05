using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Card data class
[System.Serializable]
public class CardData
{
    public string suit;
    public int value;
    public string rank; // Alternative to value for face cards
    
    public CardData(string suit, int value)
    {
        this.suit = suit;
        this.value = value;
    }
    
    public CardData(string suit, string rank)
    {
        this.suit = suit;
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
    public Image cardSuitTop;
    public Image cardSuitBottom;
    public Outline cardOutline;
    public Image cardBackground;
    
    [Header("Suit Colors")]
    public Color redSuitColor = new Color(0.8f, 0.1f, 0.1f);
    public Color blackSuitColor = new Color(0.1f, 0.1f, 0.1f);
    
    [Header("Suit Sprites (from PNG)")]
    public Sprite heartsSprite;     // Assign Anglo-American_card_suits.svg_2
    public Sprite diamondsSprite;   // Assign Anglo-American_card_suits.svg_1
    public Sprite clubsSprite;      // Assign Anglo-American_card_suits.svg_0
    public Sprite spadesSprite;     // Assign Anglo-American_card_suits.svg_3
    
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
        
        if (cardSuitTop == null) 
        {
            cardSuitTop = transform.Find("CardSuitTop")?.GetComponent<Image>();
            Debug.Log("CardSuitTop found: " + (cardSuitTop != null));
        }
        
        if (cardSuitBottom == null) 
        {
            cardSuitBottom = transform.Find("CardSuitBottom")?.GetComponent<Image>();
            Debug.Log("CardSuitBottom found: " + (cardSuitBottom != null));
        }
        
        Debug.Log("=== CardController Start() Complete ===");
    }
    
    public void SetCard(string suit, int value)
    {
        Debug.Log($"=== SetCard called: {GetCardDisplayValue(value)} of {suit} ===");
        
        // Get display value and colors
        string displayValue = GetCardDisplayValue(value);
        Color suitColor = GetSuitColor(suit);
        Sprite suitSprite = GetSuitSprite(suit);
        
        Debug.Log($"Display value: {displayValue}, Color: {suitColor}, Sprite: {(suitSprite != null ? suitSprite.name : "NULL")}");
        
        // Set card values
        if (cardValueTop != null)
        {
            cardValueTop.text = displayValue;
            cardValueTop.color = suitColor;
            Debug.Log("Set cardValueTop: " + displayValue + " with color: " + suitColor);
        }
        else
        {
            Debug.LogWarning("cardValueTop is NULL!");
        }
        
        if (cardValueBottom != null)
        {
            cardValueBottom.text = displayValue;
            cardValueBottom.color = suitColor;
            Debug.Log("Set cardValueBottom: " + displayValue + " with color: " + suitColor);
        }
        else
        {
            Debug.LogWarning("cardValueBottom is NULL!");
        }
        
        // Set center card value and make it larger/more prominent
        if (cardValueCenter != null)
        {
            cardValueCenter.text = displayValue;
            cardValueCenter.color = suitColor;
            Debug.Log("Set cardValueCenter: " + displayValue + " with color: " + suitColor);
        }
        else
        {
            Debug.LogWarning("cardValueCenter is NULL!");
        }
        
        // Set suit sprites
        if (cardSuitTop != null && suitSprite != null)
        {
            cardSuitTop.sprite = suitSprite;
            cardSuitTop.color = suitColor;
            Debug.Log("Set cardSuitTop sprite with color: " + suitColor);
        }
        else
        {
            Debug.LogWarning($"cardSuitTop: {(cardSuitTop != null ? "Found" : "NULL")}, suitSprite: {(suitSprite != null ? "Found" : "NULL")}");
        }
        
        if (cardSuitBottom != null && suitSprite != null)
        {
            cardSuitBottom.sprite = suitSprite;
            cardSuitBottom.color = suitColor;
            Debug.Log("Set cardSuitBottom sprite with color: " + suitColor);
        }
        else
        {
            Debug.LogWarning($"cardSuitBottom: {(cardSuitBottom != null ? "Found" : "NULL")}, suitSprite: {(suitSprite != null ? "Found" : "NULL")}");
        }
        
        // Set outline color
        if (cardOutline != null)
        {
            cardOutline.effectColor = suitColor;
            Debug.Log("Set outline color: " + suitColor);
        }
        else
        {
            Debug.LogWarning("cardOutline is NULL!");
        }
        
        Debug.Log($"=== SetCard complete ===");
    }
    
    // Add test methods for easy testing
    [ContextMenu("Test Hearts Card")]
    void TestHeartsCard()
    {
        SetCard("hearts", 1); // Ace of Hearts
    }
    
    [ContextMenu("Test Diamonds Card")]
    void TestDiamondsCard()
    {
        SetCard("diamonds", 13); // King of Diamonds
    }
    
    [ContextMenu("Test Clubs Card")]
    void TestClubsCard()
    {
        SetCard("clubs", 7); // 7 of Clubs
    }
    
    [ContextMenu("Test Spades Card")]
    void TestSpadesCard()
    {
        SetCard("spades", 11); // Jack of Spades
    }
    
    public void SetCard(CardData cardData)
    {
        if (cardData != null)
        {
            SetCard(cardData.suit, cardData.value);
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
    
    // Get suit color (red for hearts/diamonds, black for clubs/spades)
    Color GetSuitColor(string suit)
    {
        switch (suit.ToLower())
        {
            case "hearts":
            case "diamonds": 
                return redSuitColor;
            case "clubs":
            case "spades": 
                return blackSuitColor;
            default: 
                return blackSuitColor;
        }
    }
    
    // Get suit sprite from your SVG sprites
    Sprite GetSuitSprite(string suit)
    {
        switch (suit.ToLower())
        {
            case "hearts": return heartsSprite;
            case "diamonds": return diamondsSprite;
            case "clubs": return clubsSprite;
            case "spades": return spadesSprite;
            default: return null;
        }
    }
    
    public void ShowCardBack()
    {
        // Hide all text and suits, show card back
        if (cardValueTop != null) cardValueTop.text = "";
        if (cardValueBottom != null) cardValueBottom.text = "";
        if (cardValueCenter != null) cardValueCenter.text = "";
        if (cardSuitTop != null) cardSuitTop.sprite = null;
        if (cardSuitBottom != null) cardSuitBottom.sprite = null;
        
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