using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerPositionManager : MonoBehaviour 
{
    [Header("Player Position Containers")]
    public Transform playerPositionTop;
    public Transform playerPositionBottom;
    public Transform playerPositionLeft;
    public Transform playerPositionRight;
    
    [Header("Player UI Prefab")]
    public GameObject playerDisplayPrefab; // Simple name + card count
    
    private List<Transform> playerPositions = new List<Transform>();
    private Dictionary<string, GameObject> activePlayerDisplays = new Dictionary<string, GameObject>();
    
    void Start()
    {
        // Set up the position order (for 2-4 players)
        playerPositions.Add(playerPositionBottom); // Player 1 (host) always bottom
        playerPositions.Add(playerPositionTop);    // Player 2 always top
        playerPositions.Add(playerPositionLeft);   // Player 3 left
        playerPositions.Add(playerPositionRight);  // Player 4 right
    }
    
    public void UpdatePlayersDisplay(PlayerData[] players)
    {
        // Limit to max 4 players
        int maxPlayers = Mathf.Min(players.Length, 4);
        
        // Clear existing displays
        ClearAllPlayerDisplays();
        
        // Create new displays for each player
        for (int i = 0; i < maxPlayers; i++)
        {
            if (i < playerPositions.Count)
            {
                CreatePlayerDisplay(players[i], playerPositions[i]);
            }
        }
    }
    
    void CreatePlayerDisplay(PlayerData player, Transform position)
    {
        Debug.Log("=== Creating Player Display ===");
        Debug.Log("Player: " + player.name + ", Position: " + position.name);
        Debug.Log("Prefab: " + (playerDisplayPrefab != null ? "Found" : "NULL"));
        
        if (playerDisplayPrefab == null)
        {
            Debug.LogError("PlayerDisplayPrefab is null! Please assign it in the Inspector.");
            return;
        }
        
        GameObject playerDisplay = Instantiate(playerDisplayPrefab, position);
        Debug.Log("Player display instantiated: " + playerDisplay.name);
        
        // Find and set the text components
        TextMeshProUGUI nameText = playerDisplay.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI cardCountText = playerDisplay.transform.Find("CardCount").GetComponent<TextMeshProUGUI>();
        
        if (nameText != null)
        {
            nameText.text = player.name;
            Debug.Log("Set player name: " + player.name);
        }
        else
        {
            Debug.LogError("PlayerName text component not found!");
        }
        
        if (cardCountText != null)
        {
            cardCountText.text = player.cardCount.ToString() + " cards";
            Debug.Log("Set card count: " + player.cardCount);
        }
        else
        {
            Debug.LogError("CardCount text component not found!");
        }
        
        activePlayerDisplays[player.name] = playerDisplay;
        Debug.Log("Player display created successfully for: " + player.name);
    }
    
    void ClearAllPlayerDisplays()
    {
        foreach (var display in activePlayerDisplays.Values)
        {
            if (display != null)
                Destroy(display);
        }
        activePlayerDisplays.Clear();
    }
    
    public void HighlightCurrentPlayer(string playerName)
    {
        // Reset all players to normal
        foreach (var display in activePlayerDisplays.Values)
        {
            var image = display.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.white;
            }
        }
        
        // Highlight current player
        if (activePlayerDisplays.ContainsKey(playerName))
        {
            var image = activePlayerDisplays[playerName].GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.yellow;
            }
        }
    }
    
    public void UpdatePlayerCardCount(string playerName, int cardCount)
    {
        if (activePlayerDisplays.ContainsKey(playerName))
        {
            GameObject playerDisplay = activePlayerDisplays[playerName];
            TextMeshProUGUI cardCountText = playerDisplay.transform.Find("CardCount").GetComponent<TextMeshProUGUI>();
            
            if (cardCountText != null)
            {
                cardCountText.text = cardCount.ToString() + " cards";
            }
        }
    }
}