using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles all socket communication with the backend server
/// Manages room creation, player events, and game state synchronization
/// </summary>
public class NetworkManager : MonoBehaviour
{
    [Header("Connection")]
    public string serverURL = "http://localhost:3000";
    
    private SocketIOUnity socket;
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    // Events
    public event System.Action<string> OnRoomCreated;
    public event System.Action<string> OnGameStarted;
    public event System.Action<PlayerData[]> OnPlayerJoined;
    public event System.Action<string> OnGameOver;
    public event System.Action<string> OnCardPlayed;
    public event System.Action<string> OnColorChosen;
    public event System.Action<string> OnGameStateUpdated;
    
    public void Initialize()
    {
        Debug.Log("🔌 NetworkManager Initialize() called");
        ConnectToServer();
    }
    
    void Update()
    {
        // Process main thread actions from socket events
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
                    Debug.LogError($"NetworkManager action error: {e.Message}");
                }
            }
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
                Debug.LogError($"Socket cleanup error: {e.Message}");
            }
        }
    }
    
    private void ConnectToServer()
    {
        try
        {
            Debug.Log($"🌐 Attempting to connect to server: {serverURL}");
            var uri = new Uri(serverURL);
            socket = new SocketIOUnity(uri);
            
            socket.OnConnected += OnSocketConnected;
            socket.OnDisconnected += OnSocketDisconnected;
            
            // Register event handlers
            socket.On("room-created", HandleRoomCreated);
            socket.On("player-joined", HandlePlayerJoined);
            socket.On("game-started", HandleGameStarted);
            socket.On("card-played", HandleCardPlayed);
            socket.On("card-drawn", HandleCardDrawn);
            socket.On("color-chosen", HandleColorChosen);
            socket.On("winner-detected", HandleWinnerDetected);
            socket.On("game-over", HandleGameOver);
            socket.On("game-state-updated", HandleGameStateUpdated);
            socket.On("host-restart-game", HandleHostRestartGame);
            socket.On("new-room-created", HandleNewRoomCreated);
            socket.On("room-error", HandleRoomError);
            
            Debug.Log("🚀 Starting socket connection...");
            socket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to connect to server: {e.Message}");
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
    
    // Socket event callbacks
    private void OnSocketConnected(object sender, EventArgs e)
    {
        Debug.Log("Connected to server successfully");
    }
    
    private void OnSocketDisconnected(object sender, string e)
    {
        Debug.LogWarning($"Disconnected from server: {e}");
    }
    
    // Public methods for game actions
    public void CreateRoom()
    {
        Debug.Log("🏠 CreateRoom() called");
        
        if (socket == null)
        {
            Debug.LogError("❌ Socket is null! Cannot create room.");
            return;
        }
        
        if (!socket.Connected)
        {
            Debug.LogError("❌ Socket is not connected! Cannot create room.");
            Debug.Log($"Socket connected: {socket.Connected}");
            return;
        }
        
        Debug.Log("📤 Emitting create-room event...");
        socket.Emit("create-room");
    }
    
    public void RestartGame()
    {
        if (socket != null && socket.Connected)
        {
            socket.Emit("host-restart-game");
        }
    }
    
    public void CreateNewRoom()
    {
        if (socket != null && socket.Connected)
        {
            socket.Emit("create-new-room");
        }
    }
    
    public void PlayCard(string cardData)
    {
        if (socket != null && socket.Connected)
        {
            socket.Emit("play-card", cardData);
        }
    }
    
    public void DrawCard(string roomCode)
    {
        if (socket != null && socket.Connected)
        {
            socket.Emit("draw-card", roomCode);
        }
    }
    
    public void ChooseColor(string roomCode, string color)
    {
        if (socket != null && socket.Connected)
        {
            var colorData = $"{{\"roomCode\":\"{roomCode}\",\"color\":\"{color}\"}}";
            socket.Emit("choose-color", colorData);
        }
    }
    
    public void NotifyAnimationComplete()
    {
        Debug.Log("📡 Sending animation-complete to backend");
        if (socket != null && socket.Connected)
        {
            socket.Emit("animation-complete");
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot send animation-complete - socket not connected");
        }
    }
    
    public void NotifyWinnerAnimationComplete(string roomCode, string winner)
    {
        Debug.Log($"📡 Sending winner-animation-complete to backend for room {roomCode}, winner {winner}");
        if (socket != null && socket.Connected)
        {
            var data = $"{{\"roomCode\":\"{roomCode}\",\"winner\":\"{winner}\"}}";
            socket.Emit("winner-animation-complete", data);
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot send winner-animation-complete - socket not connected");
        }
    }
    
    public void NotifyWinnerAnimationComplete()
    {
        Debug.Log("🏆 Sending winner-animation-complete to backend");
        if (socket != null && socket.Connected)
        {
            // Get room code from GameManager
            var gameManager = FindFirstObjectByType<GameManager>();
            string roomCode = gameManager?.GetCurrentRoomCode();
            
            if (!string.IsNullOrEmpty(roomCode))
            {
                var data = new {
                    roomCode = roomCode,
                    winner = "Unknown" // Backend will handle winner tracking
                };
                socket.Emit("winner-animation-complete", data);
            }
            else
            {
                Debug.LogWarning("⚠️ Cannot send winner-animation-complete - no room code available");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot send winner-animation-complete - socket not connected");
        }
    }
    
    public void NotifyFirstCardFlipComplete()
    {
        Debug.Log("🎬 Sending first-card-flip-complete to backend");
        if (socket != null && socket.Connected)
        {
            // Get room code from GameManager
            var gameManager = FindFirstObjectByType<GameManager>();
            string roomCode = gameManager?.GetCurrentRoomCode();
            
            if (!string.IsNullOrEmpty(roomCode))
            {
                var data = $"{{\"roomCode\":\"{roomCode}\"}}";
                socket.Emit("first-card-flip-complete", data);
                Debug.Log($"🎬 Sent first-card-flip-complete for room {roomCode}");
            }
            else
            {
                // Send without room code - backend will find the active game
                socket.Emit("first-card-flip-complete", "");
                Debug.LogWarning("⚠️ Sent first-card-flip-complete without room code - backend will auto-find");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot send first-card-flip-complete - socket not connected");
        }
    }
    
    // Socket event handlers
    private void HandleRoomCreated(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            string roomCode = ExtractRoomCodeFromJson(jsonString);
            
            EnqueueMainThreadAction(() => {
                OnRoomCreated?.Invoke(roomCode);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling room created: {e.Message}");
        }
    }
    
    private void HandlePlayerJoined(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            PlayerData[] players = ExtractPlayersFromJson(jsonString);
            
            EnqueueMainThreadAction(() => {
                OnPlayerJoined?.Invoke(players);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling player joined: {e.Message}");
        }
    }
    
    private void HandleGameStarted(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log($"🎮 UNITY: Game-started event received: {jsonString}");
            
            EnqueueMainThreadAction(() => {
                Debug.Log($"🎮 UNITY: Invoking OnGameStarted event");
                OnGameStarted?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling game started: {e.Message}");
        }
    }
    
    private void HandleCardPlayed(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            
            EnqueueMainThreadAction(() => {
                OnCardPlayed?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling card played: {e.Message}");
        }
    }
    
    private void HandleCardDrawn(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            
            EnqueueMainThreadAction(() => {
                OnGameStateUpdated?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling card drawn: {e.Message}");
        }
    }
    
    private void HandleColorChosen(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            
            EnqueueMainThreadAction(() => {
                OnColorChosen?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling color chosen: {e.Message}");
        }
    }
    
    private void HandleGameStateUpdated(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            
            EnqueueMainThreadAction(() => {
                OnGameStateUpdated?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling game state updated: {e.Message}");
        }
    }
    
    private void HandleHostRestartGame(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log($"🔄 UNITY: Host restart received - but game-started event should follow");
            
            // Don't do anything here - the backend will send a game-started event
            // which will properly trigger the restart flow via HandleGameStarted
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling host restart: {e.Message}");
        }
    }
    
    private void HandleNewRoomCreated(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log($"🏠 UNITY: New room created event received: {jsonString}");
            
            string roomCode = ExtractRoomCodeFromJson(jsonString);
            Debug.Log($"🏠 UNITY: Extracted new room code: {roomCode}");
            
            // Join the new room as host
            Debug.Log($"🏠 UNITY: Joining new room as host: {roomCode}");
            socket.Emit("host-join-room", new { roomCode = roomCode });
            
            EnqueueMainThreadAction(() => {
                Debug.Log($"🏠 UNITY: Triggering OnRoomCreated for new room: {roomCode}");
                OnRoomCreated?.Invoke(roomCode);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling new room created: {e.Message}");
        }
    }
    
    private void HandleRoomError(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.LogError($"Room error: {jsonString}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling room error: {e.Message}");
        }
    }
    
    private void HandleWinnerDetected(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log($"🏆 Winner detected - starting animation: {jsonString}");
            
            EnqueueMainThreadAction(() => {
                // This triggers the winner animation sequence
                OnGameOver?.Invoke(jsonString);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling winner detected: {e.Message}");
        }
    }
    
    private void HandleGameOver(SocketIOResponse response)
    {
        try
        {
            string jsonString = response.GetValue().ToString();
            Debug.Log($"🏆 Game over event received: {jsonString}");
            
            EnqueueMainThreadAction(() => {
                // Show the final game over screen with host options (NOT another winner animation)
                GameManager gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    Debug.Log("🏆 Showing final game over screen with host options");
                    gameManager.ShowGameOverScreen(jsonString);
                }
                else
                {
                    Debug.LogError("❌ GameManager not found for game over");
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling game over: {e.Message}");
        }
    }
    
    // JSON parsing utilities
    private string ExtractRoomCodeFromJson(string json)
    {
        // Try "newRoomCode" first (for new-room-created events)
        string roomCode = ExtractJsonValue(json, "newRoomCode");
        if (string.IsNullOrEmpty(roomCode))
        {
            // Fall back to "roomCode" (for regular room-created events)
            roomCode = ExtractJsonValue(json, "roomCode");
        }
        return roomCode;
    }
    
    private string ExtractWinnerFromJson(string json)
    {
        return ExtractJsonValue(json, "winner");
    }
    
    private PlayerData[] ExtractPlayersFromJson(string json)
    {
        // Simple JSON parsing for players array
        List<PlayerData> players = new List<PlayerData>();
        
        int playersStart = json.IndexOf("\"players\":");
        if (playersStart == -1) return players.ToArray();
        
        int arrayStart = json.IndexOf('[', playersStart);
        if (arrayStart == -1) return players.ToArray();
        
        int arrayEnd = json.IndexOf(']', arrayStart);
        if (arrayEnd == -1) return players.ToArray();
        
        string playersJson = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
        string[] playerObjects = playersJson.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
        
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
        
        return players.ToArray();
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
        
        // Handle string values (enclosed in quotes)
        if (json[valueStart] == '"')
        {
            valueStart++; // Skip opening quote
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
}
