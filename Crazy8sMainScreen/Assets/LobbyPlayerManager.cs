using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages player card positioning and display in the lobby screen
/// Uses domino-4 layout for clean player arrangement
/// </summary>
public class LobbyPlayerManager : MonoBehaviour
{
    [Header("Lobby Player Setup")]
    public Transform playersContainer;
    public GameObject playerCardPrefab;
    
    [Header("Horizontal Layout Settings")]
    public float playerSpacing = 250f; // Space between players horizontally
    
    [Header("Animation Settings")]
    public float popInDuration = 0.3f;
    public float staggerDelay = 0.2f;
    
    private List<GameObject> activePlayerCards = new List<GameObject>();
    private HashSet<string> existingPlayerNames = new HashSet<string>(); // Track existing players
    
    void Start()
    {
        // Validate references
        if (playersContainer == null)
        {
            Debug.LogError("LobbyPlayerManager: playersContainer not assigned!");
        }
        
        if (playerCardPrefab == null)
        {
            Debug.LogError("LobbyPlayerManager: playerCardPrefab not assigned!");
        }
    }
    
    /// <summary>
    /// Update the lobby player display with horizontal layout
    /// Only animates NEW players, keeps existing ones in place
    /// </summary>
    public void UpdateLobbyPlayers(PlayerData[] players)
    {
        Debug.Log($"LobbyPlayerManager: Updating lobby with {players.Length} players");
        
        if (playersContainer == null || playerCardPrefab == null)
        {
            Debug.LogError("LobbyPlayerManager: Missing required components!");
            return;
        }
        
        // Limit to 4 players max
        int maxPlayers = Mathf.Min(players.Length, 4);
        
        // Find NEW players (ones not currently displayed)
        List<PlayerData> newPlayers = new List<PlayerData>();
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!existingPlayerNames.Contains(players[i].name))
            {
                newPlayers.Add(players[i]);
                existingPlayerNames.Add(players[i].name);
                Debug.Log($"NEW player detected: {players[i].name}");
            }
        }
        
        // Create cards only for NEW players
        for (int i = 0; i < newPlayers.Count; i++)
        {
            PlayerData newPlayer = newPlayers[i];
            int playerIndex = System.Array.FindIndex(players, p => p.name == newPlayer.name);
            CreateLobbyPlayerCard(newPlayer, playerIndex, maxPlayers, true); // true = animate
        }
        
        // Update positions for ALL players (but don't animate existing ones)
        UpdateAllPlayerPositions(players, maxPlayers);
    }
    
    /// <summary>
    /// Create a single player card in the lobby
    /// </summary>
    void CreateLobbyPlayerCard(PlayerData player, int playerIndex, int totalPlayers, bool shouldAnimate = false)
    {
        try
        {
            // Instantiate the card
            GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            playerCard.SetActive(true);
            
            // Disable automatic layout for custom positioning
            LayoutElement layoutElement = playerCard.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = playerCard.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;
            
            // Set player name
            TextMeshProUGUI nameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string playerName = string.IsNullOrEmpty(player.name) ? "Player" : player.name;
                nameText.text = playerName;
                nameText.color = Color.white; // Ensure text is visible
            }
            
            // Set player color - REMOVED: Make background transparent instead of colored
            Image cardImage = playerCard.GetComponent<Image>();
            if (cardImage != null)
            {
                // Make background completely transparent (no colored rectangles)
                cardImage.color = Color.clear;
            }
            
            // Set horizontal position
            RectTransform rectTransform = playerCard.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 position = GetHorizontalPosition(playerIndex, totalPlayers);
                rectTransform.anchoredPosition = position;
                
                Debug.Log($"Player {playerIndex} ({player.name}) positioned at {position}");
            }
            
            // Add to active cards list
            activePlayerCards.Add(playerCard);
            
            // Animate card pop-in ONLY if shouldAnimate is true (for new players)
            if (shouldAnimate)
            {
                StartCoroutine(AnimateCardPopIn(playerCard, 0)); // No stagger delay for single new player
            }
            else
            {
                // Make sure existing players are visible (no animation)
                playerCard.transform.localScale = Vector3.one;
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LobbyPlayerManager: Error creating player card: {e.Message}");
        }
    }
    
    /// <summary>
    /// Get horizontal position for player index
    /// Layout: [Player1] [Player2] [Player3] [Player4]
    /// </summary>
    Vector2 GetHorizontalPosition(int playerIndex, int totalPlayers)
    {
        // Calculate center position for horizontal line
        float totalWidth = (totalPlayers - 1) * playerSpacing;
        float startX = -totalWidth / 2f;
        
        Vector2 position = new Vector2(startX + (playerIndex * playerSpacing), 0);
        
        Debug.Log($"Horizontal position for player {playerIndex}: {position}");
        return position;
    }
    
    /// <summary>
    /// Update positions for all existing players without animation
    /// </summary>
    void UpdateAllPlayerPositions(PlayerData[] players, int maxPlayers)
    {
        // Update positions for all cards to maintain proper spacing
        for (int i = 0; i < activePlayerCards.Count && i < maxPlayers; i++)
        {
            GameObject card = activePlayerCards[i];
            if (card != null)
            {
                RectTransform rectTransform = card.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 newPosition = GetHorizontalPosition(i, maxPlayers);
                    rectTransform.anchoredPosition = newPosition;
                }
            }
        }
    }
    
    /// <summary>
    /// Animate card popping in with bounce effect
    /// </summary>
    IEnumerator AnimateCardPopIn(GameObject card, int playerIndex)
    {
        if (card == null) yield break;
        
        // Start invisible
        card.transform.localScale = Vector3.zero;
        
        // Wait for stagger delay
        yield return new WaitForSeconds(playerIndex * staggerDelay);
        
        // Animate scale from 0 to 1 with bounce
        float elapsedTime = 0f;
        
        while (elapsedTime < popInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / popInDuration;
            
            // Bounce easing (overshoot then settle)
            float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBounce(progress));
            
            if (card != null)
            {
                card.transform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        // Ensure final scale is exactly 1
        if (card != null)
        {
            card.transform.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// Bounce easing function for pop-in animation
    /// </summary>
    float EaseOutBounce(float t)
    {
        if (t < 1 / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2 / 2.75f)
        {
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        }
        else if (t < 2.5 / 2.75f)
        {
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        }
        else
        {
            return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }
    }
    
    /// <summary>
    /// Clear all lobby player cards
    /// </summary>
    public void ClearLobbyCards()
    {
        Debug.Log("LobbyPlayerManager: Clearing lobby cards");
        
        foreach (GameObject card in activePlayerCards)
        {
            if (card != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(card);
                }
                else
                {
                    DestroyImmediate(card);
                }
            }
        }
        
        activePlayerCards.Clear();
        existingPlayerNames.Clear(); // Clear tracking of existing players
        
        // Also clear any remaining children in container
        if (playersContainer != null)
        {
            foreach (Transform child in playersContainer)
            {
                if (child != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Test method - creates test players for horizontal layout
    /// </summary>
    [ContextMenu("Test Horizontal Layout")]
    public void TestHorizontalLayout()
    {
        PlayerData[] testPlayers = {
            new PlayerData { name = "Alice" },
            new PlayerData { name = "Bob" },
            new PlayerData { name = "Charlie" },
            new PlayerData { name = "Diana" }
        };
        
        UpdateLobbyPlayers(testPlayers);
    }
    
    /// <summary>
    /// Test with fewer players
    /// </summary>
    [ContextMenu("Test 2 Players")]
    public void Test2Players()
    {
        PlayerData[] testPlayers = {
            new PlayerData { name = "Alice" },
            new PlayerData { name = "Bob" }
        };
        
        UpdateLobbyPlayers(testPlayers);
    }
}
