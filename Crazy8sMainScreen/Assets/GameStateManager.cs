using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages overall game state including current player, current color, deck count,
/// and coordinates state updates between other managers
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Game State")]
    private string currentPlayer = "";
    private string currentColor = "";
    private int deckCount = 0;
    private string currentTopCard = "";
    
    private bool isFlipAnimationInProgress = false;
    
    // Manager references
    private UIManager uiManager;
    private CardAnimationManager cardAnimationManager;
    private PlayerManager playerManager;
    
    // Events
    public event System.Action<string> OnCurrentPlayerChanged;
    public event System.Action<string> OnCurrentColorChanged;
    public event System.Action<int> OnDeckCountChanged;
    public event System.Action<string> OnTopCardChanged;
    
    public void Initialize()
    {
        // Auto-find required managers
        uiManager = FindFirstObjectByType<UIManager>();
        cardAnimationManager = FindFirstObjectByType<CardAnimationManager>();
        playerManager = FindFirstObjectByType<PlayerManager>();
        
        ResetGameState();
    }
    
    public void UpdateFromJson(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString)) return;
        
        Debug.Log($"🎯 GameStateManager UpdateFromJson called with: {jsonString}");
        
        try
        {
            // Check if this is a winner announcement first
            string winnerName = ExtractJsonValue(jsonString, "winner");
            if (!string.IsNullOrEmpty(winnerName))
            {
                Debug.Log($"🏆 Winner detected: {winnerName}");
                HandleWinnerAnnouncement(winnerName, jsonString);
                return; // Winner handling takes precedence over normal game state updates
            }
            
            // Extract game state from JSON
            string gameStateJson = ExtractGameStateJson(jsonString);
            if (!string.IsNullOrEmpty(gameStateJson))
            {
                jsonString = gameStateJson;
            }
            
            // Extract individual state components
            string newCurrentPlayer = ExtractJsonValue(jsonString, "currentPlayer");
            string newCurrentColor = ExtractJsonValue(jsonString, "currentColor");
            string newTopCard = ExtractCurrentCardFromJson(jsonString);
            
            string deckCountStr = ExtractJsonValue(jsonString, "deckCount");
            int newDeckCount = 0;
            int.TryParse(deckCountStr, out newDeckCount);
            
            Debug.Log($"🎯 Extracted - Player: {newCurrentPlayer}, Color: {newCurrentColor}, Card: {newTopCard}, Deck: {newDeckCount}");
            
            // Check if this is the first game state update (game starting)
            bool isGameStarting = string.IsNullOrEmpty(currentTopCard) && !string.IsNullOrEmpty(newTopCard);
            
            // Update state and UI
            UpdateCurrentPlayer(newCurrentPlayer);
            UpdateCurrentColor(newCurrentColor);
            UpdateDeckCount(newDeckCount);
            UpdateTopCard(newTopCard);
            
            // If this is the game start, trigger the card flip animation
            if (isGameStarting && cardAnimationManager != null)
            {
                Debug.Log($"🎯 Game starting detected! Triggering card flip animation for: {newTopCard}");
                cardAnimationManager.StartGameWithCardFlip();
            }
            
            // Update players if included in state
            var players = ExtractPlayersFromGameState(jsonString);
            if (players != null && players.Count > 0 && playerManager != null)
            {
                playerManager.UpdatePlayers(players.ToArray());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating game state from JSON: {e.Message}");
        }
    }
    
    public void UpdateCurrentPlayer(string playerName)
    {
        if (currentPlayer != playerName && !string.IsNullOrEmpty(playerName))
        {
            currentPlayer = playerName;
            
            if (uiManager != null)
            {
                uiManager.UpdateCurrentPlayer(playerName);
            }
            
            if (playerManager != null)
            {
                playerManager.HighlightCurrentPlayer(playerName);
            }
            
            OnCurrentPlayerChanged?.Invoke(playerName);
        }
    }
    
    public void UpdateCurrentColor(string color)
    {
        if (currentColor != color && !string.IsNullOrEmpty(color))
        {
            currentColor = color;
            
            if (uiManager != null)
            {
                uiManager.UpdateCurrentColor(color);
                uiManager.ChangeBackgroundToCardColor(color);
            }
            
            OnCurrentColorChanged?.Invoke(color);
        }
    }
    
    public void UpdateDeckCount(int count)
    {
        if (deckCount != count)
        {
            deckCount = count;
            
            if (uiManager != null)
            {
                uiManager.UpdateDeckCount(count);
            }
            
            OnDeckCountChanged?.Invoke(count);
        }
    }
    
    public void UpdateTopCard(string cardValue)
    {
        Debug.Log($"🃏 UpdateTopCard called with: '{cardValue}' (current: '{currentTopCard}')");
        
        if (currentTopCard != cardValue && !string.IsNullOrEmpty(cardValue))
        {
            currentTopCard = cardValue;
            
            Debug.Log($"🃏 Updating top card to: {cardValue}");
            
            if (cardAnimationManager != null)
            {
                Debug.Log($"🃏 Calling CardAnimationManager.UpdateTopCard with: {cardValue}");
                cardAnimationManager.UpdateTopCard(cardValue);
            }
            else
            {
                Debug.LogError("🃏 CardAnimationManager is null!");
            }
            
            OnTopCardChanged?.Invoke(cardValue);
        }
        else
        {
            Debug.Log($"🃏 UpdateTopCard skipped - same card or empty value");
        }
    }
    
    public void HandleCardPlayed(string cardPlayedJson)
    {
        if (string.IsNullOrEmpty(cardPlayedJson)) return;
        
        // Extract card information
        string playedCard = ExtractPlayedCardFromJson(cardPlayedJson);
        if (!string.IsNullOrEmpty(playedCard))
        {
            UpdateTopCard(playedCard);
            
            // Extract color from the card
            string cardColor = ExtractCardColor(playedCard);
            if (!string.IsNullOrEmpty(cardColor))
            {
                UpdateCurrentColor(cardColor);
            }
        }
        
        // Update full game state if included
        UpdateFromJson(cardPlayedJson);
    }
    
    public void HandleColorChosen(string colorChosenJson)
    {
        if (cardAnimationManager != null)
        {
            cardAnimationManager.HandleColorChosen(colorChosenJson);
        }
        
        // Extract the chosen color and update current color
        string chosenColor = ExtractJsonValue(colorChosenJson, "color");
        if (!string.IsNullOrEmpty(chosenColor))
        {
            UpdateCurrentColor(chosenColor);
        }
    }
    
    public void ResetGameState()
    {
        currentPlayer = "";
        currentColor = "";
        deckCount = 0;
        currentTopCard = "";
        isFlipAnimationInProgress = false;
        
        if (uiManager != null)
        {
            uiManager.ResetUI();
        }
        
        if (cardAnimationManager != null)
        {
            cardAnimationManager.ResetAnimations();
        }
    }
    
    // Public getters for other managers
    public string GetCurrentPlayer() => currentPlayer;
    public string GetCurrentColor() => currentColor;
    public int GetDeckCount() => deckCount;
    public string GetCurrentTopCard() => currentTopCard;
    
    public bool IsFlipAnimationInProgress() => isFlipAnimationInProgress;
    public void SetFlipAnimationInProgress(bool value) => isFlipAnimationInProgress = value;
    
    // JSON parsing utilities
    private string ExtractGameStateJson(string json)
    {
        int gameStateStart = json.IndexOf("\"gameState\":");
        if (gameStateStart == -1) return "";
        
        int objectStart = json.IndexOf('{', gameStateStart + 12);
        if (objectStart == -1) return "";
        
        int braceCount = 1;
        int objectEnd = objectStart + 1;
        
        while (objectEnd < json.Length && braceCount > 0)
        {
            if (json[objectEnd] == '{') braceCount++;
            else if (json[objectEnd] == '}') braceCount--;
            objectEnd++;
        }
        
        if (braceCount == 0)
        {
            return json.Substring(objectStart, objectEnd - objectStart);
        }
        
        return "";
    }
    
    private string ExtractCurrentCardFromJson(string json)
    {
        // Try multiple possible keys for the current card
        string[] possibleKeys = { "currentCard", "topCard", "lastPlayedCard" };
        
        foreach (string key in possibleKeys)
        {
            // Extract the card object
            int keyIndex = json.IndexOf("\"" + key + "\":");
            if (keyIndex == -1) continue;
            
            int valueStart = json.IndexOf('{', keyIndex);
            if (valueStart == -1) continue;
            
            int braceCount = 1;
            int valueEnd = valueStart + 1;
            
            // Find the matching closing brace
            while (valueEnd < json.Length && braceCount > 0)
            {
                if (json[valueEnd] == '{') braceCount++;
                else if (json[valueEnd] == '}') braceCount--;
                valueEnd++;
            }
            
            if (braceCount == 0)
            {
                string cardJson = json.Substring(valueStart, valueEnd - valueStart);
                Debug.Log($"🎯 Extracted card JSON: {cardJson}");
                
                // Parse the card JSON to get color and rank
                string color = ExtractJsonValue(cardJson, "color");
                string rank = ExtractJsonValue(cardJson, "rank");
                
                if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(rank))
                {
                    string cardString = $"{rank} of {color}";
                    Debug.Log($"🎯 Converted to card string: {cardString}");
                    return cardString;
                }
            }
        }
        
        return "";
    }
    
    private List<PlayerData> ExtractPlayersFromGameState(string json)
    {
        List<PlayerData> players = new List<PlayerData>();
        
        int playersStart = json.IndexOf("\"players\":");
        if (playersStart == -1) return players;
        
        int arrayStart = json.IndexOf('[', playersStart);
        if (arrayStart == -1) return players;
        
        int arrayEnd = json.IndexOf(']', arrayStart);
        if (arrayEnd == -1) return players;
        
        string playersJson = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        string[] playerObjects = playersJson.Split(new string[] { "},{" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string playerObj in playerObjects)
        {
            string cleanObj = playerObj.Trim('{', '}', ' ');
            
            PlayerData player = new PlayerData();
            player.name = ExtractJsonValue("{" + cleanObj + "}", "name");
            
            string cardCountStr = ExtractJsonValue("{" + cleanObj + "}", "cardCount");
            int.TryParse(cardCountStr, out player.cardCount);
            
            string isFirstStr = ExtractJsonValue("{" + cleanObj + "}", "isFirstPlayer");
            bool.TryParse(isFirstStr, out player.isFirstPlayer);
            
            player.color = ExtractJsonValue("{" + cleanObj + "}", "color");
            
            if (!string.IsNullOrEmpty(player.name))
            {
                players.Add(player);
            }
        }
        
        return players;
    }
    
    private string ExtractPlayedCardFromJson(string json)
    {
        // Try different possible keys for played cards
        string[] possibleKeys = { "card", "playedCard", "lastCard" };
        
        foreach (string key in possibleKeys)
        {
            // Extract the card object
            int keyIndex = json.IndexOf("\"" + key + "\":");
            if (keyIndex == -1) continue;
            
            int valueStart = json.IndexOf('{', keyIndex);
            if (valueStart == -1) continue;
            
            int braceCount = 1;
            int valueEnd = valueStart + 1;
            
            // Find the matching closing brace
            while (valueEnd < json.Length && braceCount > 0)
            {
                if (json[valueEnd] == '{') braceCount++;
                else if (json[valueEnd] == '}') braceCount--;
                valueEnd++;
            }
            
            if (braceCount == 0)
            {
                string cardJson = json.Substring(valueStart, valueEnd - valueStart);
                Debug.Log($"🃏 Extracted played card JSON: {cardJson}");
                
                // Parse the card JSON to get color and rank
                string color = ExtractJsonValue(cardJson, "color");
                string rank = ExtractJsonValue(cardJson, "rank");
                
                if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(rank))
                {
                    string cardString = $"{rank} of {color}";
                    Debug.Log($"🃏 Converted played card to string: {cardString}");
                    return cardString;
                }
            }
        }
        
        return "";
    }
    
    private string ExtractCardColor(string cardString)
    {
        if (string.IsNullOrEmpty(cardString)) return "";
        
        string[] colors = { "red", "blue", "green", "yellow" };
        string lowerCard = cardString.ToLower();
        
        foreach (string color in colors)
        {
            if (lowerCard.Contains(color))
            {
                return color;
            }
        }
        
        return "";
    }
    
    private string ExtractJsonValue(string json, string key)
    {
        string searchKey = "\"" + key + "\":";
        int keyIndex = json.IndexOf(searchKey);
        
        if (keyIndex == -1) return "";
        
        int valueStart = keyIndex + searchKey.Length;
        
        // Skip whitespace
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
        {
            valueStart++;
        }
        
        if (valueStart >= json.Length) return "";
        
        // Handle string values
        if (json[valueStart] == '"')
        {
            valueStart++;
            int valueEnd = json.IndexOf('"', valueStart);
            if (valueEnd == -1) return "";
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
        // Handle numeric/boolean values
        else
        {
            int valueEnd = valueStart;
            while (valueEnd < json.Length && 
                   json[valueEnd] != ',' && 
                   json[valueEnd] != '}' && 
                   json[valueEnd] != ']')
            {
                valueEnd++;
            }
            
            return json.Substring(valueStart, valueEnd - valueStart).Trim();
        }
    }
    
    /// <summary>
    /// Handles winner announcement from server
    /// </summary>
    private void HandleWinnerAnnouncement(string winnerName, string jsonData)
    {
        Debug.Log($"🏆 Handling winner announcement for: {winnerName}");
        
        // Update final player states from winner JSON
        var players = ExtractPlayersFromGameState(jsonData);
        if (players != null && players.Count > 0 && playerManager != null)
        {
            playerManager.UpdatePlayers(players.ToArray());
        }
        
        // Notify GameManager to trigger winner sequence
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"🏆 Triggering winner sequence through GameManager");
            // Use reflection to call OnGameOver since it's private
            var method = gameManager.GetType().GetMethod("OnGameOver", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(gameManager, new object[] { jsonData });
            }
            else
            {
                Debug.LogWarning("⚠️ OnGameOver method not found in GameManager");
            }
        }
        else
        {
            Debug.LogError("❌ GameManager not found - cannot trigger winner sequence");
        }
    }
}
