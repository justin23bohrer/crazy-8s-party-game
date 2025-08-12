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
        // Debug.Log("======= PLAYER POSITION DEBUG START =======");
        // Debug.Log("UpdatePlayersDisplay: Number of players = " + players.Length);
        
        // Check position containers
        // Debug.Log("Position containers check:");
        // Debug.Log("- playerPositionTop: " + (playerPositionTop != null ? "FOUND" : "NULL"));
        // Debug.Log("- playerPositionBottom: " + (playerPositionBottom != null ? "FOUND" : "NULL"));
        // Debug.Log("- playerPositionLeft: " + (playerPositionLeft != null ? "FOUND" : "NULL"));
        // Debug.Log("- playerPositionRight: " + (playerPositionRight != null ? "FOUND" : "NULL"));
        // Debug.Log("- playerDisplayPrefab: " + (playerDisplayPrefab != null ? "FOUND" : "NULL"));
        
        // Clear existing displays
        // Debug.Log("Clearing existing player displays...");
        ClearAllPlayerDisplays();
        
        // Handle different player counts with proper positioning
        switch (players.Length)
        {
            case 2:
                // 2 players: left and right of the card
                // Debug.Log("Creating 2-player layout: left and right");
                
                // Debug.Log("About to create first player display...");
                try 
                {
                    CreatePlayerDisplay(players[0], playerPositionLeft);
                    // Debug.Log("✓ First player display created successfully");
                }
                catch (System.Exception)
                {
                    // Debug.LogError("Error creating first player display: " + e.Message);
                }
                
                // Debug.Log("About to create second player display...");
                try 
                {
                    CreatePlayerDisplay(players[1], playerPositionRight);
                    // Debug.Log("✓ Second player display created successfully");
                }
                catch (System.Exception)
                {
                    // Debug.LogError("Error creating second player display: " + e.Message);
                }
                break;
                
            case 3:
                // 3 players: left, right, and top
                // Debug.Log("Creating 3-player layout: left, right, and top");
                CreatePlayerDisplay(players[0], playerPositionLeft);
                CreatePlayerDisplay(players[1], playerPositionRight);
                CreatePlayerDisplay(players[2], playerPositionTop);
                break;
                
            case 4:
                // 4 players: all positions (top, bottom, left, right)
                // Debug.Log("Creating 4-player layout: top, bottom, left, and right");
                CreatePlayerDisplay(players[0], playerPositionBottom);
                CreatePlayerDisplay(players[1], playerPositionTop);
                CreatePlayerDisplay(players[2], playerPositionLeft);
                CreatePlayerDisplay(players[3], playerPositionRight);
                break;
                
            default:
                // Debug.LogError("Unsupported number of players: " + players.Length);
                break;
        }
        
        // Debug.Log("Final activePlayerDisplays count: " + activePlayerDisplays.Count);
        // Debug.Log("======= PLAYER POSITION DEBUG END =======");
    }
    
    void CreatePlayerDisplay(PlayerData player, Transform position)
    {
        // Debug.Log("======= CREATE PLAYER DISPLAY =======");
        // Debug.Log("Creating display for player: " + player.name);
        // Debug.Log("Target position: " + (position != null ? position.name : "NULL"));
        // Debug.Log("Prefab available: " + (playerDisplayPrefab != null));
        
        if (playerDisplayPrefab == null)
        {
            // Debug.LogError("CRITICAL: PlayerDisplayPrefab is null! Cannot create player display.");
            return;
        }
        
        if (position == null)
        {
            // Debug.LogError("CRITICAL: Position transform is null for player: " + player.name);
            return;
        }
        
        // Instantiate the prefab
        // Debug.Log("Instantiating prefab...");
        GameObject playerDisplay = Instantiate(playerDisplayPrefab);
        // Debug.Log("Prefab instantiated successfully: " + playerDisplay.name);
        
        // Find the main Canvas and set as parent for Screen Space - Overlay
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            playerDisplay.transform.SetParent(mainCanvas.transform, false);
            // Debug.Log("Set parent to main Canvas: " + mainCanvas.name);
            
            // Check position immediately after setting parent
            RectTransform tempRect = playerDisplay.GetComponent<RectTransform>();
            if (tempRect != null)
            {
                // Debug.Log("Position immediately after SetParent: " + tempRect.anchoredPosition);
            }
        }
        else
        {
            // Debug.LogError("No main Canvas found for Screen Space positioning!");
            return;
        }
        
        // Make sure it's active
        playerDisplay.SetActive(true);
        // Debug.Log("Player display set to active");
        
        // Get UI position based on the position transform name
        Vector2 uiPosition = GetUIPositionFromTransform(position);
        
        // Debug.Log("About to check for RectTransform component...");
        
        // Ensure the GameObject has a RectTransform component
        RectTransform rectTransform = playerDisplay.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            // Debug.Log("RectTransform not found - adding one now...");
            rectTransform = playerDisplay.AddComponent<RectTransform>();
            // Debug.Log("✓ RectTransform component added successfully");
        }
        else
        {
            // Debug.Log("RectTransform component already exists");
        }
        
        // Debug.Log("=== RECTRANSFORM POSITIONING START ===");
        // Debug.Log("RectTransform found on: " + playerDisplay.name);
        // Debug.Log("Current anchored position BEFORE: " + rectTransform.anchoredPosition);
        // Debug.Log("Target UI position: " + uiPosition);
        // Debug.Log("Current anchors: min=" + rectTransform.anchorMin + ", max=" + rectTransform.anchorMax);
        
        // Set anchors to top-left
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        
        // Set position (now relative to top-left)
        rectTransform.anchoredPosition = uiPosition;
        
        // Debug.Log("✓ POSITION SET - anchored position AFTER: " + rectTransform.anchoredPosition);
        // Debug.Log("✓ World position: " + rectTransform.position);
        // Debug.Log("✓ Local position: " + rectTransform.localPosition);
        // Debug.Log("=== RECTRANSFORM POSITIONING END ===");
        
        // Configure Canvas for proper rendering
        Canvas canvas = playerDisplay.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            // Debug.Log("Configuring Canvas for visibility...");
            try 
            {
                // CRITICAL FIX: The prefab has scale (0,0,0) which makes it invisible!
                canvas.transform.localScale = Vector3.one;
                // Debug.Log("Fixed Canvas scale to (1,1,1) - was probably (0,0,0)");
                
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                Canvas.ForceUpdateCanvases();
                // Debug.Log("Canvas configured: overrideSorting=true, sortingOrder=100");
            }
            catch (System.Exception)
            {
                // Debug.LogError("Error configuring Canvas: " + e.Message);
            }
        }
        else
        {
            // Debug.LogWarning("No Canvas component found - this is normal for the new simple prefab");
        }
        
        // Check position in hierarchy
        try 
        {
            // Debug.Log("Player display parent: " + playerDisplay.transform.parent.name);
            // Debug.Log("Player display UI position: " + rectTransform.anchoredPosition);
            // Debug.Log("Player display local position: " + playerDisplay.transform.localPosition);
        }
        catch (System.Exception)
        {
            // Debug.LogError("Error checking position hierarchy: " + e.Message);
        }
        
        // Find and set the text components
        try
        {
            // Debug.Log("Starting text component setup...");
            
            // Look for direct children PlayerName and CardCount (no Canvas wrapper)
            Transform nameTransform = playerDisplay.transform.Find("PlayerName");
            Transform cardCountTransform = playerDisplay.transform.Find("CardCount");
            
            // Debug.Log("PlayerName child found: " + (nameTransform != null));
            // Debug.Log("CardCount child found: " + (cardCountTransform != null));
            
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = player.name;
                    // Debug.Log("SUCCESS: Set player name to: " + player.name);
                }
                else
                {
                    // Debug.LogError("PlayerName has no TextMeshProUGUI component!");
                }
            }
            else
            {
                // Debug.LogError("PlayerName child not found in prefab!");
            }
            
            if (cardCountTransform != null)
            {
                TextMeshProUGUI cardCountText = cardCountTransform.GetComponent<TextMeshProUGUI>();
                if (cardCountText != null)
                {
                    cardCountText.text = player.cardCount.ToString() + " cards";
                    // Debug.Log("SUCCESS: Set card count to: " + player.cardCount + " cards");
                }
                else
                {
                    // Debug.LogError("CardCount has no TextMeshProUGUI component!");
                }
            }
            else
            {
                // Debug.LogError("CardCount child not found in prefab!");
            }
            
            // Debug.Log("Text component setup completed");
        }
        catch (System.Exception)
        {
            // Debug.LogError("Error during text component setup: " + e.Message);
        }
        
        // Add to tracking dictionary BEFORE any Canvas operations
        try
        {
            activePlayerDisplays[player.name] = playerDisplay;
            // Debug.Log("Player display added to dictionary. Total displays: " + activePlayerDisplays.Count);
        }
        catch (System.Exception)
        {
            // Debug.LogError("Error adding to dictionary: " + e.Message);
        }
        
        // Force UI refresh for immediate visibility
        try
        {
            Canvas.ForceUpdateCanvases();
            // Debug.Log("Forced Canvas refresh for immediate visibility");
        }
        catch (System.Exception)
        {
            // Debug.LogError("Error during Canvas refresh: " + e.Message);
        }
        
        // Final status check
        try
        {
            // Debug.Log("FINAL STATUS CHECK:");
            // Debug.Log("- Player display active: " + playerDisplay.activeInHierarchy);
            // Debug.Log("- Player display position: " + rectTransform.anchoredPosition);
            // Debug.Log("- Player display size: " + rectTransform.sizeDelta);
            // Debug.Log("- Parent Canvas: " + playerDisplay.transform.parent.name);
            // Debug.Log("- Dictionary contains player: " + activePlayerDisplays.ContainsKey(player.name));
        }
        catch (System.Exception)
        {
            // Debug.LogError("Error during final status check: " + e.Message);
        }
        
        // Debug.Log("======= CREATE PLAYER DISPLAY END =======");
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
    
    // Public method to clear displays when switching screens
    public void ClearPlayerDisplays()
    {
        ClearAllPlayerDisplays();
    }
    
    public void HighlightCurrentPlayer(string playerName)
    {
        // Debug.Log("=== HighlightCurrentPlayer called ===");
        // Debug.Log("Highlighting player: " + playerName);
        // Debug.Log("Active player displays count: " + activePlayerDisplays.Count);
        
        // Reset all players to normal (white background)
        foreach (var kvp in activePlayerDisplays)
        {
            var display = kvp.Value;
            var image = display.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.white;
                // Debug.Log("Reset player " + kvp.Key + " to normal color");
            }
        }
        
        // Highlight current player (yellow background)
        if (activePlayerDisplays.ContainsKey(playerName))
        {
            var image = activePlayerDisplays[playerName].GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.yellow;
                // Debug.Log("Highlighted current player: " + playerName + " with yellow color");
            }
            else
            {
                // Debug.LogError("Image component not found on player display for: " + playerName);
            }
        }
        else
        {
            // Debug.LogWarning("Player not found in active displays: " + playerName);
            // Debug.Log("Available players: " + string.Join(", ", activePlayerDisplays.Keys));
        }
    }
    
    public void UpdatePlayerCardCount(string playerName, int cardCount)
    {
        if (activePlayerDisplays.ContainsKey(playerName))
        {
            GameObject playerDisplay = activePlayerDisplays[playerName];
            
            // Look for CardCount as direct child (no Canvas wrapper)
            Transform cardCountTransform = playerDisplay.transform.Find("CardCount");
            
            if (cardCountTransform != null)
            {
                TextMeshProUGUI cardCountText = cardCountTransform.GetComponent<TextMeshProUGUI>();
                if (cardCountText != null)
                {
                    cardCountText.text = cardCount.ToString() + " cards";
                    // Debug.Log("Updated card count for " + playerName + " to " + cardCount + " cards");
                }
            }
        }
    }
    
    // Helper method to convert Transform position to UI coordinates
    private Vector2 GetUIPositionFromTransform(Transform targetTransform)
    {
        // Use simple, predictable positioning based on transform names
        Vector2 uiPosition = Vector2.zero;
        
        // Debug.Log($"Target transform world position: {targetTransform.position}");
        
        // Use exact coordinates specified by user
        if (targetTransform.name.Contains("Left"))
        {
            uiPosition = new Vector2(400, -500); // Left position
        }
        else if (targetTransform.name.Contains("Right"))
        {
            uiPosition = new Vector2(1400, -500); // Right position
        }
        else if (targetTransform.name.Contains("Top"))
        {
            uiPosition = new Vector2(850, -100); // Top position
        }
        else if (targetTransform.name.Contains("Bottom"))
        {
            uiPosition = new Vector2(850, -850); // Bottom position
        }
        
        // Debug.Log($"Using simple UI position for {targetTransform.name}: {uiPosition}");
        return uiPosition;
    }
}
