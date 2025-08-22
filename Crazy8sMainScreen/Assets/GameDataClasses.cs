using System;

/// <summary>
/// Shared data classes for the Crazy 8s game
/// Contains all serializable data structures used across managers
/// </summary>

[System.Serializable]
public class PlayerData
{
    public string name;
    public int cardCount;
    public string playerId;
    public bool isFirstPlayer;
    public string color;
    
    public PlayerData()
    {
    }
    
    public PlayerData(string name, int cardCount, string playerId = "")
    {
        this.name = name;
        this.cardCount = cardCount;
        this.playerId = playerId;
        this.isFirstPlayer = false;
        this.color = "";
    }
}

[System.Serializable]
public class PlayerJoinedData
{
    public PlayerData[] players;
    
    public PlayerJoinedData()
    {
        players = new PlayerData[0];
    }
    
    public PlayerJoinedData(PlayerData[] players)
    {
        this.players = players ?? new PlayerData[0];
    }
}

[System.Serializable]
public class GameStateData
{
    public string currentPlayer;
    public string topCard;
    public string currentColor;
    public int deckCount;
    public PlayerData[] players;
    public bool gameInProgress;
    
    public GameStateData()
    {
        players = new PlayerData[0];
    }
}

[System.Serializable]
public class CardPlayedData
{
    public string playerId;
    public string card;
    public string newTopCard;
    public string newCurrentPlayer;
    public int remainingCards;
}

[System.Serializable]
public class ColorChosenData
{
    public string playerId;
    public string color;
    public string currentPlayer;
}

[System.Serializable]
public class GameOverData
{
    public string winnerId;
    public string winnerName;
    public PlayerData[] finalstandings;
}
