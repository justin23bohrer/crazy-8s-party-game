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
  const [isAnimating, setIsAnimating] = useState(false); // Track if spiral animation is playing
  const [isProcessingHostAction, setIsProcessingHostAction] = useState(false); // Prevent multiple host actions

  useEffect(() => {
    // Check if this player is the first player based on gameData
    if (gameData && gameData.isFirstPlayer) {
      setIsFirstPlayer(true);
      console.log('This player is the first player - can start the game');
    }
  }, [gameData]);

  useEffect(() => {
    console.log('🔧 PHONE: Setting up socket event listeners...');
    // Set up WebSocket event listeners for Crazy 8s
    socketService.on('game-started', handleGameStarted);
    socketService.on('game-state-updated', handleGameStateUpdated);
    socketService.on('card-played', handleCardPlayed);
    socketService.on('card-drawn', handleCardDrawn);
    socketService.on('color-chosen', handleColorChosen); // Changed from suit-chosen to color-chosen
    socketService.on('game-ended', handleGameEnded);
    socketService.on('game-over', handleGameOver);
    socketService.on('player-action', handlePlayerAction);
    socketService.on('error', handleError);
    socketService.on('room-closed', handleRoomClosed); // Room closure event
    socketService.on('game-restarted', handleGameRestarted); // Explicit restart event
    
    console.log('🔧 PHONE: Event listeners set up, including game-over');

    // Cleanup on unmount
    return () => {
      socketService.off('game-started', handleGameStarted);
      socketService.off('game-state-updated', handleGameStateUpdated);
      socketService.off('card-played', handleCardPlayed);
      socketService.off('card-drawn', handleCardDrawn);
      socketService.off('color-chosen', handleColorChosen); // Changed from suit-chosen to color-chosen
      socketService.off('game-ended', handleGameEnded);
      socketService.off('game-over', handleGameOver);
      socketService.off('player-action', handlePlayerAction);
      socketService.off('error', handleError);
      socketService.off('room-closed', handleRoomClosed); // Room closure event
      socketService.off('game-restarted', handleGameRestarted); // Explicit restart event
    };
  }, []);

  const handleGameStarted = (data) => {
    console.log('Crazy 8s game started:', data);
    setGameState('playing');
    updateGameState(data.gameState);
    setMessage('Game started! Match the color or rank of the top card.');
  };

  const handleGameStateUpdated = (data) => {
    console.log('🔄 Game state updated:', data);
    console.log('📱 Current gameState before processing:', gameState);
    
    // Check if game was restarted - detect from waiting, game-over, or any non-playing state
    // Look for explicit restart indicators: phase=playing AND isRestarted=true
    const gameWasRestarted = (gameState === 'game-over' || gameState === 'waiting') && 
      data.gameState.phase === 'playing' && data.gameState.isRestarted === true;
    
    if (gameWasRestarted) {
      console.log('🎮 RESTART DETECTED: Game restarted by host - switching back to playing mode');
      setGameState('playing');
      setMessage('Host restarted the game - new round started!');
      
      // Clear any color selector state
      setShowColorSelector(false);
      setPendingEight(null);
      
      // Reset animation state
      setIsAnimating(false);
      
      // Clear 8 card color tracking for fresh start
      setEightCardColors(new Map());
      
      // Clear any errors
      setError(null);
      
      // Reset processing state so host buttons work again
      setIsProcessingHostAction(false);
      
      console.log('✅ Phone client switched to playing mode');
    } else {
      console.log(`📱 Not switching modes - current gameState: ${gameState}, phase: ${data.gameState?.phase}, isRestarted: ${data.gameState?.isRestarted}`);
    }
    
    updateGameState(data.gameState);
    console.log('📱 Game state update processing complete');
  };

  const handleGameRestarted = (data) => {
    console.log('🔄 EXPLICIT RESTART EVENT received:', data);
    
    // Accept restart from any state (game-over, waiting, playing)
    console.log('🎮 EXPLICIT RESTART: Switching to playing mode');
    setGameState('playing');
    setMessage(data.message || 'Host restarted the game - new round started!');
    
    // Clear any color selector state
    setShowColorSelector(false);
    setPendingEight(null);
    
    // Reset animation state
    setIsAnimating(false);
    
    // Clear 8 card color tracking for fresh start
    setEightCardColors(new Map());
    
    // Clear any errors
    setError(null);
    
    // Reset processing state
    setIsProcessingHostAction(false);
    
    console.log('✅ Phone client switched to playing mode via explicit restart event');
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
    setMessage(`${data.playerName} chose ${getColorEmoji(data.color)} - Watch the main screen! 🎬`); // Enhanced message
    setCurrentColor(data.color); // Changed from setCurrentSuit to setCurrentColor
    setShowColorSelector(false); // Changed from setShowSuitSelector to setShowColorSelector
    setPendingEight(null);
    
    // Set animation state immediately when color is chosen
    setIsAnimating(true);
    
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
    console.log('🏆 Winner detected - staying locked until animation complete:', data);
    // DON'T change gameState yet - keep it as 'playing'
    // DON'T show game over screen yet
    setMessage(`🏆 ${data.winner} wins! Watch the main screen! 🎬`);
    setIsAnimating(true); // Lock UI completely during winner animation
  };
  
  const handleGameOver = (data) => {
    console.log('🏆 PHONE: Game over event received!', data);
    console.log('🏆 PHONE: Current game state before:', gameState);
    console.log('🏆 PHONE: Current isAnimating before:', isAnimating);
    
    setGameState('game-over');
    setMessage(`Game Over! ${data.winner} wins!`);
    setIsAnimating(false); // Unlock UI now that animation is complete
    
    console.log('🏆 PHONE: Game state set to game-over');
  };

  const handlePlayerAction = (data) => {
    setMessage(data.message);
  };

  const handleError = (data) => {
    setError(data.message);
    setTimeout(() => setError(null), 3000);
  };

  const handleRoomClosed = (data) => {
    console.log('Room closed - returning to home:', data);
    setMessage(data.message || 'Host started a new game with new players');
    
    // Reset processing state
    setIsProcessingHostAction(false);
    
    // Navigate back to home screen so players can rejoin with new room code
    setTimeout(() => {
      onLeave(); // This should trigger navigation back to the join screen
    }, 2000); // Give users a moment to read the message
  };

  // Handle host actions with proper state management
  const handleHostAction = (action, data) => {
    if (isProcessingHostAction) {
      console.log(`Already processing host action, ignoring ${action}`);
      return;
    }
    
    setIsProcessingHostAction(true);
    
    if (action === 'host-new-players') {
      // For new players, immediately kick everyone back to join screen
      setMessage('Starting new game with new players...');
      socketService.emitGameAction(action, data);
      
      // Immediately navigate back to join screen after a brief moment
      setTimeout(() => {
        onLeave();
      }, 1500);
    } else {
      // For other actions like restart, stay on screen
      setMessage(`Processing ${action}...`);
      socketService.emitGameAction(action, data);
      
      // Reset processing state after timeout as fallback
      setTimeout(() => {
        setIsProcessingHostAction(false);
        console.log('🔄 Reset processing state after timeout');
      }, 5000); // 5 second timeout
    }
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
    
    // Track animation state from server
    if (gameState.isAnimating !== undefined) {
      console.log('📱 Animation state received:', gameState.isAnimating);
      setIsAnimating(gameState.isAnimating);
      if (gameState.isAnimating) {
        console.log('🚫 UI locked - animation in progress');
        // Close color selector if animation starts
        setShowColorSelector(false);
        setPendingEight(null);
      } else if (isAnimating && !gameState.isAnimating) {
        // Animation just finished
        console.log('✅ UI unlocked - animation complete');
      }
    }

    // Handle game phase changes
    if (gameState.phase) {
      console.log('📱 Game phase received:', gameState.phase);
      if (gameState.phase === 'winner-animation' || gameState.phase === 'game-over') {
        console.log('🏆 Winner animation or game over - disabling UI');
        setIsAnimating(true); // Lock UI during winner animation/game over
        setShowColorSelector(false);
        setPendingEight(null);
      }
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

    if (isAnimating) {
      setError("Please wait for the animation to complete!");
      return;
    }

    try {
      setError(null);
      
      // If it's an 8, check if this would be a winning play
      if (card.rank === '8') {
        // Check if this is the last card (winning play)
        const isWinningPlay = playerHand.length === 1; // This card is the last one
        
        if (isWinningPlay) {
          console.log('🏆 Playing winning 8 - skipping color selection');
          // Play the winning 8 directly without color selection
          socketService.emitGameAction('play-card', {
            roomCode: gameData.roomCode,
            card: card
            // No chosenColor needed for winning 8
          });
          return;
        } else {
          // Non-winning 8 - show color selector
          setPendingEight(card);
          setShowColorSelector(true);
          return;
        }
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

    if (isAnimating) {
      setError("Please wait for the animation to complete!");
      return;
    }

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

    if (isAnimating) {
      setError("Please wait for the animation to complete!");
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

  const getPlayerColorHex = (color) => {
    const colorHex = {
      'red': '#dc2626',
      'blue': '#2563eb',
      'green': '#16a34a',
      'yellow': '#ca8a04'
    };
    return colorHex[color] || '#6b7280';
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
    if (!topCard || !isPlayerTurn || isAnimating) return false; // Block actions during spiral animation
    
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
        <p>Player: <span style={{ color: getPlayerColorHex(gameData?.playerColor) }}>
          {gameData.playerName} ({getColorEmoji(gameData?.playerColor)})
        </span></p>
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
              disabled={isAnimating} // Disable color selection during animations
            >
              {getColorEmoji(color)} {/* Changed from getSuitSymbol to getColorEmoji */}
            </button>
          ))}
        </div>
      </div>
    </div>
  );

  const renderPlayingScreen = () => (
    <div className={`game-content playing ${isAnimating ? 'animation-locked' : ''}`}>
      <div className="game-header">
        <div className="player-info">
          <span className="player-name" style={{ color: getPlayerColorHex(gameData?.playerColor) }}>
            {gameData?.playerName} ({getColorEmoji(gameData?.playerColor)})
          </span>
        </div>
        <div className="game-info">
          <span className="turn-info">
            {isPlayerTurn ? "Your Turn!" : `${currentPlayer}'s Turn`}
          </span>
          <span className="current-color"> {/* Changed from current-suit to current-color */}
            Current: {currentColor ? getColorEmoji(currentColor) : 'Any'} {/* Changed from currentSuit to currentColor and getSuitSymbol to getColorEmoji */}
          </span>
        </div>
        <button 
          onClick={onLeave} 
          className="leave-button"
          disabled={isAnimating} // Disable leave button during animations
        >
          Leave
        </button>
      </div>
      
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
              disabled={isAnimating} // Disable drawing during animations
            >
              Draw Card
            </button>
          </div>
        )}
      </div>
      
      {showColorSelector && !isAnimating && renderColorSelector()}
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
        
        {isFirstPlayer ? (
          <div className="host-controls">
            <h3>👑 Host Controls</h3>
            <div className="host-buttons">
              <button 
                onClick={() => handleHostAction('host-restart-game', { roomCode: gameData.roomCode })}
                className="host-button play-again-button"
                disabled={isProcessingHostAction}
              >
                🔄 Play Again
              </button>
              <button 
                onClick={() => handleHostAction('host-new-players', { roomCode: gameData.roomCode })}
                className="host-button new-players-button"
                disabled={isProcessingHostAction}
              >
                👥 New Players
              </button>
            </div>
            <button onClick={onLeave} className="leave-button secondary">
              Leave Game
            </button>
          </div>
        ) : (
          <div className="waiting-for-host">
            <h3>⏳ Waiting for host to choose...</h3>
            <p>The host will decide whether to play again or start fresh with new players.</p>
            <button onClick={onLeave} className="leave-button secondary">
              Leave Game
            </button>
          </div>
        )}
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
