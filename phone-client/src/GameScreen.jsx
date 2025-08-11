import { useState, useEffect } from 'react';
import './index.css';

function GameScreen({ gameData, onLeave, socketService }) {
  const [gameState, setGameState] = useState('waiting'); // waiting, playing, choosing-color, game-over (changed choosing-suit to choosing-color)
  const [playerHand, setPlayerHand] = useState([]);
  const [topCard, setTopCard] = useState(null);
  const [currentColor, setCurrentColor] = useState(null); // Changed from currentSuit to currentColor
  const [isPlayerTurn, setIsPlayerTurn] = useState(false);
  const [currentPlayer, setCurrentPlayer] = useState('');
  const [playersInfo, setPlayersInfo] = useState([]);
  const [error, setError] = useState(null);
  const [message, setMessage] = useState('');
  const [showColorSelector, setShowColorSelector] = useState(false); // Changed from showSuitSelector to showColorSelector
  const [pendingEight, setPendingEight] = useState(null);
  const [isFirstPlayer, setIsFirstPlayer] = useState(false);
  const [eightCardColors, setEightCardColors] = useState(new Map()); // Track chosen colors for 8 cards

  useEffect(() => {
    // Check if this player is the first player based on gameData
    if (gameData && gameData.isFirstPlayer) {
      setIsFirstPlayer(true);
      console.log('This player is the first player - can start the game');
    }
  }, [gameData]);

  useEffect(() => {
    // Set up WebSocket event listeners for Crazy 8s
    socketService.on('game-started', handleGameStarted);
    socketService.on('game-state-updated', handleGameStateUpdated);
    socketService.on('card-played', handleCardPlayed);
    socketService.on('card-drawn', handleCardDrawn);
    socketService.on('color-chosen', handleColorChosen); // Changed from suit-chosen to color-chosen
    socketService.on('game-ended', handleGameEnded);
    socketService.on('player-action', handlePlayerAction);
    socketService.on('error', handleError);

    // Cleanup on unmount
    return () => {
      socketService.off('game-started', handleGameStarted);
      socketService.off('game-state-updated', handleGameStateUpdated);
      socketService.off('card-played', handleCardPlayed);
      socketService.off('card-drawn', handleCardDrawn);
      socketService.off('color-chosen', handleColorChosen); // Changed from suit-chosen to color-chosen
      socketService.off('game-ended', handleGameEnded);
      socketService.off('player-action', handlePlayerAction);
      socketService.off('error', handleError);
    };
  }, []);

  const handleGameStarted = (data) => {
    console.log('Crazy 8s game started:', data);
    setGameState('playing');
    updateGameState(data.gameState);
    setMessage('Game started! Match the color or rank of the top card.');
  };

  const handleGameStateUpdated = (data) => {
    console.log('Game state updated:', data);
    updateGameState(data.gameState);
  };

  const handleCardPlayed = (data) => {
    console.log('Card played:', data);
    setMessage(`${data.playerName} played ${formatCard(data.card)}`);
    updateGameState(data.gameState);
  };

  const handleCardDrawn = (data) => {
    console.log('Card drawn:', data);
    setMessage(`${data.playerName} drew a card`);
    updateGameState(data.gameState);
  };

  const handleColorChosen = (data) => { // Changed from handleSuitChosen to handleColorChosen
    console.log('Color chosen:', data);
    setMessage(`${data.playerName} chose ${getColorEmoji(data.color)}`); // Changed to use color and color emoji
    setCurrentColor(data.color); // Changed from setCurrentSuit to setCurrentColor
    setShowColorSelector(false); // Changed from setShowSuitSelector to setShowColorSelector
    setPendingEight(null);
    
    // Update the 8 card color tracking
    if (data.card && data.card.rank === '8') {
      setEightCardColors(prev => {
        const newMap = new Map(prev);
        const cardKey = `${data.card.color}-${data.card.rank}`;
        newMap.set(cardKey, data.color);
        return newMap;
      });
    }
    
    updateGameState(data.gameState);
  };

  const handleGameEnded = (data) => {
    console.log('Game ended:', data);
    setGameState('game-over');
    setMessage(`Game Over! ${data.winner} wins!`);
  };

  const handlePlayerAction = (data) => {
    setMessage(data.message);
  };

  const handleError = (data) => {
    setError(data.message);
    setTimeout(() => setError(null), 3000);
  };

  const updateGameState = (gameState) => {
    // Handle player hand (direct property from server)
    if (gameState.playerHand) {
      setPlayerHand(gameState.playerHand);
    }
    
    // Handle players list
    if (gameState.players) {
      setPlayersInfo(gameState.players);
    }
    
    if (gameState.topCard) {
      setTopCard(gameState.topCard);
    }
    
    if (gameState.currentColor) { // Changed from currentSuit to currentColor
      setCurrentColor(gameState.currentColor); // Changed from setCurrentSuit to setCurrentColor
    }
    
    if (gameState.currentPlayer !== undefined) {
      // Convert player index to player name
      if (typeof gameState.currentPlayer === 'number' && gameState.players) {
        const currentPlayerData = gameState.players[gameState.currentPlayer];
        if (currentPlayerData) {
          setCurrentPlayer(currentPlayerData.name);
          setIsPlayerTurn(currentPlayerData.name === gameData.playerName);
        }
      } else if (typeof gameState.currentPlayer === 'string') {
        setCurrentPlayer(gameState.currentPlayer);
        setIsPlayerTurn(gameState.currentPlayer === gameData.playerName);
      }
    }
    
    // Handle turn state directly if provided
    if (gameState.isYourTurn !== undefined) {
      setIsPlayerTurn(gameState.isYourTurn);
    }
  };

  const playCard = async (card) => {
    if (!isPlayerTurn) {
      setError("It's not your turn!");
      return;
    }

    try {
      setError(null);
      
      // If it's an 8, show color selector
      if (card.rank === '8') {
        setPendingEight(card);
        setShowColorSelector(true);
        return;
      }

      socketService.emitGameAction('play-card', {
        roomCode: gameData.roomCode,
        card: card
      });
    } catch (error) {
      console.error('Failed to play card:', error);
      setError('Failed to play card: ' + error.message);
    }
  };

  const chooseColor = async (color) => { // Changed from chooseSuit to chooseColor and suit to color
    if (!pendingEight) return;

    try {
      setError(null);
      
      // Track this 8 card's chosen color immediately for local state
      setEightCardColors(prev => {
        const newMap = new Map(prev);
        const cardKey = `${pendingEight.color}-${pendingEight.rank}`;
        newMap.set(cardKey, color);
        return newMap;
      });
      
      // Play the 8 with the chosen color
      socketService.emitGameAction('play-card', {
        roomCode: gameData.roomCode,
        card: pendingEight,
        chosenColor: color // Include the chosen color with the card play
      });
      
      // Reset the color selector state
      setShowColorSelector(false);
      setPendingEight(null);
      
    } catch (error) {
      console.error('Failed to choose color:', error); // Changed error message
      setError('Failed to choose color: ' + error.message); // Changed error message
    }
  };

  const drawCard = async () => {
    if (!isPlayerTurn) {
      setError("It's not your turn!");
      return;
    }

    try {
      setError(null);
      socketService.emitGameAction('draw-card', {
        roomCode: gameData.roomCode
      });
    } catch (error) {
      console.error('Failed to draw card:', error);
      setError('Failed to draw card: ' + error.message);
    }
  };

  const startGame = async () => {
    try {
      console.log('Starting game from phone client');
      setError(null);
      
      // Use emitGameAction to send to server, not the internal emit
      socketService.emitGameAction('start-game', { roomCode: gameData.roomCode });
      console.log('Sent start-game event to server with roomCode:', gameData.roomCode);
    } catch (error) {
      console.error('Failed to start game:', error);
      setError('Failed to start game: ' + error.message);
    }
  };

  const formatCard = (card) => {
    if (!card) return '';
    return `${card.rank} ${getColorEmoji(card.color)}`; // Changed to use color instead of suit
  };

  const getColorEmoji = (color) => { // Changed from getSuitSymbol to getColorEmoji
    const colorEmojis = {
      'red': '🔴',
      'blue': '🔵',
      'green': '🟢',
      'yellow': '🟡'
    };
    return colorEmojis[color] || color;
  };

  const getCardColor = (color) => { // Simplified - just return the color name
    return color;
  };

  // Helper function to get hex color values for styling
  const getCardColorHex = (color) => {
    const colorMap = {
      'red': '#ff4444',
      'blue': '#4444ff', 
      'green': '#44aa44',
      'yellow': '#ffd700'
    };
    return colorMap[color] || '#000000';
  };

  const canPlayCard = (card) => {
    if (!topCard || !isPlayerTurn) return false;
    
    // 8s can always be played
    if (card.rank === '8') return true;
    
    // Match color or rank
    return card.color === currentColor || card.rank === topCard.rank; // Changed from suit to color and currentSuit to currentColor
  };

  // Helper function to get the display color for an 8 card
  const getEightCardDisplayColor = (card) => {
    const cardKey = `${card.color}-${card.rank}`;
    return eightCardColors.get(cardKey) || null; // Returns chosen color or null if not chosen yet
  };

  const renderWaitingScreen = () => (
    <div className="game-content waiting">
      <div className="status-message">
        <h2>🎮 Connected!</h2>
        <p>Room: {gameData.roomCode}</p>
        <p>Player: {gameData.playerName}</p>
        <div className="waiting-animation">
          <div className="dots">
            <span>.</span>
            <span>.</span>
            <span>.</span>
          </div>
          {isFirstPlayer ? (
            <div className="first-player-controls">
              <p>👑 You are the first player!</p>
              <button 
                className="start-game-btn"
                onClick={startGame}
              >
                Start Game
              </button>
            </div>
          ) : (
            <p>Waiting for first player to start Crazy 8s...</p>
          )}
        </div>
      </div>
    </div>
  );

  const renderColorSelector = () => (
    <div className="color-selector-overlay">
      <div className="color-selector">
        <h3>Choose a color for your 8:</h3> {/* Changed from suit to color */}
        <div className="colors"> {/* Changed from suits to colors */}
          {['red', 'blue', 'green', 'yellow'].map(color => ( // Changed suit array to color array
            <button
              key={color}
              className={`color-button ${color}`} // Changed from suit-button to color-button
              onClick={() => chooseColor(color)} // Changed from chooseSuit to chooseColor
            >
              {getColorEmoji(color)} {/* Changed from getSuitSymbol to getColorEmoji */}
            </button>
          ))}
        </div>
      </div>
    </div>
  );

  const renderPlayingScreen = () => (
    <div className="game-content playing">
      <div className="game-header">
        <div className="game-info">
          <span className="turn-info">
            {isPlayerTurn ? "Your Turn!" : `${currentPlayer}'s Turn`}
          </span>
          <span className="current-color"> {/* Changed from current-suit to current-color */}
            Current: {currentColor ? getColorEmoji(currentColor) : 'Any'} {/* Changed from currentSuit to currentColor and getSuitSymbol to getColorEmoji */}
          </span>
        </div>
        <button onClick={onLeave} className="leave-button">Leave</button>
      </div>
      
      {message && (
        <div className="game-message">
          {message}
        </div>
      )}
      
      {error && (
        <div className="error-display">
          {error}
        </div>
      )}
      
      <div className="game-area">        
        <div className="player-hand">
          <h3>Your Hand ({playerHand.length} cards):</h3>
          <div className="cards">
            {playerHand.map((card, index) => {
              const is8Card = card.rank === '8';
              const chosenColor = is8Card ? getEightCardDisplayColor(card) : null;
              
              let cardClassName;
              if (is8Card && chosenColor) {
                // 8 card with chosen color - show as regular colored card
                cardClassName = `card ${chosenColor}`;
              } else if (is8Card) {
                // 8 card without chosen color - show spiral
                cardClassName = `card eight-card`;
              } else {
                // Regular card
                cardClassName = `card ${getCardColor(card.color)}`;
              }
              
              return (
                <button
                  key={`${card.color}-${card.rank}-${index}`}
                  className={`${cardClassName} ${canPlayCard(card) ? 'playable' : 'unplayable'}`}
                  onClick={() => playCard(card)}
                  disabled={!canPlayCard(card)}
                >
                  <div className="card-inner">
                    {is8Card && !chosenColor ? (
                      <div className="eight-card-content">
                        <div className="eight-card-number">8</div>
                      </div>
                    ) : (
                      <div className="card-number">{card.rank}</div>
                    )}
                  </div>
                </button>
              );
            })}
          </div>
        </div>
        
        {isPlayerTurn && (
          <div className="game-actions">
            <button 
              onClick={drawCard}
              className="draw-button"
            >
              Draw Card
            </button>
          </div>
        )}
      </div>
      
      {showColorSelector && renderColorSelector()}
    </div>
  );

  const renderGameOverScreen = () => (
    <div className="game-content game-over">
      <div className="status-message">
        <h2>🎉 Game Complete!</h2>
        <p>{message}</p>
        <div className="final-scores">
          <h3>Final Results:</h3>
          {playersInfo.map(player => (
            <div key={player.name} className="player-score">
              <span>{player.name}</span>
              <span>{player.cardCount} cards</span>
            </div>
          ))}
        </div>
        <button onClick={onLeave} className="play-again-button">
          Leave Game
        </button>
      </div>
    </div>
  );

  return (
    <div className="game-screen crazy-8s">
      {gameState === 'waiting' && renderWaitingScreen()}
      {gameState === 'playing' && renderPlayingScreen()}
      {gameState === 'game-over' && renderGameOverScreen()}
    </div>
  );
}

export default GameScreen;
