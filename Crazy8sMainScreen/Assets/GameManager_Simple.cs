using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject startScreen;
    public GameObject lobbyScreen;
    public GameObject gameScreen;
    public GameObject gameOverScreen;
    
    [Header("Start Screen UI")]
    public Button startScreenButton;
    
    [Header("Lobby UI")]
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI waitingText;
    public Transform playersContainer;
    public GameObject playerCardPrefab;
    public RoomCodeBorderCycler roomCodeBorderCycler;
    
    [Header("Game UI")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI currentColorText;
    public TextMeshProUGUI deckCountText;
    public Image topCardImage;
    public PlayerPositionManager playerPositionManager;
    
    [Header("Background Effects")]
    public Image colorChangerBackground;
    private Color originalBackgroundColor;
    
    [Header("8 Card Spiral Animation")]
    public MonoBehaviour spiralAnimationController;
    
    [Header("Card Colors for Background")]
    public Color redColor = new Color(0.6f, 0.08f, 0.08f, 0.8f);
    public Color blueColor = new Color(0.08f, 0.08f, 0.6f, 0.8f);
    public Color greenColor = new Color(0.08f, 0.4f, 0.08f, 0.8f);
    public Color yellowColor = new Color(0.6f, 0.6f, 0.08f, 0.8f);
    
    [Header("Game Over UI")]
    public TextMeshProUGUI winnerText;
    public Button playAgainButton;
    
    [Header("Connection")]
    public string serverURL = "http://localhost:3000";
    
    private SocketIOUnity socket;
    private string roomCode;
    private List<PlayerData> players = new List<PlayerData>();
    private bool isFirstPlayer = false;
    
    // Queue for main thread actions
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    void Start()
    {
        Debug.Log("=== SIMPLE GAMEMANAGER START ===");
        
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        // Store original background color
        if (colorChangerBackground != null)
        {
            originalBackgroundColor = colorChangerBackground.color;
        }
        
        // Find spiral animation controller
        if (spiralAnimationController == null)
        {
            MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour comp in allComponents)
            {
                if (comp.GetType().Name == "SpiralAnimationController")
                {
                    spiralAnimationController = comp;
                    Debug.Log("Found SpiralAnimationController");
                    break;
                }
            }
        }
        
        // Setup buttons
        if (startScreenButton != null)
        {
            startScreenButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(RestartGame);
        }
        
        // Show start screen
        ShowScreen("start");
        
        // Connect to server
        ConnectToServer();
        
        Debug.Log("=== SIMPLE GAMEMANAGER READY ===");
    }
    
    void Update()
    {
        // Process main thread actions
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                var action = mainThreadActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in main thread action: {e.Message}");
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
    
    void ConnectToServer()
    {
        try
        {
            socket = new SocketIOUnity(serverURL, new SocketIOOptions
            {
                Query = new Dictionary<string, string>(),
                EIO = 4,
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            socket.OnConnected += OnSocketConnected;
            socket.OnDisconnected += OnSocketDisconnected;
            
            // Register event handlers
            socket.On("room-created", HandleRoomCreated);
            socket.On("player-joined", HandlePlayerJoined);
            socket.On("game-started", HandleGameStarted);
            socket.On("card-played", HandleCardPlayed);
            socket.On("card-drawn", HandleCardDrawn);
            socket.On("game-state-updated", HandleGameStateUpdated);
            socket.On("color-chosen", HandleColorChosen);
            socket.On("game-over", HandleGameOver);
            socket.On("room-error", HandleRoomError);
            socket.On("game-ended", HandleGameEnded);

            socket.Connect();
            Debug.Log("Attempting to connect to server at: " + serverURL);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize socket connection: " + e.Message);
        }
    }
    
    void OnSocketConnected(object sender, EventArgs e)
    {
        EnqueueMainThreadAction(() => {
            Debug.Log("Connected to server successfully");
        });
    }
    
    void OnSocketDisconnected(object sender, string e)
    {
        EnqueueMainThreadAction(() => {
            Debug.Log("Disconnected from server: " + e);
        });
    }
    
    void OnStartButtonClicked()
    {
        Debug.Log("Start button clicked - creating room");
        
        if (socket != null && socket.Connected)
        {
            socket.Emit("create-room");
            Debug.Log("Sent create-room request");
        }
        else
        {
            Debug.LogError("Socket not connected!");
        }
    }
    
    void RestartGame()
    {
        if (playerPositionManager != null)
        {
            playerPositionManager.ClearAllPlayers();
        }
        
        ClearPlayersList();
        ShowScreen("start");
    }
    
    void ClearPlayersList()
    {
        if (playersContainer != null)
        {
            foreach (Transform child in playersContainer)
            {
                if (child.gameObject != null)
                    Destroy(child.gameObject);
            }
        }
    }
    
    void HandleRoomCreated(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Room created response received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Room created JSON: " + jsonString);
            
            string extractedRoomCode = ExtractRoomCodeFromJson(jsonString);
            
            EnqueueMainThreadAction(() => {
                roomCode = extractedRoomCode;
                Debug.Log("Room code extracted: " + roomCode);
                
                if (roomCodeText != null)
                {
                    roomCodeText.text = roomCode;
                }
                
                ShowScreen("lobby");
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling room created: " + e.Message);
        }
    }
    
    void HandlePlayerJoined(SocketIOResponse response)
    {
        try
        {
            Debug.Log("Player joined response received");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Player joined JSON: " + jsonString);
            
            List<PlayerData> newPlayers = ExtractPlayersSimple(jsonString);
            
            EnqueueMainThreadAction(() => {
                players = newPlayers;
                UpdateLobbyPlayersList(players.ToArray());
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling player joined: " + e.Message);
        }
    }
    
    void HandleGameStarted(SocketIOResponse response)
    {
        EnqueueMainThreadAction(() => {
            Debug.Log("Game started!");
            ShowScreen("game");
        });
    }
    
    void HandleCardPlayed(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log("Card played JSON: " + jsonString);
            
            string playedCard = ExtractPlayedCardFromJson(jsonString);
            string cardColor = ExtractCardColor(playedCard);
            
            EnqueueMainThreadAction(() => {
                UpdateTopCard(playedCard);
            });
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
            string jsonString = response.GetValue().ToString();
            string gameStateJson = ExtractGameStateJson(jsonString);
            
            EnqueueMainThreadAction(() => {
                UpdateGameStateFromResponse(gameStateJson);
            });
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
            string jsonString = response.GetValue().ToString();
            
            EnqueueMainThreadAction(() => {
                UpdateGameStateFromResponse(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling game state updated: " + e.Message);
        }
    }
    
    void HandleColorChosen(SocketIOResponse response)
    {
        try
        {
            Debug.Log("=== COLOR CHOSEN EVENT RECEIVED ===");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Color chosen JSON: " + jsonString);
            
            string color = ExtractJsonValue(jsonString, "color");
            string playerName = ExtractJsonValue(jsonString, "playerName");
            
            Debug.Log($"Color='{color}', Player='{playerName}'");
            
            if (!string.IsNullOrEmpty(color))
            {
                // Update UI
                if (currentColorText != null)
                {
                    currentColorText.text = "Current Color: " + color;
                }
                
                // Trigger simple 8-card animation
                if (topCardImage != null)
                {
                    CardController cardController = topCardImage.GetComponent<CardController>();
                    if (cardController != null)
                    {
                        Debug.Log($"Triggering simple 8-card animation to {color}");
                        
                        // Trigger spiral animation directly
                        if (spiralAnimationController != null)
                        {
                            var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                            if (method != null)
                            {
                                method.Invoke(spiralAnimationController, new object[] { cardController, color });
                                Debug.Log("✅ Animation triggered!");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No spiral animation controller found");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No color in response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling color choice: " + e.Message);
        }
    }
    
    void HandleGameOver(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            string winner = ExtractWinnerFromJson(jsonString);
            
            EnqueueMainThreadAction(() => {
                if (winnerText != null)
                {
                    winnerText.text = winner + " Wins!";
                }
                ShowScreen("game-over");
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Error handling game over: " + e.Message);
        }
    }
    
    void HandleRoomError(SocketIOResponse response)
    {
        Debug.LogError("Room error: " + response.GetValue().ToString());
    }
    
    void HandleGameEnded(SocketIOResponse response)
    {
        EnqueueMainThreadAction(() => {
            Debug.Log("Game ended");
            ShowScreen("start");
        });
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
            
            // Parse card value (format like "J of red", "8 of blue")
            CardData cardData = ParseCardValue(cardValue);
            if (cardData != null)
            {
                Debug.Log($"Parsed card data: {cardData.value} of {cardData.color}");
                
                // Simple logic: 8 cards are shown as neutral gray until animation changes them
                if (cardData.value == 8)
                {
                    Debug.Log("8 card detected - showing as neutral 8");
                    cardController.SetCard("gray", 8);
                    // Don't change background - that happens during animation
                }
                else
                {
                    // Regular card - show normally and update background
                    cardController.SetCard(cardData.color, cardData.value);
                    ChangeBackgroundToCardColor(cardData.color);
                    Debug.Log($"Regular card: {cardData.value} of {cardData.color}");
                }
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
        Debug.Log($"Parsing card string: '{cardString}'");
        
        if (string.IsNullOrEmpty(cardString))
        {
            Debug.LogWarning("Card string is null or empty");
            return null;
        }
        
        string[] parts = cardString.Split(' ');
        if (parts.Length != 2)
        {
            Debug.LogWarning($"Invalid card format: '{cardString}' (expected 'value color')");
            return null;
        }
        
        string valueStr = parts[0];
        string color = parts[1];
        
        if (color != "red" && color != "blue" && color != "green" && color != "yellow")
        {
            Debug.LogWarning($"Invalid color: '{color}'");
            return null;
        }
        
        int value;
        switch (valueStr.ToUpper())
        {
            case "J": value = 11; break;
            case "Q": value = 12; break;
            case "K": value = 13; break;
            default:
                if (!int.TryParse(valueStr, out value))
                {
                    Debug.LogWarning($"Could not parse card value: '{valueStr}'");
                    return null;
                }
                break;
        }
        
        if (value < 1 || value > 13)
        {
            Debug.LogWarning($"Card value out of range: {value}");
            return null;
        }
        
        return new CardData(color, value);
    }
    
    // Background color effect methods
    public void ChangeBackgroundToCardColor(string cardColor)
    {
        if (colorChangerBackground == null)
        {
            Debug.LogWarning("ColorChangerBackground is null, cannot change color");
            return;
        }
        
        Color bgColor = GetBackgroundColor(cardColor);
        colorChangerBackground.color = bgColor;
        Debug.Log($"Background changed to {cardColor}: {bgColor}");
    }
    
    void ResetBackgroundColor()
    {
        if (colorChangerBackground != null)
        {
            colorChangerBackground.color = originalBackgroundColor;
        }
    }
    
    public Color GetBackgroundColor(string color)
    {
        switch (color.ToLower())
        {
            case "red": return redColor;
            case "blue": return blueColor;
            case "green": return greenColor;
            case "yellow": return yellowColor;
            default: return originalBackgroundColor;
        }
    }
    
    /// <summary>
    /// Called by SpiralAnimationController when spiral animation completes
    /// </summary>
    public void OnSpiralAnimationComplete()
    {
        Debug.Log("Spiral animation complete - notifying server");
        
        if (socket != null && socket.Connected)
        {
            socket.Emit("animation-complete");
        }
        else
        {
            Debug.LogWarning("Cannot notify server - socket not connected");
        }
    }
    
    void UpdateGameStateFromResponse(string jsonString)
    {
        try
        {
            string gameStateJson = ExtractGameStateJson(jsonString);
            if (!string.IsNullOrEmpty(gameStateJson))
            {
                List<PlayerData> newPlayers = ExtractPlayersFromGameState(gameStateJson);
                string currentCard = ExtractCurrentCardFromJson(gameStateJson);
                
                if (newPlayers != null && newPlayers.Count > 0)
                {
                    players = newPlayers;
                    UpdatePlayersList(players.ToArray());
                }
                
                if (!string.IsNullOrEmpty(currentCard))
                {
                    UpdateTopCard(currentCard);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error updating game state: " + e.Message);
        }
    }
    
    string ExtractGameStateJson(string json)
    {
        // Look for "gameState" key
        int gameStateStart = json.IndexOf("\"gameState\":");
        if (gameStateStart != -1)
        {
            gameStateStart += "\"gameState\":".Length;
            
            // Find the matching closing brace
            int braceCount = 0;
            int startBrace = json.IndexOf('{', gameStateStart);
            if (startBrace == -1) return null;
            
            for (int i = startBrace; i < json.Length; i++)
            {
                if (json[i] == '{') braceCount++;
                else if (json[i] == '}') 
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        return json.Substring(startBrace, i - startBrace + 1);
                    }
                }
            }
        }
        
        return json; // Return original if no gameState found
    }
    
    string ExtractCurrentCardFromJson(string json)
    {
        return ExtractJsonValue(json, "currentCard");
    }
    
    List<PlayerData> ExtractPlayersFromGameState(string json)
    {
        return ExtractPlayersSimple(json);
    }
    
    // Simple JSON parsing functions
    string ExtractRoomCodeFromJson(string json)
    {
        return ExtractJsonValue(json, "roomCode");
    }
    
    List<PlayerData> ExtractPlayersSimple(string json)
    {
        List<PlayerData> playersList = new List<PlayerData>();
        
        try
        {
            // Find the players array
            int playersStart = json.IndexOf("\"players\":");
            if (playersStart == -1) return playersList;
            
            int arrayStart = json.IndexOf('[', playersStart);
            int arrayEnd = json.IndexOf(']', arrayStart);
            
            if (arrayStart == -1 || arrayEnd == -1) return playersList;
            
            string playersArray = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            
            // Simple parsing - split by }, then extract name and cardCount
            string[] playerObjects = playersArray.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string playerObj in playerObjects)
            {
                string cleanObj = playerObj.Replace("{", "").Replace("}", "");
                
                string name = ExtractJsonValue(cleanObj, "name");
                string cardCountStr = ExtractJsonValue(cleanObj, "cardCount");
                
                if (!string.IsNullOrEmpty(name) && int.TryParse(cardCountStr, out int cardCount))
                {
                    playersList.Add(new PlayerData { name = name, cardCount = cardCount });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing players: " + e.Message);
        }
        
        return playersList;
    }
    
    string ExtractJsonValue(string json, string key)
    {
        string searchKey = "\"" + key + "\":";
        int keyIndex = json.IndexOf(searchKey);
        
        if (keyIndex == -1) return null;
        
        int valueStart = keyIndex + searchKey.Length;
        
        // Skip whitespace
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;
        
        if (valueStart >= json.Length) return null;
        
        if (json[valueStart] == '"')
        {
            // String value
            valueStart++; // Skip opening quote
            int valueEnd = json.IndexOf('"', valueStart);
            if (valueEnd == -1) return null;
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
        else
        {
            // Number or other value
            int valueEnd = valueStart;
            while (valueEnd < json.Length && 
                   json[valueEnd] != ',' && 
                   json[valueEnd] != '}' && 
                   json[valueEnd] != ']' &&
                   !char.IsWhiteSpace(json[valueEnd]))
            {
                valueEnd++;
            }
            
            return json.Substring(valueStart, valueEnd - valueStart);
        }
    }
    
    string ExtractPlayedCardFromJson(string json)
    {
        return ExtractJsonValue(json, "card");
    }
    
    string ExtractCardColor(string cardString)
    {
        if (string.IsNullOrEmpty(cardString)) return "";
        
        // Card format: "value color" (e.g., "8 red", "J blue")
        string[] parts = cardString.Split(' ');
        if (parts.Length >= 2)
        {
            return parts[1]; // The color part
        }
        
        return "";
    }
    
    string ExtractWinnerFromJson(string json)
    {
        return ExtractJsonValue(json, "winner");
    }
    
    void ShowScreen(string screenName)
    {
        if (screenName != "game" && playerPositionManager != null)
        {
            playerPositionManager.ClearAllPlayers();
        }
        
        if (screenName != "game")
        {
            ClearPlayersList();
        }
        
        if (startScreen != null) startScreen.SetActive(screenName == "start");
        if (lobbyScreen != null) lobbyScreen.SetActive(screenName == "lobby");
        if (gameScreen != null) gameScreen.SetActive(screenName == "game");
        if (gameOverScreen != null) gameOverScreen.SetActive(screenName == "game-over");
        
        switch (screenName)
        {
            case "start":
                if (startScreen != null && roomCodeBorderCycler != null)
                {
                    roomCodeBorderCycler.StopCycling();
                }
                break;
                
            case "lobby":
                if (lobbyScreen != null && roomCodeBorderCycler != null)
                {
                    roomCodeBorderCycler.StartCycling();
                }
                break;
                
            case "game":
                if (gameScreen != null && roomCodeBorderCycler != null)
                {
                    roomCodeBorderCycler.StopCycling();
                }
                break;
                
            case "game-over":
                if (gameOverScreen != null && roomCodeBorderCycler != null)
                {
                    roomCodeBorderCycler.StopCycling();
                }
                break;
        }
    }
    
    void UpdatePlayersList(PlayerData[] newPlayers)
    {
        if (gameScreen != null && gameScreen.activeInHierarchy)
        {
            if (playerPositionManager != null)
            {
                playerPositionManager.UpdatePlayerDisplay(newPlayers);
            }
            else
            {
                Debug.LogWarning("PlayerPositionManager is null");
            }
        }
        else
        {
            UpdateLobbyPlayersList(newPlayers);
        }
    }
    
    void UpdateLobbyPlayersList(PlayerData[] newPlayers)
    {
        if (playersContainer == null) 
        {
            Debug.LogWarning("PlayersContainer is null");
            return;
        }
        
        if (playerCardPrefab == null) 
        {
            Debug.LogWarning("PlayerCardPrefab is null");
            return;
        }
        
        // Clear existing player cards
        foreach (Transform child in playersContainer)
        {
            if (child.gameObject != null)
                Destroy(child.gameObject);
        }
        
        // Create new player cards
        foreach (PlayerData player in newPlayers)
        {
            GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            
            TextMeshProUGUI playerNameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            if (playerNameText != null)
            {
                playerNameText.text = player.name;
            }
        }
        
        // Force layout refresh
        if (playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            StartCoroutine(RefreshUIDelayed());
        }
    }
    
    System.Collections.IEnumerator RefreshUIDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        if (playersContainer != null && playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            UnityEngine.UI.LayoutGroup layout = playersContainer.GetComponent<UnityEngine.UI.LayoutGroup>();
            layout.enabled = false;
            layout.enabled = true;
        }
    }
    
    void OnDestroy()
    {
        if (socket != null)
        {
            try
            {
                socket.Disconnect();
                socket.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError("Error disposing socket: " + e.Message);
            }
        }
    }
}

// Data classes for Socket.IO
[Serializable]
public class PlayerData
{
    public string name;
    public int cardCount;
    public bool isFirstPlayer;
}

[Serializable]
public class PlayerJoinedData
{
    public string playerName;
    public PlayerData[] players;
}
