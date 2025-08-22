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
    
    [Header("Player Colors")]
    public Color redColor = new Color(0.8f, 0.1f, 0.1f, 1f);   // Red #CC1A1A
    public Color blueColor = new Color(0.1f, 0.1f, 0.8f, 1f);   // Blue #1A1ACC
    public Color greenColor = new Color(0.1f, 0.6f, 0.1f, 1f);   // Green #1A991A
    public Color yellowColor = new Color(0.8f, 0.8f, 0.1f, 1f);  // Yellow
    
    private List<Transform> playerPositions = new List<Transform>();
    private Dictionary<string, GameObject> activePlayerDisplays = new Dictionary<string, GameObject>();
    private Dictionary<string, Color> originalPlayerColors = new Dictionary<string, Color>(); // Store original colors
    private string currentHighlightedPlayer = ""; // Track who is currently highlighted
    private PlayerData[] lastPlayerList; // Track the last player list to detect changes
    
    void Start()
    {
        // Set up the position order (for 2-4 players)
        playerPositions.Add(playerPositionBottom); // Player 1 (host) always bottom
        playerPositions.Add(playerPositionTop);    // Player 2 always top
        playerPositions.Add(playerPositionLeft);   // Player 3 left
        playerPositions.Add(playerPositionRight);  // Player 4 right
    }
    
    /// <summary>
    /// Get the Unity Color for a player's assigned color string
    /// </summary>
    Color GetPlayerColor(string colorName)
    {
        switch (colorName?.ToLower())
        {
            case "red": return redColor;
            case "blue": return blueColor;
            case "green": return greenColor;
            case "yellow": return yellowColor;
            default: return Color.white; // Default color if unassigned
        }
    }
    
    public void UpdatePlayersDisplay(PlayerData[] players)
    {
        // Debug.Log("======= PLAYER POSITION DEBUG START =======");
        // Debug.Log("UpdatePlayersDisplay: Number of players = " + players.Length);
        
        // Check if the player list has actually changed before clearing everything
        bool playerListChanged = HasPlayerListChanged(players);
        
        if (!playerListChanged)
        {
            // Just update card counts for existing players
            UpdateExistingPlayerCardCounts(players);
            return;
        }
        
        // Clear existing displays only when player list actually changed
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
        
        // Update our tracking of the last player list
        lastPlayerList = new PlayerData[players.Length];
        System.Array.Copy(players, lastPlayerList, players.Length);
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
        
        // Set player color on the main Image component (the rounded rectangle background)
        try
        {
            UnityEngine.UI.Image mainImage = playerDisplay.GetComponent<UnityEngine.UI.Image>();
            if (mainImage != null)
            {
                Color playerColor = GetPlayerColor(player.color);
                mainImage.color = playerColor;
                
                // Store the original color for later use in highlighting
                originalPlayerColors[player.name] = playerColor;
                
                Debug.Log($"✓ Set player {player.name} color to: {player.color} ({playerColor}) on main Image component");
            }
            else
            {
                Debug.LogWarning($"No main Image component found on PlayerDisplay for player {player.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error setting player color: " + e.Message);
        }
        
        // Check if Outline components exist and configure the FIRST one for turn highlighting
        try
        {
            UnityEngine.UI.Outline[] outlines = playerDisplay.GetComponents<UnityEngine.UI.Outline>();
            if (outlines != null && outlines.Length > 0)
            {
                // Use the FIRST outline component for turn-based highlighting
                UnityEngine.UI.Outline firstOutline = outlines[0];
                
                // Set initial outline to inactive (disabled)
                firstOutline.enabled = false;
                Debug.Log($"✓ Found {outlines.Length} Outline components for player {player.name}");
                Debug.Log($"✓ Configured FIRST Outline component for turn highlighting (initially disabled)");
                
                // Log info about all outline components for debugging
                for (int i = 0; i < outlines.Length; i++)
                {
                    Debug.Log($"  Outline {i}: enabled={outlines[i].enabled}, effectColor={outlines[i].effectColor}");
                }
            }
            else
            {
                Debug.LogWarning($"❌ NO OUTLINE COMPONENTS found on PlayerDisplay for player {player.name} - highlighting won't work!");
                Debug.LogWarning("Please add Outline components to your PlayerDisplay prefab in the Inspector.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error checking Outline components: " + e.Message);
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
        Debug.Log("ClearAllPlayerDisplays: Clearing all player displays and resetting highlighting");
        Debug.Log($"ClearAllPlayerDisplays: Resetting currentHighlightedPlayer from '{currentHighlightedPlayer}' to empty");
        foreach (var display in activePlayerDisplays.Values)
        {
            if (display != null)
                Destroy(display);
        }
        activePlayerDisplays.Clear();
        originalPlayerColors.Clear(); // Also clear the color tracking
        currentHighlightedPlayer = ""; // Reset highlighted player
        lastPlayerList = null; // Reset the player list tracking
        Debug.Log("ClearAllPlayerDisplays: Completed - all tracking reset");
    }
    
    // Public method to clear displays when switching screens
    public void ClearPlayerDisplays()
    {
        ClearAllPlayerDisplays();
    }
    
    public void HighlightCurrentPlayer(string playerName)
    {
        // Normalize the player name for comparison (trim whitespace, handle null)
        string normalizedPlayerName = string.IsNullOrEmpty(playerName) ? "" : playerName.Trim();
        string normalizedCurrentPlayer = string.IsNullOrEmpty(currentHighlightedPlayer) ? "" : currentHighlightedPlayer.Trim();
        
        // If the same player is already highlighted, don't do anything
        if (normalizedCurrentPlayer == normalizedPlayerName)
        {
            return;
        }
    
        // Reset all players: disable FIRST outline only (keep second outline always enabled)
        foreach (var kvp in activePlayerDisplays)
        {
            var display = kvp.Value;
            var image = display.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Outline[] outlines = display.GetComponents<UnityEngine.UI.Outline>();
            
            if (image != null && originalPlayerColors.ContainsKey(kvp.Key))
            {
                // Restore to original background color
                image.color = originalPlayerColors[kvp.Key];
            }
            
            if (outlines != null && outlines.Length > 0)
            {
                // Disable FIRST outline (turn indicator) - keep second outline enabled
                outlines[0].enabled = false;
                
                // Keep second outline enabled if it exists
                if (outlines.Length > 1)
                {
                    outlines[1].enabled = true;
                }
            }
        }
        
        // Highlight current player: enable BOTH outlines
        Debug.Log($"🎯 Looking for player '{normalizedPlayerName}' in activePlayerDisplays...");
        if (activePlayerDisplays.ContainsKey(normalizedPlayerName))
        {
            var display = activePlayerDisplays[normalizedPlayerName];
            UnityEngine.UI.Outline[] outlines = display.GetComponents<UnityEngine.UI.Outline>();
            
            Debug.Log($"🎯 Found player display for '{normalizedPlayerName}': Outlines={outlines?.Length ?? 0}");
            
            if (outlines != null && outlines.Length > 0)
            {
                // Enable FIRST outline (turn indicator)
                outlines[0].enabled = true;
                Debug.Log($"✅ Enabled FIRST outline for current player {normalizedPlayerName} (active turn)");
                
                // Enable SECOND outline too (if it exists)
                if (outlines.Length > 1)
                {
                    outlines[1].enabled = true;
                    Debug.Log($"✅ Enabled SECOND outline for current player {normalizedPlayerName}");
                }
                
                // Log all outline states for debugging
                for (int i = 0; i < outlines.Length; i++)
                {
                    Debug.Log($"🔍 Outline {i} for {normalizedPlayerName}: enabled={outlines[i].enabled}, effectColor={outlines[i].effectColor}");
                }
            }
            else
            {
                Debug.LogError($"❌ Outline components not found on player display for: {normalizedPlayerName}");
            }
        }
        else
        {
            Debug.LogWarning($"❌ Player not found in active displays: {normalizedPlayerName}");
            Debug.Log("Available players: " + string.Join(", ", activePlayerDisplays.Keys));
        }    // Update the currently highlighted player
    currentHighlightedPlayer = normalizedPlayerName;
    
    Debug.Log($"HighlightCurrentPlayer: Successfully updated highlighting for '{normalizedPlayerName}'");
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
    
    /// <summary>
    /// Check if the player list has actually changed (different players or different order)
    /// </summary>
    private bool HasPlayerListChanged(PlayerData[] newPlayers)
    {
        // If this is the first time or we have no previous list, it's changed
        if (lastPlayerList == null || lastPlayerList.Length != newPlayers.Length)
        {
            return true;
        }
        
        // Check if any player names or order changed
        for (int i = 0; i < newPlayers.Length; i++)
        {
            if (lastPlayerList[i].name != newPlayers[i].name)
            {
                return true;
            }
        }
        
        return false; // No changes detected
    }
    
    /// <summary>
    /// Update card counts for existing players without rebuilding the entire display
    /// </summary>
    private void UpdateExistingPlayerCardCounts(PlayerData[] players)
    {
        foreach (var player in players)
        {
            UpdatePlayerCardCount(player.name, player.cardCount);
        }
        
        // Update our tracking of the last player list
        lastPlayerList = new PlayerData[players.Length];
        System.Array.Copy(players, lastPlayerList, players.Length);
    }
    
    /// <summary>
    /// Get all currently active player display GameObjects for winner animation
    /// </summary>
    public List<GameObject> GetActivePlayerDisplays()
    {
        return new List<GameObject>(activePlayerDisplays.Values);
    }
    
    /// <summary>
    /// Find a specific player display GameObject by player name
    /// </summary>
    public GameObject FindPlayerDisplay(string playerName)
    {
        if (activePlayerDisplays.ContainsKey(playerName))
        {
            return activePlayerDisplays[playerName];
        }
        return null;
    }
    
    /// <summary>
    /// Get all currently active player display GameObjects (alias for GetActivePlayerDisplays)
    /// </summary>
    public List<GameObject> GetAllPlayerDisplays()
    {
        return GetActivePlayerDisplays();
    }
}
