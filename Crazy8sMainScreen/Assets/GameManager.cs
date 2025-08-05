using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocketIOClient;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject lobbyScreen;
    public GameObject gameScreen;
    public GameObject gameOverScreen;
    
    [Header("Lobby UI")]
    public TextMeshProUGUI roomCodeText;
    public Button createRoomButton;
    public Button startGameButton;
    public Transform playersContainer;
    public GameObject playerCardPrefab;
    
    [Header("Game UI")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI currentSuitText;
    public TextMeshProUGUI deckCountText;
    public Image topCardImage;
    public PlayerPositionManager playerPositionManager;
    
    [Header("Game Over UI")]
    public TextMeshProUGUI winnerText;
    public Button playAgainButton;
    
    [Header("Connection")]
    public string serverURL = "http://localhost:3000";
    
    private SocketIOUnity socket;
    private string roomCode;
    private List<PlayerData> players = new List<PlayerData>();
    
    // Queue for main thread actions
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    void Start()
    {
        // Check if all references are assigned
        if (!ValidateReferences())
        {
            Debug.LogError("Some UI references are missing! Please assign them in the Inspector.");
            return;
        }
        
        // Clear any existing player cards from previous sessions
        ClearPlayersList();
        
        // Set initial room code text
        roomCodeText.text = "----";
        
        // Disable start game button initially (needs 2+ players)
        startGameButton.interactable = false;
        
        // Set up button events
        createRoomButton.onClick.AddListener(CreateRoom);
        startGameButton.onClick.AddListener(StartGame);
        playAgainButton.onClick.AddListener(RestartGame);
        
        // Show lobby screen initially
        ShowScreen("lobby");
        
        // Initialize socket connection
        ConnectToServer();
    }
    
    void OnDestroy()
    {
        // Clean up socket connection when the game stops
        if (socket != null)
        {
            try
            {
                socket.Disconnect();
                socket.Dispose();
            }
            catch (Exception e)
            {
                Debug.Log("Error disconnecting socket: " + e.Message);
            }
        }
    }
    
    void Update()
    {
        // Process main thread actions
        lock (mainThreadActions)
        {
            if (mainThreadActions.Count > 0)
            {
                Debug.Log("Processing " + mainThreadActions.Count + " main thread actions");
            }
            
            while (mainThreadActions.Count > 0)
            {
                try
                {
                    System.Action action = mainThreadActions.Dequeue();
                    if (action != null)
                    {
                        Debug.Log("Executing main thread action");
                        action.Invoke();
                        Debug.Log("Main thread action completed");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error executing main thread action: " + e.Message);
                    Debug.LogError("Stack trace: " + e.StackTrace);
                }
            }
        }
    }
    
    private void EnqueueMainThreadAction(System.Action action)
    {
        if (action != null)
        {
            lock (mainThreadActions)
            {
                mainThreadActions.Enqueue(action);
            }
        }
    }
    
    bool ValidateReferences()
    {
        bool valid = true;
        
        if (lobbyScreen == null) { Debug.LogError("LobbyScreen is not assigned!"); valid = false; }
        if (gameScreen == null) { Debug.LogError("GameScreen is not assigned!"); valid = false; }
        if (gameOverScreen == null) { Debug.LogError("GameOverScreen is not assigned!"); valid = false; }
        if (roomCodeText == null) { Debug.LogError("RoomCodeText is not assigned!"); valid = false; }
        if (createRoomButton == null) { Debug.LogError("CreateRoomButton is not assigned!"); valid = false; }
        if (startGameButton == null) { Debug.LogError("StartGameButton is not assigned!"); valid = false; }
        if (playersContainer == null) { Debug.LogError("PlayersContainer is not assigned!"); valid = false; }
        if (playerCardPrefab == null) { Debug.LogError("PlayerCardPrefab is not assigned!"); valid = false; }
        
        return valid;
    }
    
    void ConnectToServer()
    {
        try
        {
            Debug.Log("Attempting to connect to: " + serverURL);
            var uri = new Uri(serverURL);
            socket = new SocketIOUnity(uri);
            
            // Socket events - using correct signatures
            socket.OnConnected += OnSocketConnected;
            socket.OnDisconnected += OnSocketDisconnected;
            
            // Room events - using Action<SocketIOResponse> for socket events
            socket.On("room-created", new Action<SocketIOResponse>(HandleRoomCreated));
            socket.On("player-joined", new Action<SocketIOResponse>(HandlePlayerJoined));
            socket.On("game-started", new Action<SocketIOResponse>(HandleGameStarted));
            socket.On("game-over", new Action<SocketIOResponse>(HandleGameOver));
            socket.On("game-ended", new Action<SocketIOResponse>(HandleGameEnded));
            socket.On("room-error", new Action<SocketIOResponse>(HandleRoomError));
            
            // Game events - these update the main screen during gameplay
            socket.On("card-played", new Action<SocketIOResponse>(HandleCardPlayed));
            socket.On("card-drawn", new Action<SocketIOResponse>(HandleCardDrawn));
            socket.On("game-state-updated", new Action<SocketIOResponse>(HandleGameStateUpdated));
            socket.On("suit-chosen", new Action<SocketIOResponse>(HandleSuitChosen));
            
            socket.Connect();
            Debug.Log("Socket connection initiated...");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
        }
    }
    
    void OnSocketConnected(object sender, EventArgs e)
    {
        Debug.Log("Connected to server successfully!");
    }
    
    void OnSocketDisconnected(object sender, string e)
    {
        Debug.Log("Disconnected from server: " + e);
    }
    
    void HandleGameStarted(SocketIOResponse response)
    {
        Debug.Log("Game started event received");
        
        try
        {
            // Parse the game state from the response
            string jsonString = response.GetValue().ToString();
            Debug.Log("Game started JSON: " + jsonString);
            
            EnqueueMainThreadAction(delegate() {
                ShowScreen("game");
                
                // Initialize game UI
                if (currentPlayerText != null) currentPlayerText.text = "Waiting for game state...";
                if (currentSuitText != null) currentSuitText.text = "Current Suit: --";
                if (deckCountText != null) deckCountText.text = "Cards Left: --";
                
                Debug.Log("Switched to game screen and initialized UI");
            });
            
            // Try to extract and update the initial game state
            UpdateGameStateFromResponse(jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing game started response: " + e.Message);
            
            // Fallback - just switch to game screen
            EnqueueMainThreadAction(delegate() {
                ShowScreen("game");
                
                // Initialize game UI
                if (currentPlayerText != null) currentPlayerText.text = "Waiting for game state...";
                if (currentSuitText != null) currentSuitText.text = "Current Suit: --";
                if (deckCountText != null) deckCountText.text = "Cards Left: --";
                
                Debug.Log("Switched to game screen and initialized UI (fallback)");
            });
        }
    }
    
    void CreateRoom()
    {
        Debug.Log("Create Room button clicked");
        
        if (socket != null && socket.Connected)
        {
            Debug.Log("Sending create-room event to server");
            socket.Emit("create-room");
            
            // Update UI to show we're creating
            roomCodeText.text = "Creating room...";
            createRoomButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("Socket not connected, using test room");
            // For testing without server
            roomCode = "DISC";
            roomCodeText.text = "Room Code: " + roomCode;
        }
    }
    
    void StartGame()
    {
        Debug.Log("Start Game clicked - Players: " + players.Count + ", Room: " + roomCode);
        
        if (socket != null && socket.Connected && roomCode != null && roomCode != "")
        {
            if (players.Count >= 2 && players.Count <= 4)
            {
                Debug.Log("Starting game with " + players.Count + " players");
                // Send just the room code as data - no callback needed
                socket.Emit("start-game", new { roomCode = roomCode });
            }
            else if (players.Count > 4)
            {
                Debug.LogWarning("Cannot start game - too many players (" + players.Count + "/4 max)");
                string currentRoom = roomCode;
                EnqueueMainThreadAction(delegate() {
                    roomCodeText.text = currentRoom + " - Max 4 players";
                });
            }
            else
            {
                Debug.LogWarning("Cannot start game - only " + players.Count + " players (need 2+)");
                string currentRoom = roomCode;
                EnqueueMainThreadAction(delegate() {
                    roomCodeText.text = currentRoom + " - Need 2+ players";
                });
            }
        }
        else
        {
            Debug.LogWarning("Socket not connected or no room code");
        }
    }
    
    void RestartGame()
    {
        ShowScreen("lobby");
        roomCode = "";
        players.Clear();
        ClearPlayersList();
        roomCodeText.text = "----";
        startGameButton.interactable = false;
        
        // Show the create room button again
        if (createRoomButton != null)
        {
            createRoomButton.gameObject.SetActive(true);
            createRoomButton.interactable = true;
            Debug.Log("Create Room button shown and enabled for restart");
        }
    }
    
    void ClearPlayersList()
    {
        if (playersContainer != null)
        {
            // Clear any existing player cards
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
            Debug.Log("Cleared all player cards from container");
        }
    }
    
    void HandleRoomCreated(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Processing room-created response");
            
            // Get the first value as string directly
            try
            {
                string jsonString = response.GetValue().ToString();
                Debug.Log("JSON string: " + jsonString);
                
                // Try to extract room code using simple string parsing
                string extractedRoomCode = ExtractRoomCodeFromJson(jsonString);
                
                if (extractedRoomCode != null && extractedRoomCode != "")
                {
                    Debug.Log("Extracted room code: " + extractedRoomCode);
                    
                    string newRoomCode = extractedRoomCode;
                    EnqueueMainThreadAction(delegate() {
                        roomCode = newRoomCode;
                        roomCodeText.text = roomCode;
                        createRoomButton.interactable = true;
                        
                        // Hide the create room button after room is created
                        createRoomButton.gameObject.SetActive(false);
                        Debug.Log("Create Room button hidden - room already created");
                        
                        Debug.Log("UI updated with room code: " + roomCode);
                    });
                }
                else
                {
                    Debug.LogError("Could not extract room code from response");
                    EnqueueMainThreadAction(delegate() {
                        roomCodeText.text = "Error creating room. Try again.";
                        createRoomButton.interactable = true;
                    });
                }
            }
            catch (Exception parseEx)
            {
                Debug.LogError("Error getting value from response: " + parseEx.Message);
                EnqueueMainThreadAction(delegate() {
                    roomCodeText.text = "Error creating room. Try again.";
                    createRoomButton.interactable = true;
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing room created data: " + e.Message);
            Debug.LogError("Stack trace: " + e.StackTrace);
            
            EnqueueMainThreadAction(delegate() {
                roomCodeText.text = "Error creating room. Try again.";
                createRoomButton.interactable = true;
            });
        }
    }
    
    void HandlePlayerJoined(SocketIOResponse response)
    {
        try 
        {
            Debug.Log("=== PLAYER JOINED EVENT ===");
            
            // Get the raw JSON string
            string jsonString = response.GetValue().ToString();
            Debug.Log("Raw JSON: " + jsonString);
            
            // Extract players array more simply
            var players = ExtractPlayersSimple(jsonString);
            
            if (players != null && players.Count > 0)
            {
                Debug.Log("Found " + players.Count + " players");
                
                PlayerData[] playersArray = players.ToArray();
                Debug.Log("About to enqueue main thread action for UpdatePlayersList");
                
                EnqueueMainThreadAction(() => {
                    Debug.Log("Main thread action executing - calling UpdatePlayersList");
                    UpdatePlayersList(playersArray);
                    Debug.Log("UpdatePlayersList completed - starting delayed refresh");
                    StartCoroutine(RefreshUIDelayed());
                });
                
                Debug.Log("Main thread action enqueued successfully");
            }
            else
            {
                Debug.LogError("No players found in response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in HandlePlayerJoined: " + e.Message);
        }
    }
    
    void HandleGameOver(SocketIOResponse response)
    {
        try
        {
            try
            {
                string jsonString = response.GetValue().ToString();
                string winner = ExtractWinnerFromJson(jsonString);
                
                if (winner != null && winner != "")
                {
                    string winnerName = winner;
                    EnqueueMainThreadAction(delegate() {
                        if (winnerText != null) winnerText.text = winnerName + " Wins!";
                        ShowScreen("game-over");
                    });
                }
            }
            catch (Exception parseEx)
            {
                Debug.LogError("Error getting value from game over response: " + parseEx.Message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing game over data: " + e.Message);
        }
    }
    
    void HandleRoomError(SocketIOResponse response)
    {
        try
        {
            var error = response.GetValue<string>();
            Debug.LogError("Room error: " + error);
            
            EnqueueMainThreadAction(delegate() {
                roomCodeText.text = "Error creating room. Try again.";
                createRoomButton.interactable = true;
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing room error: " + e.Message);
        }
    }
    
    void HandleGameEnded(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Game ended event received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Game ended JSON: " + jsonString);
            
            string winner = ExtractJsonValue(jsonString, "winner");
            
            if (winner != null && winner != "")
            {
                string winnerName = winner;
                EnqueueMainThreadAction(delegate() {
                    if (winnerText != null) winnerText.text = winnerName + " Wins!";
                    ShowScreen("game-over");
                    Debug.Log("Game ended - winner: " + winnerName);
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing game ended data: " + e.Message);
        }
    }
    
    void HandleCardPlayed(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Card played event received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Card played JSON: " + jsonString);
            
            // Extract game state and update UI
            UpdateGameStateFromResponse(jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling card played: " + e.Message);
        }
    }
    
    void HandleCardDrawn(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Card drawn event received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Card drawn JSON: " + jsonString);
            
            // Extract game state and update UI
            UpdateGameStateFromResponse(jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling card drawn: " + e.Message);
        }
    }
    
    void HandleGameStateUpdated(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Game state updated event received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Game state JSON: " + jsonString);
            
            // This is the main screen game state update
            UpdateGameStateFromResponse(jsonString);
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling game state update: " + e.Message);
        }
    }
    
    void HandleSuitChosen(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Suit chosen event received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Suit chosen JSON: " + jsonString);
            
            string suit = ExtractJsonValue(jsonString, "suit");
            string playerName = ExtractJsonValue(jsonString, "playerName");
            
            EnqueueMainThreadAction(delegate() {
                if (currentSuitText != null && suit != null && suit != "")
                {
                    currentSuitText.text = "Current Suit: " + suit;
                    Debug.Log(playerName + " chose suit: " + suit);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling suit chosen: " + e.Message);
        }
    }
    
    void UpdateGameStateFromResponse(string jsonString)
    {
        try
        {
            Debug.Log("=== UpdateGameStateFromResponse ===");
            Debug.Log("Raw JSON: " + jsonString);
            
            // Check if this is wrapped in a "gameState" object
            string gameStateJson = ExtractGameStateJson(jsonString);
            if (gameStateJson != "")
            {
                Debug.Log("Extracted gameState JSON: " + gameStateJson);
                jsonString = gameStateJson; // Use the inner gameState
            }
            
            // Extract current player - backend sends "currentPlayer", not "currentPlayerName"
            string currentPlayer = ExtractJsonValue(jsonString, "currentPlayer");
            
            // Extract current card info
            string currentCard = ExtractCurrentCardFromJson(jsonString);
            string currentSuit = ExtractJsonValue(jsonString, "currentSuit");
            
            // Extract deck count
            string deckCountStr = ExtractJsonValue(jsonString, "deckCount");
            int deckCount = 0;
            int.TryParse(deckCountStr, out deckCount);
            
            // Extract players and update
            var players = ExtractPlayersFromGameState(jsonString);
            
            Debug.Log("Parsed values - Player: '" + currentPlayer + "', Suit: '" + currentSuit + "', Deck: " + deckCount + ", PlayersCount: " + (players != null ? players.Count : 0));
            
            EnqueueMainThreadAction(delegate() {
                // Update current player
                if (currentPlayerText != null && currentPlayer != null && currentPlayer != "")
                {
                    currentPlayerText.text = "Current Player: " + currentPlayer;
                    Debug.Log("Updated current player text to: " + currentPlayer);
                    
                    // Highlight current player in the position manager
                    if (playerPositionManager != null)
                    {
                        playerPositionManager.HighlightCurrentPlayer(currentPlayer);
                    }
                }
                else
                {
                    Debug.LogWarning("Current player is empty or null: '" + currentPlayer + "'");
                }
                
                // Update current suit
                if (currentSuitText != null && currentSuit != null && currentSuit != "")
                {
                    currentSuitText.text = "Current Suit: " + currentSuit;
                    Debug.Log("Updated current suit text to: " + currentSuit);
                }
                
                // Update top card display
                if (topCardImage != null && currentCard != null && currentCard != "")
                {
                    // Call the UpdateTopCard method to properly update the card display
                    UpdateTopCard(currentCard);
                    topCardImage.gameObject.SetActive(true);
                    Debug.Log("Updated top card to: " + currentCard);
                }
                
                // Update deck count
                if (deckCountText != null)
                {
                    deckCountText.text = "Cards Left: " + deckCount;
                    Debug.Log("Updated deck count text to: " + deckCount);
                }
                
                // Update players list if we have players
                if (players != null && players.Count > 0)
                {
                    UpdatePlayersList(players.ToArray());
                    Debug.Log("Updated players list with " + players.Count + " players");
                }
                
                Debug.Log("Game state updated - Player: " + currentPlayer + ", Suit: " + currentSuit + ", Deck: " + deckCount);
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error updating game state from response: " + e.Message);
            Debug.LogError("Stack trace: " + e.StackTrace);
        }
    }
    
    string ExtractGameStateJson(string json)
    {
        try
        {
            // Look for "gameState":{...} pattern
            string pattern = "\"gameState\":{";
            int startIdx = json.IndexOf(pattern);
            if (startIdx == -1) 
            {
                Debug.Log("No gameState wrapper found, using JSON as-is");
                return ""; // No gameState wrapper
            }
            
            startIdx += pattern.Length - 1; // Include the opening brace
            
            // Find the matching closing brace
            int braceCount = 1;
            int endIdx = startIdx + 1;
            
            while (endIdx < json.Length && braceCount > 0)
            {
                if (json[endIdx] == '{') braceCount++;
                else if (json[endIdx] == '}') braceCount--;
                endIdx++;
            }
            
            if (braceCount == 0)
            {
                string gameStateJson = json.Substring(startIdx, endIdx - startIdx);
                Debug.Log("Extracted gameState object: " + gameStateJson);
                return gameStateJson;
            }
            else
            {
                Debug.LogError("Malformed gameState JSON - unmatched braces");
                return "";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting gameState JSON: " + e.Message);
            return "";
        }
    }
    
    string ExtractCurrentCardFromJson(string json)
    {
        try
        {
            // Look for "topCard" object and extract rank and suit
            string topCardPattern = "\"topCard\":{";
            int startIdx = json.IndexOf(topCardPattern);
            if (startIdx == -1) return "";
            
            int endIdx = json.IndexOf("}", startIdx);
            if (endIdx == -1) return "";
            
            string topCardJson = json.Substring(startIdx, endIdx - startIdx + 1);
            
            string rank = ExtractJsonValue(topCardJson, "rank");
            string suit = ExtractJsonValue(topCardJson, "suit");
            
            if (rank != "" && suit != "")
            {
                return rank + " of " + suit;
            }
            
            return "";
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting current card: " + e.Message);
            return "";
        }
    }
    
    List<PlayerData> ExtractPlayersFromGameState(string json)
    {
        List<PlayerData> playersList = new List<PlayerData>();
        
        try
        {
            // Look for "players" array in the gameState
            string playersPattern = "\"players\":[";
            int startIdx = json.IndexOf(playersPattern);
            
            if (startIdx == -1)
            {
                Debug.Log("No players array found in game state JSON");
                return playersList;
            }
            
            startIdx += playersPattern.Length;
            int endIdx = json.IndexOf(']', startIdx);
            if (endIdx == -1) return playersList;
            
            string playersContent = json.Substring(startIdx, endIdx - startIdx);
            
            if (string.IsNullOrWhiteSpace(playersContent))
            {
                return playersList;
            }
            
            string[] playerStrings = playersContent.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string playerStr in playerStrings)
            {
                string cleanStr = playerStr.Trim('{', '}', ' ');
                
                string name = ExtractJsonValue(cleanStr, "name");
                string cardCountStr = ExtractJsonValue(cleanStr, "cardCount");
                
                int cardCount = 0;
                int.TryParse(cardCountStr, out cardCount);
                
                if (!string.IsNullOrEmpty(name))
                {
                    playersList.Add(new PlayerData { name = name, cardCount = cardCount });
                }
            }
            
            return playersList;
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting players from game state: " + e.Message);
            return playersList;
        }
    }
    
    // Simple JSON parsing functions without external dependencies
    string ExtractRoomCodeFromJson(string json)
    {
        try
        {
            // Look for "roomCode":"VALUE" pattern
            string pattern = "\"roomCode\":\"";
            int startIndex = json.IndexOf(pattern);
            if (startIndex != -1)
            {
                startIndex += pattern.Length;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex != -1)
                {
                    return json.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting room code: " + e.Message);
            return null;
        }
    }
    
    // Simplified JSON parsing - just look for the players array
    List<PlayerData> ExtractPlayersSimple(string json)
    {
        List<PlayerData> playersList = new List<PlayerData>();
        
        try
        {
            Debug.Log("=== SIMPLIFIED JSON PARSING ===");
            Debug.Log("Input JSON: " + json);
            
            // Look for the pattern: "players":[
            string playersPattern = "\"players\":[";
            int startIdx = json.IndexOf(playersPattern);
            
            if (startIdx == -1)
            {
                Debug.LogError("No players array found in JSON");
                return playersList;
            }
            
            Debug.Log("Found players array at index: " + startIdx);
            startIdx += playersPattern.Length;
            
            // Find the closing bracket
            int endIdx = json.IndexOf(']', startIdx);
            if (endIdx == -1)
            {
                Debug.LogError("Malformed players array - no closing bracket");
                return playersList;
            }
            
            string playersContent = json.Substring(startIdx, endIdx - startIdx);
            Debug.Log("Players content: '" + playersContent + "'");
            
            // If empty array, return empty list
            if (string.IsNullOrWhiteSpace(playersContent))
            {
                Debug.Log("Empty players array");
                return playersList;
            }
            
            // Split by player objects
            string[] playerStrings = playersContent.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("Split into " + playerStrings.Length + " player objects");
            
            foreach (string playerStr in playerStrings)
            {
                string cleanStr = playerStr.Trim('{', '}', ' ');
                Debug.Log("Processing player string: '" + cleanStr + "'");
                
                // Extract name
                string name = ExtractJsonValue(cleanStr, "name");
                string cardCountStr = ExtractJsonValue(cleanStr, "cardCount");
                
                int cardCount = 0;
                int.TryParse(cardCountStr, out cardCount);
                
                if (!string.IsNullOrEmpty(name))
                {
                    playersList.Add(new PlayerData { name = name, cardCount = cardCount });
                    Debug.Log("✓ Added player: " + name + " (" + cardCount + " cards)");
                }
                else
                {
                    Debug.LogWarning("Skipped player with empty name");
                }
            }
            
            Debug.Log("=== PARSING COMPLETE ===");
            Debug.Log("Total players parsed: " + playersList.Count);
            return playersList;
        }
        catch (Exception e)
        {
            Debug.LogError("Error in ExtractPlayersSimple: " + e.Message);
            Debug.LogError("Stack trace: " + e.StackTrace);
            return playersList;
        }
    }
    
    // Helper to extract a value from JSON string
    string ExtractJsonValue(string json, string key)
    {
        try
        {
            string pattern = "\"" + key + "\":\"";
            int startIdx = json.IndexOf(pattern);
            
            if (startIdx == -1)
            {
                // Try numeric pattern (for cardCount)
                pattern = "\"" + key + "\":";
                startIdx = json.IndexOf(pattern);
                if (startIdx == -1) 
                {
                    Debug.Log("Key '" + key + "' not found in JSON");
                    return "";
                }
                
                startIdx += pattern.Length;
                int endIdx = startIdx;
                
                // Find end of number
                while (endIdx < json.Length && (char.IsDigit(json[endIdx]) || json[endIdx] == '-'))
                {
                    endIdx++;
                }
                
                string result = json.Substring(startIdx, endIdx - startIdx);
                Debug.Log("Extracted numeric value for '" + key + "': '" + result + "'");
                return result;
            }
            else
            {
                // String value
                startIdx += pattern.Length;
                int endIdx = json.IndexOf('"', startIdx);
                if (endIdx == -1) 
                {
                    Debug.LogError("Malformed string value for key: " + key);
                    return "";
                }
                
                string result = json.Substring(startIdx, endIdx - startIdx);
                Debug.Log("Extracted string value for '" + key + "': '" + result + "'");
                return result;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting JSON value for key '" + key + "': " + e.Message);
            return "";
        }
    }
    
    string ExtractWinnerFromJson(string json)
    {
        try
        {
            // Look for "winner":"VALUE" pattern
            string pattern = "\"winner\":\"";
            int startIndex = json.IndexOf(pattern);
            if (startIndex != -1)
            {
                startIndex += pattern.Length;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex != -1)
                {
                    return json.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting winner: " + e.Message);
            return null;
        }
    }
    
    void ShowScreen(string screenName)
    {
        Debug.Log("Showing screen: " + screenName);
        
        // Hide all screens
        if (lobbyScreen != null) lobbyScreen.SetActive(false);
        if (gameScreen != null) gameScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        
        // Show target screen
        switch (screenName)
        {
            case "lobby":
                if (lobbyScreen != null) lobbyScreen.SetActive(true);
                break;
            case "game":
                if (gameScreen != null) gameScreen.SetActive(true);
                break;
            case "game-over":
                if (gameOverScreen != null) gameOverScreen.SetActive(true);
                break;
        }
    }
    
    void UpdatePlayersList(PlayerData[] newPlayers)
    {
        Debug.Log("=== UpdatePlayersList called ===");
        Debug.Log("Updating players list with " + newPlayers.Length + " players");
        
        // Limit to max 4 players
        int maxPlayers = Mathf.Min(newPlayers.Length, 4);
        PlayerData[] limitedPlayers = new PlayerData[maxPlayers];
        System.Array.Copy(newPlayers, limitedPlayers, maxPlayers);
        
        // Log each player being processed
        for (int i = 0; i < limitedPlayers.Length; i++)
        {
            Debug.Log("Player " + i + ": " + limitedPlayers[i].name + " (" + limitedPlayers[i].cardCount + " cards)");
        }
        
        // Update our local players list
        players.Clear();
        players.AddRange(limitedPlayers);
        
        // Use PlayerPositionManager for game screen
        if (playerPositionManager != null)
        {
            playerPositionManager.UpdatePlayersDisplay(limitedPlayers);
        }
        else
        {
            Debug.LogError("PlayerPositionManager is null! Please assign it in the Inspector.");
        }
        
        // Still update lobby screen for compatibility
        UpdateLobbyPlayersList(limitedPlayers);
        
        // Enable start game button only if 2-4 players
        if (startGameButton != null)
        {
            bool canStart = limitedPlayers.Length >= 2 && limitedPlayers.Length <= 4;
            startGameButton.interactable = canStart;
            Debug.Log("Start game button enabled: " + canStart + " (players: " + limitedPlayers.Length + "/2-4 required)");
            
            if (limitedPlayers.Length < 2)
            {
                Debug.Log("Need " + (2 - limitedPlayers.Length) + " more players to start the game");
            }
            else if (limitedPlayers.Length > 4)
            {
                Debug.Log("Too many players (" + limitedPlayers.Length + "/4 max)");
            }
        }
        else
        {
            Debug.LogError("StartGameButton is null!");
        }
    }
    
    void UpdateLobbyPlayersList(PlayerData[] newPlayers)
    {
        if (playersContainer == null) 
        {
            Debug.LogError("PlayersContainer is null! Please assign it in the Inspector.");
            return;
        }
        
        if (playerCardPrefab == null) 
        {
            Debug.LogError("PlayerCardPrefab is null! Please assign it in the Inspector.");
            return;
        }

        // Clear existing player cards in lobby
        foreach (Transform child in playersContainer)
        {
            Destroy(child.gameObject);
        }

        // Add new player cards for lobby
        foreach (var player in newPlayers)
        {
            GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            playerCard.SetActive(true);
            
            var nameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string playerName = (player.name == null || player.name == "") ? "Unknown" : player.name;
                nameText.text = playerName;
            }
        }

        // Force UI layout refresh for lobby
        Canvas.ForceUpdateCanvases();
        if (playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playersContainer.GetComponent<RectTransform>());
        }
    }
    
    void UpdateTopCard(string cardValue)
    {
        Debug.Log($"=== UpdateTopCard called with: {cardValue} ===");
        
        if (topCardImage != null && cardValue != null && cardValue != "")
        {
            // Get the CardController component from the top card
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController == null)
            {
                Debug.Log("No CardController found, adding one to topCardImage");
                cardController = topCardImage.gameObject.AddComponent<CardController>();
            }
            else
            {
                Debug.Log("Found existing CardController on topCardImage");
            }
            
            // Parse card value (format like "J of hearts", "8 of hearts")
            CardData cardData = ParseCardValue(cardValue);
            if (cardData != null)
            {
                Debug.Log($"Parsed card data: {cardData.value} of {cardData.suit}");
                cardController.SetCard(cardData.suit, cardData.value);
                Debug.Log($"Called CardController.SetCard with: {cardData.suit}, {cardData.value}");
            }
            else
            {
                Debug.LogWarning("Could not parse card value: " + cardValue);
            }
        }
        else
        {
            if (topCardImage == null) Debug.LogError("topCardImage is null!");
            if (cardValue == null || cardValue == "") Debug.LogError("cardValue is null or empty!");
        }
    }
    
    // Helper method to parse card strings like "J of hearts", "8 of hearts", "A of spades"
    CardData ParseCardValue(string cardString)
    {
        Debug.Log($"ParseCardValue called with: '{cardString}'");
        
        if (string.IsNullOrEmpty(cardString))
        {
            Debug.LogError("Card string is null or empty");
            return null;
        }
        
        // Expected format: "J of hearts", "8 of hearts", "A of spades"
        string[] parts = cardString.Split(new string[] { " of " }, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            Debug.LogError($"Invalid card format: '{cardString}'. Expected format: 'RANK of SUIT'");
            return null;
        }
        
        string valueStr = parts[0].Trim();
        string suit = parts[1].Trim().ToLower();
        
        Debug.Log($"Parsed parts - Value: '{valueStr}', Suit: '{suit}'");
        
        // Convert suit to lowercase for consistency
        if (suit != "hearts" && suit != "diamonds" && suit != "clubs" && suit != "spades")
        {
            Debug.LogError($"Invalid suit: '{suit}'. Expected: hearts, diamonds, clubs, or spades");
            return null;
        }
        
        // Convert value
        int value = 0;
        switch (valueStr.ToUpper())
        {
            case "A": value = 1; break;
            case "J": value = 11; break;
            case "Q": value = 12; break;
            case "K": value = 13; break;
            default:
                if (!int.TryParse(valueStr, out value))
                {
                    Debug.LogError($"Invalid card value: '{valueStr}'");
                    return null;
                }
                break;
        }
        
        if (value < 1 || value > 13)
        {
            Debug.LogError($"Card value out of range: {value}");
            return null;
        }
        
        Debug.Log($"Successfully parsed card: {value} of {suit}");
        return new CardData(suit, value);
    }
    
    System.Collections.IEnumerator RefreshUIDelayed()
    {
        // Wait a tiny bit for the UI to settle
        yield return new WaitForEndOfFrame();
        
        // Force another UI refresh
        Canvas.ForceUpdateCanvases();
        
        if (playersContainer != null && playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playersContainer.GetComponent<RectTransform>());
            Debug.Log("Delayed UI refresh completed");
        }
    }
}

// Data classes for Socket.IO
[Serializable]
public class PlayerData
{
    public string name;
    public int cardCount;
}

[Serializable]
public class PlayerJoinedData
{
    public string playerName;     // The name of the player who just joined
    public PlayerData[] players;  // Array of all players in the room
}