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
    private bool isFirstPlayer = false;
    
    // Queue for main thread actions
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    void Start()
    {
        Debug.Log("=== GAMEMANAGER START() BEGIN ===");
        
        // Check if all references are assigned
        bool validationPassed = ValidateReferences();
        if (!validationPassed)
        {
            Debug.LogWarning("Some UI references are missing, but continuing with available components...");
        }
        else
        {
            Debug.Log("All references validated successfully");
        }
        
        // Clear any existing player cards from previous sessions
        ClearPlayersList();
        
        // Set initial room code text
        if (roomCodeText != null)
        {
            roomCodeText.text = "----";
        }
        
        // Set up button events with debugging
        Debug.Log("Setting up button listeners...");
        
        if (startScreenButton != null)
        {
            Debug.Log("StartScreenButton found, adding OnStartButtonClicked listener");
            startScreenButton.onClick.AddListener(() => {
                Debug.Log("*** START BUTTON CLICK DETECTED ***");
                OnStartButtonClicked();
            });
            Debug.Log("StartScreenButton listener added successfully");
            
            // Fix Canvas Group blocking issue - ALWAYS call this
            FixCanvasGroupBlocking();
        }
        else
        {
            Debug.LogError("CRITICAL: StartScreenButton is NULL!");
        }
        
        if (playAgainButton != null)
        {
            Debug.Log("PlayAgainButton found, adding RestartGame listener");
            playAgainButton.onClick.AddListener(RestartGame);
        }
        else
        {
            Debug.LogError("PlayAgainButton is NULL!");
        }
        
        Debug.Log("Button listeners setup complete");
        
        // Show start screen initially
        Debug.Log("Showing start screen...");
        ShowScreen("start");
        
        // Initialize socket connection
        Debug.Log("Initializing socket connection...");
        ConnectToServer();
        
        Debug.Log("=== GAMEMANAGER START() COMPLETE ===");
    }
    
    void FixCanvasGroupBlocking()
    {
        Debug.Log("=== FIXING CANVAS GROUP BLOCKING ===");
        
        if (startScreenButton != null)
        {
            // Check for Canvas Group on the button itself
            CanvasGroup buttonCanvasGroup = startScreenButton.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup != null)
            {
                Debug.Log("Found CanvasGroup on button - setting interactable to true");
                buttonCanvasGroup.interactable = true;
                buttonCanvasGroup.blocksRaycasts = true;
                buttonCanvasGroup.alpha = 1f;
            }
            else
            {
                Debug.Log("No CanvasGroup found directly on button");
            }
            
            // Check for Canvas Group on parent objects and fix ALL of them
            Transform current = startScreenButton.transform;
            int parentLevel = 0;
            while (current != null && parentLevel < 10) // Prevent infinite loop
            {
                CanvasGroup parentCanvasGroup = current.GetComponent<CanvasGroup>();
                if (parentCanvasGroup != null)
                {
                    Debug.Log($"Found CanvasGroup on {current.name} (level {parentLevel}) - enabling interaction");
                    Debug.Log($"  Before: interactable={parentCanvasGroup.interactable}, blocksRaycasts={parentCanvasGroup.blocksRaycasts}, alpha={parentCanvasGroup.alpha}");
                    
                    parentCanvasGroup.interactable = true;
                    parentCanvasGroup.blocksRaycasts = true;
                    parentCanvasGroup.alpha = 1f;
                    
                    Debug.Log($"  After: interactable={parentCanvasGroup.interactable}, blocksRaycasts={parentCanvasGroup.blocksRaycasts}, alpha={parentCanvasGroup.alpha}");
                }
                current = current.parent;
                parentLevel++;
            }
            
            // Also check the start screen itself
            if (startScreen != null)
            {
                CanvasGroup screenCanvasGroup = startScreen.GetComponent<CanvasGroup>();
                if (screenCanvasGroup != null)
                {
                    Debug.Log("Found CanvasGroup on StartScreen - enabling interaction");
                    Debug.Log($"  Before: interactable={screenCanvasGroup.interactable}, blocksRaycasts={screenCanvasGroup.blocksRaycasts}, alpha={screenCanvasGroup.alpha}");
                    
                    screenCanvasGroup.interactable = true;
                    screenCanvasGroup.blocksRaycasts = true;
                    screenCanvasGroup.alpha = 1f;
                    
                    Debug.Log($"  After: interactable={screenCanvasGroup.interactable}, blocksRaycasts={screenCanvasGroup.blocksRaycasts}, alpha={screenCanvasGroup.alpha}");
                }
                else
                {
                    Debug.Log("No CanvasGroup found on StartScreen");
                }
                
                // Check all child Canvas Groups in the start screen
                CanvasGroup[] allCanvasGroups = startScreen.GetComponentsInChildren<CanvasGroup>();
                Debug.Log($"Found {allCanvasGroups.Length} CanvasGroups in StartScreen hierarchy");
                
                for (int i = 0; i < allCanvasGroups.Length; i++)
                {
                    CanvasGroup cg = allCanvasGroups[i];
                    Debug.Log($"  CanvasGroup {i} on {cg.name}: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                    cg.alpha = 1f;
                    Debug.Log($"    Fixed to: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");
                }
            }
        }
        
        Debug.Log("=== CANVAS GROUP FIX COMPLETE ===");
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
        
        // Debug button state every few seconds when on start screen
        if (startScreen != null && startScreen.activeInHierarchy && startScreenButton != null)
        {
            if (Time.time % 3f < 0.02f) // Every 3 seconds approximately
            {
                Debug.Log("=== START BUTTON STATUS CHECK ===");
                Debug.Log("Button active: " + startScreenButton.gameObject.activeInHierarchy);
                Debug.Log("Button interactable: " + startScreenButton.interactable);
                Debug.Log("Button enabled: " + startScreenButton.enabled);
                
                CanvasGroup blockingGroup = startScreenButton.GetComponentInParent<CanvasGroup>();
                bool isBlocked = blockingGroup != null && !blockingGroup.interactable;
                Debug.Log("Canvas Group blocking: " + isBlocked);
                
                // If still blocked, try to fix it again
                if (isBlocked)
                {
                    Debug.Log("Canvas Group still blocking - attempting fix...");
                    FixCanvasGroupBlocking();
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
        
        Debug.Log("=== VALIDATING REFERENCES ===");
        
        // Screen references
        if (startScreen == null) { Debug.LogError("StartScreen is not assigned!"); valid = false; }
        else { Debug.Log("✓ StartScreen assigned"); }
        
        if (lobbyScreen == null) { Debug.LogError("LobbyScreen is not assigned!"); valid = false; }
        else { Debug.Log("✓ LobbyScreen assigned"); }
        
        if (gameScreen == null) { Debug.LogError("GameScreen is not assigned!"); valid = false; }
        else { Debug.Log("✓ GameScreen assigned"); }
        
        if (gameOverScreen == null) { Debug.LogError("GameOverScreen is not assigned!"); valid = false; }
        else { Debug.Log("✓ GameOverScreen assigned"); }
        
        // Start screen UI
        if (startScreenButton == null) { Debug.LogError("StartScreenButton is not assigned!"); valid = false; }
        else { 
            Debug.Log("✓ StartScreenButton assigned"); 
            Debug.Log("StartScreenButton interactable: " + startScreenButton.interactable);
            Debug.Log("StartScreenButton gameObject active: " + startScreenButton.gameObject.activeInHierarchy);
        }
        
        // Lobby UI  
        if (roomCodeText == null) { Debug.LogError("RoomCodeText is not assigned!"); valid = false; }
        else { Debug.Log("✓ RoomCodeText assigned"); }
        
        if (waitingText == null) { Debug.LogError("WaitingText is not assigned!"); valid = false; }
        else { Debug.Log("✓ WaitingText assigned"); }
        
        if (playersContainer == null) { Debug.LogError("PlayersContainer is not assigned!"); valid = false; }
        else { Debug.Log("✓ PlayersContainer assigned"); }
        
        if (playerCardPrefab == null) { Debug.LogError("PlayerCardPrefab is not assigned!"); valid = false; }
        else { Debug.Log("✓ PlayerCardPrefab assigned"); }
        
        // Game UI
        if (currentPlayerText == null) { Debug.LogError("CurrentPlayerText is not assigned!"); valid = false; }
        else { Debug.Log("✓ CurrentPlayerText assigned"); }
        
        if (currentSuitText == null) { Debug.LogError("CurrentSuitText is not assigned!"); valid = false; }
        else { Debug.Log("✓ CurrentSuitText assigned"); }
        
        if (deckCountText == null) { Debug.LogError("DeckCountText is not assigned!"); valid = false; }
        else { Debug.Log("✓ DeckCountText assigned"); }
        
        if (topCardImage == null) { Debug.LogError("TopCardImage is not assigned!"); valid = false; }
        else { Debug.Log("✓ TopCardImage assigned"); }
        
        if (playerPositionManager == null) { Debug.LogError("PlayerPositionManager is not assigned!"); valid = false; }
        else { Debug.Log("✓ PlayerPositionManager assigned"); }
        
        // Game Over UI
        if (winnerText == null) { Debug.LogError("WinnerText is not assigned!"); valid = false; }
        else { Debug.Log("✓ WinnerText assigned"); }
        
        if (playAgainButton == null) { Debug.LogError("PlayAgainButton is not assigned!"); valid = false; }
        else { Debug.Log("✓ PlayAgainButton assigned"); }
        
        Debug.Log("=== VALIDATION COMPLETE - Valid: " + valid + " ===");
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
    
    void OnStartButtonClicked()
    {
        Debug.Log("=== START BUTTON CLICKED ===");
        Debug.Log("Socket connected: " + (socket != null && socket.Connected));
        Debug.Log("Socket is null: " + (socket == null));
        
        if (socket != null && socket.Connected)
        {
            Debug.Log("Sending create-room event to server");
            socket.Emit("create-room");
            
            // Transition to lobby screen
            ShowScreen("lobby");
            
            // Update UI to show we're creating
            roomCodeText.text = "Creating room...";
        }
        else
        {
            Debug.LogWarning("Socket not connected, attempting to connect first...");
            
            // Try to connect first
            ConnectToServer();
            
            // For testing without server - create test room immediately  
            roomCode = "TEST";
            roomCodeText.text = "Room Code: " + roomCode;
            ShowScreen("lobby");
        }
    }
    
    // Manual test method - you can call this from the Inspector or add a key press
    [System.Obsolete("Debug method only")]
    public void TestStartButtonManually()
    {
        Debug.Log("=== MANUAL START BUTTON TEST ===");
        OnStartButtonClicked();
    }
    
    // Force fix Canvas Groups - can be called from Inspector
    public void ForceFixCanvasGroups()
    {
        Debug.Log("=== FORCE FIXING CANVAS GROUPS ===");
        FixCanvasGroupBlocking();
    }
    
    void RestartGame()
    {
        ShowScreen("lobby");
        roomCode = "";
        players.Clear();
        ClearPlayersList();
        
        // Clear player position displays when going back to lobby
        if (playerPositionManager != null)
        {
            playerPositionManager.ClearPlayerDisplays();
            Debug.Log("Cleared player position displays for lobby return");
        }
        
        roomCodeText.text = "----";
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
                        
                        Debug.Log("Room created successfully, waiting for players to join via phone");
                        Debug.Log("UI updated with room code: " + roomCode);
                    });
                }
                else
                {
                    Debug.LogError("Could not extract room code from response");
                    EnqueueMainThreadAction(delegate() {
                        roomCodeText.text = "Error creating room. Try again.";
                    });
                }
            }
            catch (Exception parseEx)
            {
                Debug.LogError("Error getting value from response: " + parseEx.Message);
                EnqueueMainThreadAction(delegate() {
                    roomCodeText.text = "Error creating room. Try again.";
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing room created data: " + e.Message);
            Debug.LogError("Stack trace: " + e.StackTrace);
            
            EnqueueMainThreadAction(delegate() {
                roomCodeText.text = "Error creating room. Try again.";
            });
        }
    }
    
    void HandlePlayerJoined(SocketIOResponse response)
    {
        try 
        {
            Debug.Log("======= PLAYER JOINED EVENT =======");
            
            // Get the raw JSON string
            string jsonString = response.GetValue().ToString();
            // Debug.Log("Raw JSON: " + jsonString);
            
            // Extract players array more simply
            var players = ExtractPlayersSimple(jsonString);
            
            if (players != null && players.Count > 0)
            {
                Debug.Log("PLAYER JOINED: Found " + players.Count + " players, calling UpdatePlayersList");
                
                // Check if this is the first player joining
                if (players.Count == 1 && !isFirstPlayer)
                {
                    isFirstPlayer = true;
                    Debug.Log("FIRST PLAYER JOINED: Granting start permissions");
                }
                
                PlayerData[] playersArray = players.ToArray();
                
                EnqueueMainThreadAction(() => {
                    UpdatePlayersList(playersArray);
                    
                    // Update waiting text based on first player
                    if (waitingText != null)
                    {
                        var firstPlayer = players.FirstOrDefault(p => p.isFirstPlayer);
                        if (firstPlayer != null)
                        {
                            waitingText.text = $"Waiting for {firstPlayer.name} to start the game...";
                        }
                        else
                        {
                            waitingText.text = "Waiting for first player to start the game...";
                        }
                    }
                    // StartCoroutine(RefreshUIDelayed());
                });
            }
            else
            {
                Debug.LogError("PLAYER JOINED: No players found in response");
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
            // Debug.Log("=== UpdateGameStateFromResponse ===");
            // Debug.Log("Raw JSON: " + jsonString);
            
            // Check if this is wrapped in a "gameState" object
            string gameStateJson = ExtractGameStateJson(jsonString);
            if (gameStateJson != "")
            {
                // Debug.Log("Extracted gameState JSON: " + gameStateJson);
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
            
            Debug.Log("GAME STATE UPDATE: Player: '" + currentPlayer + "', Players found: " + (players != null ? players.Count : 0));
            
            EnqueueMainThreadAction(delegate() {
                // Update current player
                if (currentPlayerText != null && currentPlayer != null && currentPlayer != "")
                {
                    currentPlayerText.text = "Current Player: " + currentPlayer;
                    // Debug.Log("Updated current player text to: " + currentPlayer);
                    
                    // Highlight current player in the position manager
                    if (playerPositionManager != null)
                    {
                        playerPositionManager.HighlightCurrentPlayer(currentPlayer);
                    }
                }
                else
                {
                    // Debug.LogWarning("Current player is empty or null: '" + currentPlayer + "'");
                }
                
                // Update current suit
                if (currentSuitText != null && currentSuit != null && currentSuit != "")
                {
                    currentSuitText.text = "Current Suit: " + currentSuit;
                    // Debug.Log("Updated current suit text to: " + currentSuit);
                }
                
                // Update top card display
                if (topCardImage != null && currentCard != null && currentCard != "")
                {
                    // Call the UpdateTopCard method to properly update the card display
                    UpdateTopCard(currentCard);
                    topCardImage.gameObject.SetActive(true);
                    // Debug.Log("Updated top card to: " + currentCard);
                }
                
                // Update deck count
                if (deckCountText != null)
                {
                    deckCountText.text = "Cards Left: " + deckCount;
                    // Debug.Log("Updated deck count text to: " + deckCount);
                }
                
                // Update players list if we have players
                if (players != null && players.Count > 0)
                {
                    Debug.Log("CALLING UpdatePlayersList from game state update with " + players.Count + " players");
                    UpdatePlayersList(players.ToArray());
                }
                else
                {
                    Debug.LogWarning("No players found in game state update!");
                }
                
                // Debug.Log("Game state updated - Player: " + currentPlayer + ", Suit: " + currentSuit + ", Deck: " + deckCount);
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
                string isFirstPlayerStr = ExtractJsonValue(cleanStr, "isFirstPlayer");
                
                int cardCount = 0;
                int.TryParse(cardCountStr, out cardCount);
                
                bool isFirstPlayer = false;
                bool.TryParse(isFirstPlayerStr, out isFirstPlayer);
                
                if (!string.IsNullOrEmpty(name))
                {
                    playersList.Add(new PlayerData { name = name, cardCount = cardCount, isFirstPlayer = isFirstPlayer });
                    Debug.Log("✓ Added player: " + name + " (" + cardCount + " cards)" + (isFirstPlayer ? " [FIRST PLAYER]" : ""));
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
        Debug.Log("=== SHOWING SCREEN: " + screenName + " ===");
        
        // Clear player displays when switching away from game screen
        if (screenName != "game" && playerPositionManager != null)
        {
            playerPositionManager.ClearPlayerDisplays();
            Debug.Log("Cleared player displays when switching to " + screenName);
        }
        
        // Hide all screens
        if (startScreen != null) 
        {
            startScreen.SetActive(false);
            Debug.Log("StartScreen set to inactive");
        }
        if (lobbyScreen != null) 
        {
            lobbyScreen.SetActive(false);
            Debug.Log("LobbyScreen set to inactive");
        }
        if (gameScreen != null) 
        {
            gameScreen.SetActive(false);
            Debug.Log("GameScreen set to inactive");
        }
        if (gameOverScreen != null) 
        {
            gameOverScreen.SetActive(false);
            Debug.Log("GameOverScreen set to inactive");
        }
        
        // Show target screen
        switch (screenName)
        {
            case "start":
                if (startScreen != null) 
                {
                    startScreen.SetActive(true);
                    Debug.Log("StartScreen activated");
                    
                    // Additional debugging for start screen button
                    if (startScreenButton != null)
                    {
                        Debug.Log("StartScreenButton state after screen activation:");
                        Debug.Log("- Active in hierarchy: " + startScreenButton.gameObject.activeInHierarchy);
                        Debug.Log("- Interactable: " + startScreenButton.interactable);
                        Debug.Log("- GameObject name: " + startScreenButton.gameObject.name);
                    }
                }
                break;
            case "lobby":
                if (lobbyScreen != null) 
                {
                    lobbyScreen.SetActive(true);
                    Debug.Log("LobbyScreen activated");
                }
                break;
            case "game":
                if (gameScreen != null) 
                {
                    gameScreen.SetActive(true);
                    Debug.Log("GameScreen activated");
                }
                break;
            case "game-over":
                if (gameOverScreen != null) 
                {
                    gameOverScreen.SetActive(true);
                    Debug.Log("GameOverScreen activated");
                }
                break;
        }
        
        Debug.Log("=== SCREEN SWITCH COMPLETE ===");
    }
    
    void UpdatePlayersList(PlayerData[] newPlayers)
    {
        Debug.Log("======= PLAYER DISPLAY DEBUG START =======");
        Debug.Log("UpdatePlayersList called with " + newPlayers.Length + " players");
        
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
        
        // Check PlayerPositionManager assignment
        Debug.Log("PlayerPositionManager assigned: " + (playerPositionManager != null));
        
        // Use PlayerPositionManager ONLY when game screen is active
        if (gameScreen != null && gameScreen.activeInHierarchy)
        {
            if (playerPositionManager != null)
            {
                Debug.Log("Game screen is active - Calling PlayerPositionManager.UpdatePlayersDisplay...");
                playerPositionManager.UpdatePlayersDisplay(limitedPlayers);
                Debug.Log("PlayerPositionManager.UpdatePlayersDisplay completed");
            }
            else
            {
                Debug.LogError("CRITICAL: PlayerPositionManager is null! Please assign it in the Inspector.");
            }
        }
        else
        {
            Debug.Log("Game screen not active - skipping PlayerPositionManager.UpdatePlayersDisplay");
        }
        
        // Still update lobby screen for compatibility
        UpdateLobbyPlayersList(limitedPlayers);
        
        Debug.Log("======= PLAYER DISPLAY DEBUG END =======");
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
    public bool isFirstPlayer;
}

[Serializable]
public class PlayerJoinedData
{
    public string playerName;     // The name of the player who just joined
    public PlayerData[] players;  // Array of all players in the room
}