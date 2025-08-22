using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages player data, player displays, and player positioning
/// Handles both lobby and game screen player representations
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Player UI References")]
    public Transform playersContainer;
    public GameObject playerCardPrefab;
    public PlayerPositionManager playerPositionManager;
    public LobbyPlayerManager lobbyPlayerManager;
    
    private List<PlayerData> players = new List<PlayerData>();
    private Dictionary<string, string> playerColors = new Dictionary<string, string>();
    
    // Events
    public event System.Action<PlayerData[]> OnPlayersUpdated;
    public event System.Action<string> OnCurrentPlayerChanged;
    
    public void Initialize()
    {
        // Auto-find components if not assigned
        if (playersContainer == null)
        {
            GameObject container = GameObject.Find("PlayersContainer");
            if (container != null)
            {
                playersContainer = container.transform;
            }
        }
        
        if (playerPositionManager == null)
        {
            playerPositionManager = FindFirstObjectByType<PlayerPositionManager>();
        }
        
        if (lobbyPlayerManager == null)
        {
            lobbyPlayerManager = FindFirstObjectByType<LobbyPlayerManager>();
        }
        
        ClearAllPlayers();
    }
    
    public void UpdatePlayers(PlayerData[] newPlayers)
    {
        if (newPlayers == null) return;
        
        // Update internal player list
        players.Clear();
        players.AddRange(newPlayers);
        
        // Update player colors dictionary
        playerColors.Clear();
        foreach (PlayerData player in players)
        {
            if (!string.IsNullOrEmpty(player.name) && !string.IsNullOrEmpty(player.color))
            {
                playerColors[player.name] = player.color;
            }
        }
        
        // Update appropriate display based on current screen
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // Check which screen is active
            if (IsGameScreenActive())
            {
                UpdateGamePlayerDisplays(newPlayers);
            }
            else if (IsLobbyScreenActive())
            {
                UpdateLobbyPlayerDisplays(newPlayers);
            }
        }
        
        OnPlayersUpdated?.Invoke(newPlayers);
    }
    
    private void UpdateGamePlayerDisplays(PlayerData[] newPlayers)
    {
        if (playerPositionManager != null)
        {
            playerPositionManager.UpdatePlayersDisplay(newPlayers);
        }
    }
    
    private void UpdateLobbyPlayerDisplays(PlayerData[] newPlayers)
    {
        if (lobbyPlayerManager != null)
        {
            lobbyPlayerManager.UpdateLobbyPlayers(newPlayers);
        }
        else
        {
            // Fallback to basic lobby display
            UpdateBasicLobbyDisplay(newPlayers);
        }
    }
    
    private void UpdateBasicLobbyDisplay(PlayerData[] newPlayers)
    {
        if (playersContainer == null || playerCardPrefab == null) return;
        
        // Clear existing player cards
        foreach (Transform child in playersContainer)
        {
            if (child.gameObject != playerCardPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Create new player cards
        foreach (PlayerData player in newPlayers)
        {
            if (string.IsNullOrEmpty(player.name)) continue;
            
            GameObject playerCard = Instantiate(playerCardPrefab, playersContainer);
            
            // Update player card UI
            TextMeshProUGUI nameText = playerCard.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = player.name;
                
                // Color the text based on player color
                if (!string.IsNullOrEmpty(player.color))
                {
                    Color playerColor = GetPlayerColorValue(player.color);
                    nameText.color = playerColor;
                }
            }
            
            // Add first player indicator
            if (player.isFirstPlayer)
            {
                // Find or create crown/indicator
                Transform indicator = playerCard.transform.Find("FirstPlayerIndicator");
                if (indicator != null)
                {
                    indicator.gameObject.SetActive(true);
                }
            }
        }
        
        // Force layout refresh
        StartCoroutine(RefreshLayoutDelayed());
    }
    
    private System.Collections.IEnumerator RefreshLayoutDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        if (playersContainer != null)
        {
            LayoutGroup layoutGroup = playersContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(playersContainer.GetComponent<RectTransform>());
            }
        }
    }
    
    public void HighlightCurrentPlayer(string playerName)
    {
        if (playerPositionManager != null)
        {
            playerPositionManager.HighlightCurrentPlayer(playerName);
        }
        
        OnCurrentPlayerChanged?.Invoke(playerName);
    }
    
    public void ResetPlayers()
    {
        players.Clear();
        playerColors.Clear();
        ClearAllPlayers();
    }
    
    private void ClearAllPlayers()
    {
        if (playersContainer != null)
        {
            foreach (Transform child in playersContainer)
            {
                if (child.gameObject != playerCardPrefab)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        if (playerPositionManager != null)
        {
            playerPositionManager.ClearPlayerDisplays();
        }
        
        if (lobbyPlayerManager != null)
        {
            lobbyPlayerManager.ClearLobbyCards();
        }
    }
    
    // Helper methods for other managers
    public string GetPlayerColor(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return "";
        
        if (playerColors.ContainsKey(playerName))
        {
            return playerColors[playerName];
        }
        
        // Fallback: search in players list
        PlayerData player = players.FirstOrDefault(p => p.name == playerName);
        if (player != null && !string.IsNullOrEmpty(player.color))
        {
            playerColors[playerName] = player.color; // Cache it
            return player.color;
        }
        
        return "blue"; // Default color
    }
    
    public PlayerData GetPlayer(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return null;
        
        return players.FirstOrDefault(p => p.name == playerName);
    }
    
    public List<PlayerData> GetAllPlayers()
    {
        return new List<PlayerData>(players);
    }
    
    public int GetPlayerCount()
    {
        return players.Count;
    }
    
    public bool HasPlayers()
    {
        return players.Count > 0;
    }
    
    public GameObject FindPlayerDisplay(string playerName)
    {
        if (playerPositionManager != null)
        {
            return playerPositionManager.FindPlayerDisplay(playerName);
        }
        
        // Fallback: search in lobby container
        if (playersContainer != null)
        {
            foreach (Transform child in playersContainer)
            {
                TextMeshProUGUI nameText = child.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null && nameText.text == playerName)
                {
                    return child.gameObject;
                }
            }
        }
        
        return null;
    }
    
    public List<GameObject> GetAllPlayerDisplays()
    {
        List<GameObject> displays = new List<GameObject>();
        
        if (playerPositionManager != null)
        {
            displays.AddRange(playerPositionManager.GetAllPlayerDisplays());
        }
        else if (playersContainer != null)
        {
            // Fallback: get from lobby container
            foreach (Transform child in playersContainer)
            {
                if (child.gameObject != playerCardPrefab)
                {
                    displays.Add(child.gameObject);
                }
            }
        }
        
        return displays;
    }
    
    public string GetPlayerNameFromDisplay(GameObject playerDisplay)
    {
        if (playerDisplay == null) return "";
        
        TextMeshProUGUI nameText = playerDisplay.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            return nameText.text;
        }
        
        return "";
    }
    
    private Color GetPlayerColorValue(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "red": return new Color(0.8f, 0.2f, 0.2f);
            case "blue": return new Color(0.2f, 0.4f, 0.8f);
            case "green": return new Color(0.2f, 0.8f, 0.2f);
            case "yellow": return new Color(0.8f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }
    
    private bool IsGameScreenActive()
    {
        GameObject gameScreen = GameObject.Find("GameScreen");
        return gameScreen != null && gameScreen.activeInHierarchy;
    }
    
    private bool IsLobbyScreenActive()
    {
        GameObject lobbyScreen = GameObject.Find("LobbyScreen");
        return lobbyScreen != null && lobbyScreen.activeInHierarchy;
    }
}
