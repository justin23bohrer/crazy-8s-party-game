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
    public Image colorChangerBackground; // Reference to the ColorChangerBackground Image
    private Color originalBackgroundColor;
    
    [Header("8 Card Spiral Animation")]
    public MonoBehaviour spiralAnimationController; // Reference to the spectacular spiral animation system
    
    [Header("Card Colors for Background")]
    public Color redColor = new Color(0.8f, 0.1f, 0.1f, 0.7f);
    public Color blueColor = new Color(0.1f, 0.1f, 0.8f, 0.7f);
    public Color greenColor = new Color(0.1f, 0.6f, 0.1f, 0.7f);
    public Color yellowColor = new Color(0.8f, 0.8f, 0.1f, 0.7f);
    
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
        
        // CRITICAL FOR APPLE TV: Ensure the game runs independently of mouse/keyboard input
        // This prevents Unity from throttling when there's no desktop interaction
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Debug.Log("✅ APPLE TV MODE: Set runInBackground=true and sleepTimeout=NeverSleep");
        
        // Force Unity to maintain consistent frame rate regardless of input
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // Disable VSync to ensure consistent updates
        Debug.Log("✅ APPLE TV MODE: Set targetFrameRate=60, disabled VSync");
        
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
        
        // Initialize background Image for color effects
        if (colorChangerBackground == null)
        {
            // Try to find it automatically
            GameObject bgObject = GameObject.Find("ColorChangerBackground");
            if (bgObject != null)
            {
                colorChangerBackground = bgObject.GetComponent<Image>();
            }
        }
        
        // Store original background color
        if (colorChangerBackground != null)
        {
            originalBackgroundColor = colorChangerBackground.color;
            Debug.Log("Stored original background color: " + originalBackgroundColor);
        }
        else
        {
            Debug.LogWarning("ColorChangerBackground Image not found for background effects");
        }
        
        // Initialize Spiral Animation Controller for spectacular 8 card animations
        Debug.Log("=== INITIALIZING SPIRAL ANIMATION CONTROLLER ===");
        
        if (spiralAnimationController == null)
        {
            Debug.Log("SpiralAnimationController field is null - searching for existing instances...");
            
            // Try to find it automatically using component name
            MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            Debug.Log($"Found {allComponents.Length} total MonoBehaviour components in scene");
            
            foreach (MonoBehaviour comp in allComponents)
            {
                Debug.Log($"Checking component: {comp.GetType().Name} on {comp.gameObject.name}");
                if (comp.GetType().Name == "SpiralAnimationController")
                {
                    spiralAnimationController = comp;
                    Debug.Log("✅ Found existing SpiralAnimationController!");
                    break;
                }
            }
            
            if (spiralAnimationController == null)
            {
                Debug.Log("❌ No existing SpiralAnimationController found - creating one now...");
                
                // Create it immediately instead of waiting
                if (TryCreateSpiralAnimationController())
                {
                    Debug.Log("✅ Successfully created SpiralAnimationController during initialization!");
                }
                else
                {
                    Debug.LogError("❌ Failed to create SpiralAnimationController during initialization!");
                }
            }
            else
            {
                Debug.Log($"✅ SpiralAnimationController found: {spiralAnimationController.GetType().Name}");
            }
        }
        else
        {
            Debug.Log($"✅ SpiralAnimationController already assigned: {spiralAnimationController.GetType().Name}");
        }
        
        // Set initial room code text
        if (roomCodeText != null)
        {
            roomCodeText.text = "----";
        }
        
        // Set up ESSENTIAL button - START BUTTON for room creation (Apple TV compatible)
        Debug.Log("Setting up START BUTTON for room creation...");
        
        if (startScreenButton != null)
        {
            Debug.Log("StartScreenButton found, adding OnStartButtonClicked listener");
            startScreenButton.onClick.AddListener(() => {
                Debug.Log("*** START BUTTON PRESSED - CREATING ROOM ***");
                OnStartButtonClicked();
            });
            Debug.Log("StartScreenButton listener added successfully");
            
            // APPLE TV: Ensure START button always works (critical for room creation)
            EnsureStartButtonWorksForAppleTV();
        }
        else
        {
            Debug.LogError("CRITICAL: StartScreenButton is NULL! Cannot create rooms!");
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
    
    /// <summary>
    /// APPLE TV: Ensure START button works perfectly for room creation (critical functionality)
    /// </summary>
    void EnsureStartButtonWorksForAppleTV()
    {
        Debug.Log("=== ENSURING START BUTTON WORKS FOR APPLE TV ===");
        
        if (startScreenButton != null)
        {
            // Force start button to be interactable regardless of Canvas Group states
            startScreenButton.interactable = true;
            
            // Check and fix all Canvas Groups in hierarchy that might block START button
            CanvasGroup[] allCanvasGroups = startScreenButton.GetComponentsInParent<CanvasGroup>();
            Debug.Log($"Found {allCanvasGroups.Length} CanvasGroups in START button hierarchy");
            
            foreach (CanvasGroup cg in allCanvasGroups)
            {
                if (cg != null)
                {
                    Debug.Log($"Enabling CanvasGroup on {cg.gameObject.name} for START button");
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                    cg.alpha = 1f;
                }
            }
            
            // Also fix Canvas Groups on the start screen itself (critical for room creation)
            if (startScreen != null)
            {
                CanvasGroup[] screenCanvasGroups = startScreen.GetComponentsInChildren<CanvasGroup>();
                Debug.Log($"Fixing {screenCanvasGroups.Length} CanvasGroups in start screen for room creation");
                foreach (CanvasGroup cg in screenCanvasGroups)
                {
                    if (cg != null)
                    {
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                        cg.alpha = 1f;
                    }
                }
            }
            
            Debug.Log("✅ APPLE TV START button ready for room creation");
        }
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
        // APPLE TV MODE: Essential operations only
        // Focus: Phone-controlled gameplay + START button for room creation
        
        // Process phone events immediately (critical for gameplay)
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                try
                {
                    System.Action action = mainThreadActions.Dequeue();
                    if (action != null)
                    {
                        action.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error executing phone event: " + e.Message);
                }
            }
        }
        
        // Periodic status check - every 30 seconds (minimal logging for Apple TV)
        if (Time.time % 30f < Time.deltaTime)
        {
            Debug.Log($"🍎 APPLE TV STATUS: Running at {1f/Time.deltaTime:F0} FPS");
            
            // Verify socket connection for phone communications
            if (socket != null && !socket.Connected)
            {
                Debug.LogWarning("📱 Phone connection lost - phones cannot control game");
            }
        }
        
        // Ensure START button stays functional for room creation (check every 10 seconds)
        if (Time.time % 10f < Time.deltaTime && startScreen != null && startScreen.activeInHierarchy)
        {
            if (startScreenButton != null && !startScreenButton.interactable)
            {
                Debug.LogWarning("🍎 START button became non-interactable - fixing for room creation");
                EnsureStartButtonWorksForAppleTV();
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
        
        if (roomCodeBorderCycler == null) { Debug.LogWarning("RoomCodeBorderCycler is not assigned - room code border won't cycle colors"); }
        else { Debug.Log("✓ RoomCodeBorderCycler assigned"); }
        
        // Game UI
        if (currentPlayerText == null) { Debug.LogError("CurrentPlayerText is not assigned!"); valid = false; }
        else { Debug.Log("✓ CurrentPlayerText assigned"); }
        
        if (currentColorText == null) { Debug.LogError("CurrentColorText is not assigned!"); valid = false; }
        else { Debug.Log("✓ CurrentColorText assigned"); }
        
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
            socket.On("color-chosen", new Action<SocketIOResponse>(HandleColorChosen));
            
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
                if (currentColorText != null) currentColorText.text = "Current Color: --";
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
                if (currentColorText != null) currentColorText.text = "Current Color: --";
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
            roomCode = "----";
            roomCodeText.text = roomCode;
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
    
    // Test method for 8 card color change - can be called from Inspector
    [ContextMenu("Test 8 Card Yellow Color Change")]
    public void TestEightCardYellowChange()
    {
        if (topCardImage != null)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController != null)
            {
                Debug.Log("Testing: Setting 8 card to spiral first");
                cardController.SetCard("red", 8); // First show spiral
                
                // Wait a moment then change to yellow
                StartCoroutine(ChangeToYellowAfterDelay(cardController));
            }
            else
            {
                Debug.LogError("No CardController found on topCardImage!");
            }
        }
        else
        {
            Debug.LogError("topCardImage is not assigned!");
        }
    }
    
    /// <summary>
    /// Force assignment of SpiralAnimationController
    /// </summary>
    [ContextMenu("Force Find Spiral Animation Controller")]
    public void ForceFindSpiralAnimationController()
    {
        Debug.Log("=== FORCE FINDING SPIRAL ANIMATION CONTROLLER ===");
        
        // Method 1: Direct type search
        SpiralAnimationController foundController = FindFirstObjectByType<SpiralAnimationController>();
        if (foundController != null)
        {
            spiralAnimationController = foundController;
            Debug.Log($"✅ Found SpiralAnimationController directly: {foundController.gameObject.name}");
            return;
        }
        
        // Method 2: Search through all MonoBehaviours
        MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        Debug.Log($"Searching through {allComponents.Length} components...");
        
        foreach (MonoBehaviour comp in allComponents)
        {
            if (comp.GetType().Name == "SpiralAnimationController")
            {
                spiralAnimationController = comp;
                Debug.Log($"✅ Found SpiralAnimationController: {comp.gameObject.name}");
                return;
            }
        }
        
        Debug.LogWarning("❌ No SpiralAnimationController found in scene!");
    }
    
    /// <summary>
    /// Test the 8 card detection and spiral animation trigger manually
    /// </summary>
    [ContextMenu("Test 8 Card Color Choice")]
    public void TestEightCardColorChoice()
    {
        Debug.Log("=== TESTING 8 CARD COLOR CHOICE ===");
        
        // First ensure we have a SpiralAnimationController
        if (spiralAnimationController == null)
        {
            Debug.Log("SpiralAnimationController not assigned - attempting to find...");
            ForceFindSpiralAnimationController();
        }
        
        if (spiralAnimationController == null)
        {
            Debug.LogError("❌ No SpiralAnimationController available for test!");
            return;
        }
        
        // Set up an 8 card on the top card
        if (topCardImage != null)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController != null)
            {
                Debug.Log("Setting up 8 card for test...");
                cardController.SetCard("red", 8); // Set to red 8 first
                
                // Simulate the color choice event for an 8 card
                Debug.Log("Simulating color choice event for 8 card...");
                
                // Create a mock JSON response similar to what the server would send
                string testJson = "{\"color\":\"yellow\",\"playerName\":\"TestPlayer\",\"card\":{\"rank\":\"8\",\"value\":8,\"color\":\"red\"}}";
                
                // Manually call the color chosen handler logic
                Debug.Log("Testing 8 card detection with test JSON: " + testJson);
                
                // Extract the data
                string color = "yellow";
                string cardInfo = "{\"rank\":\"8\",\"value\":8,\"color\":\"red\"}";
                
                bool isForEightCard = false;
                
                // Test the card parsing logic
                try
                {
                    var cardData = JsonUtility.FromJson<CardData>(cardInfo);
                    if (cardData != null)
                    {
                        bool isEightByRank = cardData.rank == "8";
                        bool isEightByValue = cardData.value == 8;
                        isForEightCard = isEightByRank || isEightByValue;
                        
                        Debug.Log($"Card parsed - rank: '{cardData.rank}', value: {cardData.value}");
                        Debug.Log($"isEightByRank: {isEightByRank}, isEightByValue: {isEightByValue}");
                        Debug.Log($"isForEightCard: {isForEightCard}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Card parsing failed: " + ex.Message);
                }
                
                // If this is detected as an 8 card, trigger the animation
                if (isForEightCard)
                {
                    Debug.Log("✅ 8 card detected! Triggering spiral animation...");
                    
                    try
                    {
                        var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                        if (method != null)
                        {
                            method.Invoke(spiralAnimationController, new object[] { cardController, color });
                            Debug.Log("✅ Spiral animation triggered for 8 card color choice!");
                        }
                        else
                        {
                            Debug.LogError("❌ TriggerSpiralAnimation method not found!");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("❌ Error triggering spiral animation: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogError("❌ 8 card was NOT detected - animation will not trigger!");
                }
            }
            else
            {
                Debug.LogError("No CardController found on topCardImage!");
            }
        }
        else
        {
            Debug.LogError("topCardImage is not assigned!");
        }
    }
    
    System.Collections.IEnumerator ChangeToYellowAfterDelay(CardController cardController)
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds
        Debug.Log("Testing: Now changing 8 card to yellow");
        cardController.SetCard("yellow", 8, true); // Force yellow color
    }
    
    // Test current chosen color state
    [ContextMenu("Check Chosen 8 Card Color")]
    public void CheckChosenEightCardColor()
    {
        Debug.Log($"Current chosenEightCardColor: {chosenEightCardColor ?? "null"}");
        if (topCardImage != null)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController != null)
            {
                Debug.Log("CardController found on topCardImage");
            }
            else
            {
                Debug.Log("No CardController on topCardImage");
            }
        }
        else
        {
            Debug.Log("topCardImage is null");
        }
    }
    
    /// <summary>
    /// Test method to verify start button works for room creation
    /// </summary>
    [ContextMenu("Test Start Button (Room Creation)")]
    public void TestStartButton()
    {
        Debug.Log("=== TESTING START BUTTON FOR ROOM CREATION ===");
        
        if (startScreenButton != null)
        {
            Debug.Log("✅ Start button found - simulating press for room creation");
            
            // Ensure it's properly set up first
            EnsureStartButtonWorksForAppleTV();
            
            // Simulate button press
            Debug.Log("Simulating START button press...");
            OnStartButtonClicked();
            
            Debug.Log("✅ Start button test complete - check if room creation was triggered");
        }
        else
        {
            Debug.LogError("❌ Start button is NULL - cannot create rooms!");
        }
    }
    public void TestAppleTVAnimation()
    {
        Debug.Log("=== TESTING APPLE TV PHONE-CONTROLLED ANIMATION ===");
        Debug.Log("This simulates the complete flow as if controlled by phone");
        
        if (topCardImage != null)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController == null)
            {
                cardController = topCardImage.gameObject.AddComponent<CardController>();
            }
            
            // Simulate the exact sequence that happens when phone controls the game:
            // 1. 8 card is played (from phone)
            Debug.Log("📱 PHONE ACTION: Player played an 8 card");
            cardController.SetCard("red", 8);
            
            // 2. Phone sends color choice
            Debug.Log("📱 PHONE ACTION: Player chose yellow color");
            chosenEightCardColor = "yellow";
            
            // 3. Trigger animation immediately (as it would happen from phone event)
            Debug.Log("🎬 APPLE TV: Triggering animation sequence...");
            TriggerAppleTVAnimation(cardController, "yellow");
        }
        else
        {
            Debug.LogError("❌ topCardImage is null - cannot test animation");
        }
    }
    
    /// <summary>
    /// APPLE TV: Direct animation trigger that works without any input dependencies
    /// </summary>
    public void TriggerAppleTVAnimation(CardController cardController, string targetColor)
    {
        Debug.Log($"=== APPLE TV ANIMATION TRIGGER ===");
        Debug.Log($"Target: {targetColor}, Time: {Time.time}");
        
        if (isWaitingForEightCardAnimation)
        {
            Debug.LogWarning("Animation already in progress - ignoring duplicate trigger");
            return;
        }
        
        isWaitingForEightCardAnimation = true;
        
        // Use InvokeRepeating for more reliable timing (Apple TV compatible)
        Debug.Log("Using Apple TV compatible timing method...");
        Invoke(nameof(ExecuteAppleTVAnimationStep1), 1f); // Show card for 1 second
        
        // Store animation data for the delayed execution
        pendingAnimationCardController = cardController;
        pendingAnimationColor = targetColor;
    }
    
    // Variables for Apple TV animation
    private CardController pendingAnimationCardController;
    private string pendingAnimationColor;
    
    /// <summary>
    /// APPLE TV: Animation step 1 - executed after 1 second delay
    /// </summary>
    public void ExecuteAppleTVAnimationStep1()
    {
        Debug.Log("=== APPLE TV ANIMATION STEP 1: TRIGGERING SPIRAL ===");
        
        if (pendingAnimationCardController == null || string.IsNullOrEmpty(pendingAnimationColor))
        {
            Debug.LogError("❌ Pending animation data is invalid");
            isWaitingForEightCardAnimation = false;
            return;
        }
        
        // Try to trigger spiral animation directly
        bool animationTriggered = false;
        
        if (spiralAnimationController != null)
        {
            try
            {
                var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                if (method != null)
                {
                    Debug.Log("✅ Triggering spiral animation for Apple TV...");
                    method.Invoke(spiralAnimationController, new object[] { pendingAnimationCardController, pendingAnimationColor });
                    animationTriggered = true;
                    Debug.Log("✅ Apple TV spiral animation triggered successfully!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ Apple TV animation error: " + ex.Message);
            }
        }
        
        if (!animationTriggered)
        {
            // Fallback: Direct card transformation
            Debug.LogWarning("Spiral animation failed - using direct transformation");
            pendingAnimationCardController.SetCard(pendingAnimationColor, 8, true);
        }
        
        // Clean up and reset
        pendingAnimationCardController = null;
        pendingAnimationColor = null;
        isWaitingForEightCardAnimation = false;
        
        Debug.Log("=== APPLE TV ANIMATION COMPLETE ===");
    }
    
    /// <summary>
    /// Simulate receiving a color choice for testing
    /// </summary>
    private System.Collections.IEnumerator SimulateColorChoice()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("Simulating color choice: yellow");
        chosenEightCardColor = "yellow";
        
        // Trigger the animation sequence
        if (topCardImage != null && !isWaitingForEightCardAnimation)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController != null)
            {
                Debug.Log("✅ Triggering simplified 8 card animation for yellow");
                isWaitingForEightCardAnimation = true;
                StartCoroutine(DelayedEightCardAnimation(cardController, "yellow"));
            }
        }
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
        
        // Reset background color when restarting
        ResetBackgroundColor();
        
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
            
            // Extract the card that was played to change background color
            string playedCard = ExtractPlayedCardFromJson(jsonString);
            if (!string.IsNullOrEmpty(playedCard))
            {
                string cardColor = ExtractCardColor(playedCard);
                if (!string.IsNullOrEmpty(cardColor))
                {
                    Debug.Log("Card played: " + playedCard + ", changing background to " + cardColor);
                    EnqueueMainThreadAction(() => ChangeBackgroundToCardColor(cardColor));
                }
            }
            
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
    
    void HandleColorChosen(SocketIOResponse response)
    {
        try
        {
            Debug.Log("=== COLOR CHOSEN EVENT RECEIVED (PHONE CONTROLLED) ===");
            string jsonString = response.GetValue().ToString();
            Debug.Log("Color chosen JSON: " + jsonString);
            
            string color = ExtractJsonValue(jsonString, "color");
            string playerName = ExtractJsonValue(jsonString, "playerName");
            
            Debug.Log($"📱 PHONE INPUT: Color='{color}', Player='{playerName}'");
            
            // APPLE TV MODE: Process phone input immediately without any desktop dependencies
            if (!string.IsNullOrEmpty(color))
            {
                chosenEightCardColor = color;
                Debug.Log($"✅ APPLE TV: Stored chosen color: {chosenEightCardColor}");
                
                // IMMEDIATE PROCESSING - NO QUEUES, NO MOUSE DEPENDENCIES
                Debug.Log("=== APPLE TV: PROCESSING PHONE INPUT IMMEDIATELY ===");
                
                // Update UI immediately
                if (currentColorText != null)
                {
                    currentColorText.text = "Current Color: " + color;
                    Debug.Log($"📺 APPLE TV: Updated display text for {playerName} chose {color}");
                }
                
                // Update background immediately
                ChangeBackgroundToCardColor(color);
                Debug.Log($"📺 APPLE TV: Background changed to {color}");
                
                // Trigger animation immediately if we have an 8 card
                if (topCardImage != null && !isWaitingForEightCardAnimation)
                {
                    CardController cardController = topCardImage.GetComponent<CardController>();
                    if (cardController != null)
                    {
                        Debug.Log("📺 APPLE TV: 8 card detected - triggering phone-controlled animation");
                        
                        // Use Apple TV compatible animation trigger
                        TriggerAppleTVAnimation(cardController, color);
                    }
                    else
                    {
                        Debug.LogWarning("📺 APPLE TV: No CardController found");
                    }
                }
                
                Debug.Log("=== APPLE TV: PHONE INPUT PROCESSING COMPLETE ===");
            }
            else
            {
                Debug.LogWarning("📱 PHONE INPUT: No color in response");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("📺 APPLE TV ERROR handling phone color choice: " + e.Message);
        }
    }
    
    /// <summary>
    /// Check if we can trigger the 8 card animation (current card is an 8 and not already animating)
    /// </summary>
    private bool CanTriggerEightCardAnimation()
    {
        if (isWaitingForEightCardAnimation)
        {
            Debug.Log("Animation already in progress");
            return false;
        }
        
        if (topCardImage == null)
        {
            Debug.Log("No top card image");
            return false;
        }
        
        CardController cardController = topCardImage.GetComponent<CardController>();
        if (cardController == null)
        {
            Debug.Log("No card controller");
            return false;
        }
        
        // For simplicity, we'll assume if we received a color-chosen event, 
        // it's because an 8 was played. The server should only send this when appropriate.
        Debug.Log("✅ Can trigger 8 card animation");
        return true;
    }
    
    /// <summary>
    /// Try to create a SpiralAnimationController dynamically using reflection
    /// </summary>
    /// <returns>True if successfully created, false otherwise</returns>
    bool TryCreateSpiralAnimationController()
    {
        try
        {
            Debug.Log("=== ATTEMPTING TO CREATE SPIRAL ANIMATION CONTROLLER ===");
            
            // Look for the SpiralAnimationController type
            System.Type spiralType = null;
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            
            Debug.Log($"Searching through {assemblies.Length} assemblies for SpiralAnimationController...");
            
            foreach (var assembly in assemblies)
            {
                Debug.Log($"Checking assembly: {assembly.FullName}");
                try 
                {
                    spiralType = assembly.GetType("SpiralAnimationController");
                    if (spiralType != null) 
                    {
                        Debug.Log($"✅ Found SpiralAnimationController type in assembly: {assembly.FullName}");
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error checking assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            if (spiralType != null)
            {
                Debug.Log("✅ Found SpiralAnimationController type - creating instance...");
                Debug.Log($"Type full name: {spiralType.FullName}");
                Debug.Log($"Type assembly: {spiralType.Assembly.FullName}");
                
                GameObject spiralControllerObject = new GameObject("SpiralAnimationController");
                Debug.Log($"Created GameObject: {spiralControllerObject.name}");
                
                spiralAnimationController = (MonoBehaviour)spiralControllerObject.AddComponent(spiralType);
                Debug.Log($"Added component of type: {spiralAnimationController.GetType().Name}");
                Debug.Log("✅ SpiralAnimationController created successfully!");
                
                // Verify the component has the method we need
                var triggerMethod = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                Debug.Log($"TriggerSpiralAnimation method available: {triggerMethod != null}");
                
                return true;
            }
            else
            {
                Debug.LogWarning("❌ SpiralAnimationController type not found in any assembly");
                
                // List all MonoBehaviour types in assemblies for debugging
                Debug.Log("Available MonoBehaviour types in assemblies:");
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && t.Name.Contains("Spiral"));
                        foreach (var type in types)
                        {
                            Debug.Log($"  Found type: {type.FullName}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Error listing types in {assembly.FullName}: {ex.Message}");
                    }
                }
                
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Error creating SpiralAnimationController dynamically: " + ex.Message);
            Debug.LogError("Exception stack trace: " + ex.StackTrace);
            return false;
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
            string currentColor = ExtractJsonValue(jsonString, "currentColor");
            
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
                
                // Update current color
                if (currentColorText != null && currentColor != null && currentColor != "")
                {
                    currentColorText.text = "Current Color: " + currentColor;
                    // Debug.Log("Updated current color text to: " + currentColor);
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
                
                // Debug.Log("Game state updated - Player: " + currentPlayer + ", Color: " + currentColor + ", Deck: " + deckCount);
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
            // Look for "topCard" object and extract rank and color
            string topCardPattern = "\"topCard\":{";
            int startIdx = json.IndexOf(topCardPattern);
            if (startIdx == -1) return "";
            
            int endIdx = json.IndexOf("}", startIdx);
            if (endIdx == -1) return "";
            
            string topCardJson = json.Substring(startIdx, endIdx - startIdx + 1);
            
            string rank = ExtractJsonValue(topCardJson, "rank");
            string color = ExtractJsonValue(topCardJson, "color");
            
            if (rank != "" && color != "")
            {
                return rank + " of " + color;
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
    
    // Extract the played card information from card-played event JSON
    string ExtractPlayedCardFromJson(string json)
    {
        try
        {
            // Look for "card" field in the JSON
            string cardJson = ExtractJsonValue(json, "card");
            if (string.IsNullOrEmpty(cardJson))
            {
                // Try extracting from gameState.topCard if direct card field not found
                string gameStateJson = ExtractGameStateJson(json);
                if (!string.IsNullOrEmpty(gameStateJson))
                {
                    string topCardJson = ExtractCurrentCardFromJson(gameStateJson);
                    return topCardJson;
                }
            }
            return cardJson;
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting played card: " + e.Message);
            return "";
        }
    }
    
    // Extract color from card string (format: "5 red", "J blue", etc.)
    string ExtractCardColor(string cardString)
    {
        try
        {
            if (string.IsNullOrEmpty(cardString))
                return "";
                
            string[] parts = cardString.Split(' ');
            if (parts.Length >= 2)
            {
                string color = parts[parts.Length - 1].ToLower(); // Last part should be color
                
                // Validate it's a valid color
                if (color == "red" || color == "blue" || color == "green" || color == "yellow")
                {
                    return color;
                }
            }
            
            Debug.LogWarning("Could not extract valid color from card: " + cardString);
            return "";
        }
        catch (Exception e)
        {
            Debug.LogError("Error extracting card color: " + e.Message);
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
        
        // Reset background color when leaving game screen
        if (screenName != "game")
        {
            ResetBackgroundColor();
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
                    
                    // Stop border cycling when not in lobby
                    if (roomCodeBorderCycler != null)
                    {
                        roomCodeBorderCycler.StopCycling();
                    }
                    
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
                    
                    // Start the beautiful color cycling effect!
                    if (roomCodeBorderCycler != null)
                    {
                        roomCodeBorderCycler.StartCycling();
                        Debug.Log("Started room code border color cycling");
                    }
                }
                break;
            case "game":
                if (gameScreen != null) 
                {
                    gameScreen.SetActive(true);
                    Debug.Log("GameScreen activated");
                    
                    // Stop border cycling when in game
                    if (roomCodeBorderCycler != null)
                    {
                        roomCodeBorderCycler.StopCycling();
                    }
                }
                break;
            case "game-over":
                if (gameOverScreen != null) 
                {
                    gameOverScreen.SetActive(true);
                    Debug.Log("GameOverScreen activated");
                    
                    // Stop border cycling when in game over
                    if (roomCodeBorderCycler != null)
                    {
                        roomCodeBorderCycler.StopCycling();
                    }
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
    
    // Track if we have a chosen color for the current 8 card
    private string chosenEightCardColor = null;
    private bool isWaitingForEightCardAnimation = false;
    
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
                
                // Reset chosen color if this is not an 8 card
                if (cardData.value != 8)
                {
                    chosenEightCardColor = null;
                    isWaitingForEightCardAnimation = false;
                    Debug.Log("Reset chosen 8 card color (new card is not an 8)");
                }
                
                // SIMPLIFIED 8 CARD HANDLING: Show card for 1 second, then animate to chosen color
                if (cardData.value == 8 && chosenEightCardColor != null && !isWaitingForEightCardAnimation)
                {
                    Debug.Log($"=== 8 CARD WITH CHOSEN COLOR DETECTED ===");
                    Debug.Log($"8 card will be shown for 1 second, then animated to: {chosenEightCardColor}");
                    
                    // First show the basic 8 card (spiral image)
                    cardController.SetCard(cardData.color, 8);
                    Debug.Log($"Showing basic 8 card for 1 second...");
                    
                    // Start the animation sequence: wait 1 second, then animate
                    isWaitingForEightCardAnimation = true;
                    StartCoroutine(DelayedEightCardAnimation(cardController, chosenEightCardColor));
                }
                else if (cardData.value == 8 && chosenEightCardColor == null)
                {
                    // Show basic 8 card (waiting for color choice)
                    cardController.SetCard(cardData.color, 8);
                    Debug.Log($"Showing 8 card, waiting for color choice...");
                }
                else
                {
                    // Regular card (not an 8)
                    cardController.SetCard(cardData.color, cardData.value);
                    Debug.Log($"Called CardController.SetCard with: {cardData.color}, {cardData.value}");
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
    
    /// <summary>
    /// Coroutine to handle the 8 card animation sequence: wait 1 second, then trigger spiral animation
    /// </summary>
    private System.Collections.IEnumerator DelayedEightCardAnimation(CardController cardController, string targetColor)
    {
        Debug.Log($"=== STARTING DELAYED 8 CARD ANIMATION SEQUENCE ===");
        Debug.Log($"Target color: {targetColor}");
        Debug.Log($"Time.time at start: {Time.time}");
        Debug.Log($"GameManager enabled: {enabled}");
        Debug.Log($"GameManager active: {gameObject.activeInHierarchy}");
        
        // Wait for 1 second to show the basic 8 card
        Debug.Log("Starting 1 second wait...");
        float startTime = Time.time;
        yield return new WaitForSeconds(1f);
        float endTime = Time.time;
        Debug.Log($"1 second wait complete! Start: {startTime}, End: {endTime}, Actual duration: {endTime - startTime}");
        
        Debug.Log("Now triggering spectacular spiral animation!");
        
        // Ensure we still have valid references
        if (cardController == null)
        {
            Debug.LogError("❌ CardController became null during wait!");
            isWaitingForEightCardAnimation = false;
            yield break;
        }
        
        if (string.IsNullOrEmpty(targetColor))
        {
            Debug.LogError("❌ Target color became null during wait!");
            isWaitingForEightCardAnimation = false;
            yield break;
        }
        
        // Try to trigger the spiral animation
        bool animationTriggered = false;
        
        if (spiralAnimationController != null)
        {
            Debug.Log($"SpiralAnimationController found: {spiralAnimationController.GetType().Name}");
            
            try
            {
                var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                if (method != null)
                {
                    Debug.Log("✅ Triggering TriggerSpiralAnimation method...");
                    method.Invoke(spiralAnimationController, new object[] { cardController, targetColor });
                    Debug.Log("✅ Spiral animation triggered successfully!");
                    animationTriggered = true;
                }
                else
                {
                    Debug.LogWarning("❌ TriggerSpiralAnimation method not found");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ Error calling TriggerSpiralAnimation: " + ex.Message);
                Debug.LogError("Stack trace: " + ex.StackTrace);
            }
        }
        else
        {
            Debug.LogWarning("SpiralAnimationController is null - attempting to create...");
            
            if (TryCreateSpiralAnimationController())
            {
                Debug.Log("✅ Successfully created SpiralAnimationController - retrying animation...");
                
                try
                {
                    var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                    if (method != null)
                    {
                        Debug.Log("✅ Triggering animation with newly created controller...");
                        method.Invoke(spiralAnimationController, new object[] { cardController, targetColor });
                        Debug.Log("✅ Animation triggered with new controller!");
                        animationTriggered = true;
                    }
                    else
                    {
                        Debug.LogWarning("❌ Method not found on new controller");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ Error with new controller: " + ex.Message);
                }
            }
            else
            {
                Debug.LogWarning("❌ Could not create SpiralAnimationController");
            }
        }
        
        // Fallback if animation didn't trigger
        if (!animationTriggered)
        {
            Debug.LogWarning("❌ Spiral animation failed to trigger - using direct transformation");
            cardController.SetCard(targetColor, 8, true);
            Debug.Log($"Directly set card to {targetColor} 8");
        }
        
        // Reset the waiting flag
        isWaitingForEightCardAnimation = false;
        Debug.Log("=== DELAYED 8 CARD ANIMATION SEQUENCE COMPLETE ===");
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
        
        // Expected format: "J of red", "8 of blue", "A of green"
        string[] parts = cardString.Split(new string[] { " of " }, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            Debug.LogError($"Invalid card format: '{cardString}'. Expected format: 'RANK of COLOR'");
            return null;
        }
        
        string valueStr = parts[0].Trim();
        string color = parts[1].Trim().ToLower();
        
        Debug.Log($"Parsed parts - Value: '{valueStr}', Color: '{color}'");
        
        // Convert color to lowercase for consistency
        if (color != "red" && color != "blue" && color != "green" && color != "yellow")
        {
            Debug.LogError($"Invalid color: '{color}'. Expected: red, blue, green, or yellow");
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
        
        Debug.Log($"Successfully parsed card: {value} of {color}");
        return new CardData(color, value);
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
    
    // Background color effect methods
    void ChangeBackgroundToCardColor(string cardColor)
    {
        if (colorChangerBackground == null)
        {
            Debug.LogWarning("ColorChangerBackground Image not available for background color change");
            return;
        }
        
        Color newBackgroundColor = GetBackgroundColor(cardColor);
        Debug.Log("Changing background to " + cardColor + " color: " + newBackgroundColor);
        
        // Apply the color with 70% opacity by lerping with the original color
        Color finalColor = Color.Lerp(originalBackgroundColor, newBackgroundColor, 0.7f);
        colorChangerBackground.color = finalColor;
    }
    
    void ResetBackgroundColor()
    {
        if (colorChangerBackground != null)
        {
            colorChangerBackground.color = originalBackgroundColor;
            Debug.Log("Reset background to original color");
        }
    }
    
    public Color GetBackgroundColor(string color)
    {
        switch (color.ToLower())
        {
            case "red":
                return redColor;
            case "blue": 
                return blueColor;
            case "green":
                return greenColor;
            case "yellow":
                return yellowColor;
            default: 
                return redColor;
        }
    }
    
    /// <summary>
    /// Manual test method to verify spiral animation works
    /// Call this from Unity inspector or via debug menu
    /// </summary>
    [ContextMenu("Test 8 Card Spiral Animation")]
    public void TestSpiralAnimation()
    {
        Debug.Log("=== MANUAL SPIRAL ANIMATION TEST ===");
        
        if (topCardImage != null)
        {
            CardController cardController = topCardImage.GetComponent<CardController>();
            if (cardController != null)
            {
                Debug.Log("Found CardController - testing spiral animation with red color...");
                
                if (spiralAnimationController != null)
                {
                    Debug.Log("SpiralAnimationController available - triggering animation...");
                    try
                    {
                        var method = spiralAnimationController.GetType().GetMethod("TriggerSpiralAnimation");
                        if (method != null)
                        {
                            method.Invoke(spiralAnimationController, new object[] { cardController, "red" });
                            Debug.Log("✅ Manual spiral animation triggered!");
                        }
                        else
                        {
                            Debug.LogError("❌ TriggerSpiralAnimation method not found!");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("❌ Error in manual test: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogError("❌ SpiralAnimationController is null!");
                    Debug.Log("Attempting to create it now...");
                    if (TryCreateSpiralAnimationController())
                    {
                        Debug.Log("Created controller - retrying test...");
                        TestSpiralAnimation(); // Recursive call after creation
                    }
                }
            }
            else
            {
                Debug.LogError("❌ No CardController found on topCardImage!");
            }
        }
        else
        {
            Debug.LogError("❌ topCardImage is null!");
        }
    }
    
    /// <summary>
    /// Called by SpiralAnimationController when spiral animation completes
    /// Notifies server to clear animation lock so players can continue
    /// </summary>
    public void OnSpiralAnimationComplete()
    {
        Debug.Log("🎬 Spiral animation completed - notifying server to clear animation lock");
        
        if (socket != null && socket.Connected)
        {
            try
            {
                // Send event to server to clear animation lock
                socket.Emit("animation-complete");
                Debug.Log("✅ Sent animation-complete event to server");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to notify server of animation completion: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot notify server - socket not connected");
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