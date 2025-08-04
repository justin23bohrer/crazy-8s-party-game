import { useState, useEffect } from 'react';
import './index.css';

function GameScreen({ gameData, onLeave, socketService }) {
  const [gameState, setGameState] = useState('waiting'); // waiting, playing, choosing-suit, game-over
  const [playerHand, setPlayerHand] = useState([]);
  const [topCard, setTopCard] = useState(null);
  const [currentSuit, setCurrentSuit] = useState(null);
  const [isPlayerTurn, setIsPlayerTurn] = useState(false);
  const [currentPlayer, setCurrentPlayer] = useState('');
  const [playersInfo, setPlayersInfo] = useState([]);
  const [error, setError] = useState(null);
  const [message, setMessage] = useState('');
  const [showSuitSelector, setShowSuitSelector] = useState(false);
  const [pendingEight, setPendingEight] = useState(null);

  useEffect(() => {
    // Set up WebSocket event listeners for Crazy 8s
    socketService.on('game-started', handleGameStarted);
    socketService.on('game-state-updated', handleGameStateUpdated);
    socketService.on('card-played', handleCardPlayed);
    socketService.on('card-drawn', handleCardDrawn);
    socketService.on('suit-chosen', handleSuitChosen);
    socketService.on('game-ended', handleGameEnded);
    socketService.on('player-action', handlePlayerAction);
    socketService.on('error', handleError);

    // Cleanup on unmount
    return () => {
      socketService.off('game-started', handleGameStarted);
      socketService.off('game-state-updated', handleGameStateUpdated);
      socketService.off('card-played', handleCardPlayed);
      socketService.off('card-drawn', handleCardDrawn);
      socketService.off('suit-chosen', handleSuitChosen);
      socketService.off('game-ended', handleGameEnded);
      socketService.off('player-action', handlePlayerAction);
      socketService.off('error', handleError);
    };
  }, []);

  const handleGameStarted = (data) => {
    console.log('Crazy 8s game started:', data);
    setGameState('playing');
    updateGameState(data.gameState);
    setMessage('Game started! Match the suit or rank of the top card.');
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

  const handleSuitChosen = (data) => {
    console.log('Suit chosen:', data);
    setMessage(`${data.playerName} chose ${getSuitSymbol(data.suit)}`);
    setCurrentSuit(data.suit);
    setShowSuitSelector(false);
    setPendingEight(null);
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
    
    if (gameState.currentSuit) {
      setCurrentSuit(gameState.currentSuit);
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
      
      // If it's an 8, show suit selector
      if (card.rank === '8') {
        setPendingEight(card);
        setShowSuitSelector(true);
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

  const chooseSuit = async (suit) => {
    if (!pendingEight) return;

    try {
      setError(null);
      
      // First play the 8
      socketService.emitGameAction('play-card', {
        roomCode: gameData.roomCode,
        card: pendingEight
      });
      
      // Then choose the suit
      socketService.emitGameAction('choose-suit', {
        roomCode: gameData.roomCode,
        suit: suit
      });
      
    } catch (error) {
      console.error('Failed to choose suit:', error);
      setError('Failed to choose suit: ' + error.message);
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

  const formatCard = (card) => {
    if (!card) return '';
    return `${card.rank}${getSuitSymbol(card.suit)}`;
  };

  const getSuitSymbol = (suit) => {
    const symbols = {
      'hearts': '♥️',
      'diamonds': '♦️',
      'clubs': '♣️',
      'spades': '♠️'
    };
    return symbols[suit] || suit;
  };

  const getSuitColor = (suit) => {
    return (suit === 'hearts' || suit === 'diamonds') ? 'red' : 'black';
  };

  const canPlayCard = (card) => {
    if (!topCard || !isPlayerTurn) return false;
    
    // 8s can always be played
    if (card.rank === '8') return true;
    
    // Match suit or rank
    return card.suit === currentSuit || card.rank === topCard.rank;
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
          <p>Waiting for host to start Crazy 8s...</p>
        </div>
      </div>
    </div>
  );

  const renderSuitSelector = () => (
    <div className="suit-selector-overlay">
      <div className="suit-selector">
        <h3>Choose a suit for your 8:</h3>
        <div className="suits">
          {['hearts', 'diamonds', 'clubs', 'spades'].map(suit => (
            <button
              key={suit}
              className={`suit-button ${suit}`}
              onClick={() => chooseSuit(suit)}
            >
              {getSuitSymbol(suit)}
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
          <span className="current-suit">
            Current: {currentSuit ? getSuitSymbol(currentSuit) : 'Any'}
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
        <div className="top-card-area">
          <h3>Top Card:</h3>
          {topCard && (
            <div className={`card ${getSuitColor(topCard.suit)}`}>
              {formatCard(topCard)}
            </div>
          )}
        </div>
        
        <div className="player-info">
          {playersInfo.map(player => (
            <div key={player.name} className={`player-info-item ${player.name === currentPlayer ? 'current-player' : ''}`}>
              <span className="player-name">{player.name}</span>
              <span className="card-count">{player.cardCount} cards</span>
            </div>
          ))}
        </div>
        
        <div className="player-hand">
          <h3>Your Hand ({playerHand.length} cards):</h3>
          <div className="cards">
            {playerHand.map((card, index) => (
              <button
                key={`${card.suit}-${card.rank}-${index}`}
                className={`card ${getSuitColor(card.suit)} ${canPlayCard(card) ? 'playable' : 'unplayable'}`}
                onClick={() => playCard(card)}
                disabled={!canPlayCard(card)}
              >
                {formatCard(card)}
              </button>
            ))}
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
      
      {showSuitSelector && renderSuitSelector()}
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
