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
    public Transform gamePlayersContainer;
    public Transform playerHand;
    
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
        roomCodeText.text = "Click 'Create Room' to start";
        
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
            socket.On("room-error", new Action<SocketIOResponse>(HandleRoomError));
            
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
        EnqueueMainThreadAction(delegate() {
            ShowScreen("game");
        });
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
            roomCode = "TEST123";
            roomCodeText.text = "Room Code: " + roomCode;
        }
    }
    
    void StartGame()
    {
        Debug.Log("Start Game clicked - Players: " + players.Count + ", Room: " + roomCode);
        
        if (socket != null && socket.Connected && roomCode != null && roomCode != "")
        {
            if (players.Count >= 2)
            {
                Debug.Log("Starting game with " + players.Count + " players");
                // Send just the room code as data - no callback needed
                socket.Emit("start-game", new { roomCode = roomCode });
            }
            else
            {
                Debug.LogWarning("Cannot start game - only " + players.Count + " players (need 2+)");
                string currentRoom = roomCode;
                EnqueueMainThreadAction(delegate() {
                    roomCodeText.text = "Room Code: " + currentRoom + " - Need 2+ players to start";
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
        roomCodeText.text = "Click 'Create Room' to start";
        startGameButton.interactable = false;
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
                        roomCodeText.text = "Room Code: " + roomCode;
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
        
        // Force immediate UI refresh at start
        Canvas.ForceUpdateCanvases();
        
        // Log each player being processed
        for (int i = 0; i < newPlayers.Length; i++)
        {
            Debug.Log("Player " + i + ": " + newPlayers[i].name + " (" + newPlayers[i].cardCount + " cards)");
        }
        
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
        
        Debug.Log("PlayersContainer name: " + playersContainer.name);
        Debug.Log("PlayersContainer active: " + playersContainer.gameObject.activeInHierarchy);
        Debug.Log("PlayersContainer position: " + playersContainer.position);
        Debug.Log("PlayerCardPrefab name: " + playerCardPrefab.name);
        Debug.Log("PlayerCardPrefab active: " + playerCardPrefab.activeInHierarchy);
        
        // Check if the PlayersContainer is actually visible in the scene
        Canvas parentCanvas = playersContainer.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.Log("PlayersContainer parent Canvas: " + parentCanvas.name + " - enabled: " + parentCanvas.enabled);
        }
        else
        {
            Debug.LogError("PlayersContainer has no parent Canvas!");
        }
        
        // Update our local players list
        players.Clear();
        players.AddRange(newPlayers);
        
        // Clear existing player cards
        int childrenToDestroy = playersContainer.childCount;
        Debug.Log("Destroying " + childrenToDestroy + " existing player cards");
        
        foreach (Transform child in playersContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add new player cards
        Debug.Log("Starting to create " + newPlayers.Length + " player cards");
        
        foreach (var player in newPlayers)
        {
            Debug.Log("Creating player card for: " + player.name + " (" + player.cardCount + " cards)");
            
            GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            Debug.Log("Player card instantiated: " + playerCard.name);
            Debug.Log("Player card parent: " + playerCard.transform.parent.name);
            Debug.Log("Player card world position: " + playerCard.transform.position);
            Debug.Log("Player card local position: " + playerCard.transform.localPosition);
            
            // Ensure the card is active
            playerCard.SetActive(true);
            Debug.Log("Player card set to active");
            
            // Fix RectTransform sizing - this is crucial!
            RectTransform cardRect = playerCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                // Set a proper size for the player card
                cardRect.sizeDelta = new Vector2(300f, 50f); // Width: 300px, Height: 50px
                Debug.Log("Set PlayerCard RectTransform size to: " + cardRect.sizeDelta);
                
                // Add Content Size Fitter if it doesn't exist
                ContentSizeFitter sizeFitter = playerCard.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = playerCard.AddComponent<ContentSizeFitter>();
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    Debug.Log("Added Content Size Fitter to PlayerCard");
                }
                
                // Add Layout Element for proper sizing
                LayoutElement layoutElement = playerCard.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = playerCard.AddComponent<LayoutElement>();
                    layoutElement.minWidth = 250f;
                    layoutElement.minHeight = 40f;
                    layoutElement.preferredWidth = 300f;
                    layoutElement.preferredHeight = 50f;
                    Debug.Log("Added Layout Element to PlayerCard");
                }
                
                // Force immediate layout recalculation
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
                Debug.Log("Final player card size: " + cardRect.sizeDelta);
            }
            
            var nameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            
            if (nameText != null)
            {
                string playerName = (player.name == null || player.name == "") ? "Unknown" : player.name;
                nameText.text = playerName + " (" + player.cardCount + " cards)";
                Debug.Log("Player card text set to: " + nameText.text);
                Debug.Log("Text component active: " + nameText.gameObject.activeInHierarchy);
                Debug.Log("Text component enabled: " + nameText.enabled);
                Debug.Log("Player card RectTransform size: " + playerCard.GetComponent<RectTransform>().sizeDelta);
            }
            else
            {
                Debug.LogError("PlayerCard prefab doesn't have TextMeshProUGUI component!");
                Debug.LogError("PlayerCard components: ");
                Component[] components = playerCard.GetComponentsInChildren<Component>();
                foreach (Component comp in components)
                {
                    Debug.LogError("  - " + comp.GetType().Name + " on " + comp.gameObject.name);
                }
            }
        }
        
        Debug.Log("PlayersContainer now has " + playersContainer.childCount + " children");
        
        // Force UI layout refresh - this is crucial for Layout Groups!
        Canvas.ForceUpdateCanvases();
        
        // Also try to rebuild the layout
        if (playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(playersContainer.GetComponent<RectTransform>());
            Debug.Log("Forced layout rebuild on PlayersContainer");
        }
        
        // Enable start game button only if 2+ players
        if (startGameButton != null)
        {
            bool canStart = newPlayers.Length >= 2; // Need at least 2 players for a real game
            startGameButton.interactable = canStart;
            Debug.Log("Start game button enabled: " + canStart + " (players: " + newPlayers.Length + "/2 required)");
            
            if (!canStart)
            {
                Debug.Log("Need " + (2 - newPlayers.Length) + " more players to start the game");
            }
        }
        else
        {
            Debug.LogError("StartGameButton is null!");
        }
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