import { useState, useEffect } from 'react';
import './index.css';

function GameScreen({ gameData, onLeave, socketService }) {
  // Over Under game states
  const [gameState, setGameState] = useState('waiting'); // waiting, playing, answering, voting, round-results, game-over
  const [currentQuestion, setCurrentQuestion] = useState('');
  const [isAnswerer, setIsAnswerer] = useState(false);
  const [answererName, setAnswererName] = useState('');
  const [playerAnswer, setPlayerAnswer] = useState(null);
  const [isVotingActive, setIsVotingActive] = useState(false);
  const [votingTimeLeft, setVotingTimeLeft] = useState(30);
  const [playerVote, setPlayerVote] = useState(null);
  const [votesSubmitted, setVotesSubmitted] = useState(0);
  const [totalVotesNeeded, setTotalVotesNeeded] = useState(0);
  const [currentRound, setCurrentRound] = useState(1);
  const [totalRounds, setTotalRounds] = useState(4);
  const [scores, setScores] = useState([]);
  const [roundResults, setRoundResults] = useState(null);
  const [playersInfo, setPlayersInfo] = useState([]);
  const [error, setError] = useState(null);
  const [message, setMessage] = useState('');
  const [isFirstPlayer, setIsFirstPlayer] = useState(false);
  const [isProcessingHostAction, setIsProcessingHostAction] = useState(false);
  const [answerInput, setAnswerInput] = useState('');

  useEffect(() => {
    // Check if this player is the first player based on gameData
    if (gameData && gameData.isFirstPlayer) {
      setIsFirstPlayer(true);
      console.log('This player is the first player - can start the game');
    }
  }, [gameData]);

  useEffect(() => {
    console.log('🔧 PHONE: Setting up Over Under socket event listeners...');
    // Set up WebSocket event listeners for Over Under
    socketService.on('game-started', handleGameStarted);
    socketService.on('show-question', handleShowQuestion);
    socketService.on('voting-phase', handleVotingPhase);
    socketService.on('vote-submitted', handleVoteSubmitted);
    socketService.on('voting-timer-update', handleVotingTimerUpdate);
    socketService.on('round-results', handleRoundResults);
    socketService.on('update-scoreboard', handleUpdateScoreboard);
    socketService.on('game-over', handleGameOver);
    socketService.on('error', handleError);
    socketService.on('room-closed', handleRoomClosed);
    
    console.log('🔧 PHONE: Over Under event listeners set up');

    // Cleanup on unmount
    return () => {
      socketService.off('game-started', handleGameStarted);
      socketService.off('show-question', handleShowQuestion);
      socketService.off('voting-phase', handleVotingPhase);
      socketService.off('vote-submitted', handleVoteSubmitted);
      socketService.off('voting-timer-update', handleVotingTimerUpdate);
      socketService.off('round-results', handleRoundResults);
      socketService.off('update-scoreboard', handleUpdateScoreboard);
      socketService.off('game-over', handleGameOver);
      socketService.off('error', handleError);
      socketService.off('room-closed', handleRoomClosed);
    };
  }, []);

  const handleGameStarted = (data) => {
    console.log('Over Under game started:', data);
    setGameState('playing');
    setMessage('Over Under game started! Wait for your question...');
    setTotalRounds(data.totalRounds || 4);
  };

  const handleShowQuestion = (data) => {
    console.log('Question received:', data);
    setCurrentQuestion(data.question);
    setAnswererName(data.answerer);
    setIsAnswerer(data.answererId === gameData.socketId);
    setCurrentRound(data.roundNumber);
    setTotalRounds(data.totalRounds);
    setPlayerAnswer(null);
    setPlayerVote(null);
    setIsVotingActive(false);
    setAnswerInput('');
    setRoundResults(null);
    
    if (data.answererId === gameData.socketId) {
      setGameState('answering');
      setMessage(`Your turn! Answer: ${data.question}`);
    } else {
      setGameState('waiting-for-answer');
      setMessage(`${data.answerer} is answering: ${data.question}`);
    }
  };

  const handleVotingPhase = (data) => {
    console.log('Voting phase started:', data);
    setPlayerAnswer(data.playerAnswer);
    setIsVotingActive(true);
    setVotingTimeLeft(data.votingTimeLeft);
    setVotesSubmitted(0);
    setTotalVotesNeeded(playersInfo.length - 1);
    
    if (isAnswerer) {
      setGameState('waiting-for-votes');
      setMessage(`You answered ${data.playerAnswer}. Others are voting...`);
    } else {
      setGameState('voting');
      setMessage(`${data.answerer} answered ${data.playerAnswer}. Vote Over or Under!`);
    }
  };

  const handleVoteSubmitted = (data) => {
    console.log('Vote submitted update:', data);
    setVotesSubmitted(data.votesSubmitted);
    setTotalVotesNeeded(data.totalVotesNeeded);
  };

  const handleVotingTimerUpdate = (data) => {
    setVotingTimeLeft(data.timeLeft);
  };

  const handleRoundResults = (data) => {
    console.log('Round results received:', data);
    setRoundResults(data);
    setScores(data.scores);
    setGameState('round-results');
    
    const winners = data.winners.length > 0 ? data.winners.join(', ') : 'No one';
    setMessage(`Correct answer: ${data.correctAnswer}. Winners: ${winners}`);
  };

  const handleUpdateScoreboard = (data) => {
    console.log('Scoreboard updated:', data);
    setScores(data.scores);
  };

  const handleGameOver = (data) => {
    console.log('🏆 Game over received:', data);
    setGameState('game-over');
    setScores(data.finalScores);
    setMessage(`Game Over! ${data.winner.playerName} wins with ${data.winner.totalScore} points!`);
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

  const submitAnswer = async () => {
    if (!answerInput.trim()) {
      setError('Please enter a number');
      return;
    }

    const numAnswer = parseInt(answerInput.trim());
    if (isNaN(numAnswer)) {
      setError('Please enter a valid number');
      return;
    }

    try {
      setError(null);
      socketService.emitGameAction('submit-answer', {
        roomCode: gameData.roomCode,
        answer: numAnswer
      });
      setMessage('Answer submitted! Waiting for others to vote...');
    } catch (error) {
      console.error('Failed to submit answer:', error);
      setError('Failed to submit answer: ' + error.message);
    }
  };

  const submitVote = async (vote) => {
    if (playerVote) {
      setError('You have already voted!');
      return;
    }

    try {
      setError(null);
      socketService.emitGameAction('submit-vote', {
        roomCode: gameData.roomCode,
        vote: vote
      });
      setPlayerVote(vote);
      setMessage(`Voted ${vote}! Waiting for others...`);
    } catch (error) {
      console.error('Failed to submit vote:', error);
      setError('Failed to submit vote: ' + error.message);
    }
  };



  const startGame = async () => {
    try {
      console.log('Starting Over Under game from phone client');
      setError(null);
      
      socketService.emitGameAction('start-game', { roomCode: gameData.roomCode });
      console.log('Sent start-game event to server with roomCode:', gameData.roomCode);
    } catch (error) {
      console.error('Failed to start game:', error);
      setError('Failed to start game: ' + error.message);
    }
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

  const renderWaitingScreen = () => (
    <div className="game-content waiting">
      <div className="status-message">
        <h2>� Connected to Over Under!</h2>
        <p>Room: {gameData.roomCode}</p>
        <p>Player: <span className="player-name-white">
          {gameData.playerName}
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
                Start Over Under
              </button>
            </div>
          ) : (
            <p>Waiting for first player to start Over Under...</p>
          )}
        </div>
      </div>
    </div>
  );

  const renderAnsweringScreen = () => (
    <div className="game-content answering">
      <div className="question-container">
        <h2>🤔 Your Turn to Answer!</h2>
        <div className="question-text">
          {currentQuestion}
        </div>
        <div className="answer-input-section">
          <input
            type="number"
            value={answerInput}
            onChange={(e) => setAnswerInput(e.target.value)}
            placeholder="Enter your guess..."
            className="answer-input"
          />
          <button
            onClick={submitAnswer}
            className="submit-answer-btn"
            disabled={!answerInput.trim()}
          >
            Submit Answer
          </button>
        </div>
        <p className="round-info">Round {currentRound} of {totalRounds}</p>
      </div>
    </div>
  );

  const renderWaitingForAnswerScreen = () => (
    <div className="game-content waiting-answer">
      <div className="status-message">
        <h2>⏳ Waiting for Answer</h2>
        <div className="question-text">
          {currentQuestion}
        </div>
        <p>{answererName} is thinking...</p>
        <p className="round-info">Round {currentRound} of {totalRounds}</p>
      </div>
    </div>
  );

  const renderVotingScreen = () => (
    <div className="game-content voting">
      <div className="voting-container">
        <h2>🗳️ Time to Vote!</h2>
        <div className="question-text">
          {currentQuestion}
        </div>
        <div className="answer-display">
          <p><strong>{answererName} answered: {playerAnswer}</strong></p>
        </div>
        <div className="voting-buttons">
          <button
            onClick={() => submitVote('under')}
            className="vote-btn under-btn"
            disabled={playerVote !== null}
          >
            UNDER 👇
          </button>
          <button
            onClick={() => submitVote('over')}
            className="vote-btn over-btn"
            disabled={playerVote !== null}
          >
            OVER 👆
          </button>
        </div>
        {playerVote && (
          <div className="vote-submitted">
            ✅ You voted: {playerVote.toUpperCase()}
          </div>
        )}
        <div className="voting-status">
          <p>Votes: {votesSubmitted}/{totalVotesNeeded}</p>
          <p>Time left: {votingTimeLeft}s</p>
        </div>
        <p className="round-info">Round {currentRound} of {totalRounds}</p>
      </div>
    </div>
  );

  const renderWaitingForVotesScreen = () => (
    <div className="game-content waiting-votes">
      <div className="status-message">
        <h2>⏳ Waiting for Votes</h2>
        <div className="question-text">
          {currentQuestion}
        </div>
        <p>You answered: <strong>{playerAnswer}</strong></p>
        <p>Others are voting Over or Under...</p>
        <div className="voting-status">
          <p>Votes: {votesSubmitted}/{totalVotesNeeded}</p>
          <p>Time left: {votingTimeLeft}s</p>
        </div>
        <p className="round-info">Round {currentRound} of {totalRounds}</p>
      </div>
    </div>
  );

  const renderRoundResultsScreen = () => (
    <div className="game-content round-results">
      <div className="results-container">
        <h2>📊 Round Results</h2>
        <div className="question-text">
          {roundResults?.question}
        </div>
        <div className="answer-comparison">
          <p>Player Answer: <strong>{roundResults?.playerAnswer}</strong></p>
          <p>Correct Answer: <strong>{roundResults?.correctAnswer}</strong></p>
          <p>Correct Vote: <strong>{roundResults?.correctVote?.toUpperCase()}</strong></p>
        </div>
        {roundResults?.winners?.length > 0 && (
          <div className="winners">
            <p>🏆 Winners: {roundResults.winners.join(', ')}</p>
          </div>
        )}
        <div className="scores">
          <h3>Current Scores:</h3>
          {scores.map(player => (
            <div key={player.playerId} className="score-item">
              <span style={{ color: getPlayerColorHex(player.playerColor) }}>
                {player.playerName}
              </span>
              <span>{player.score} pts</span>
            </div>
          ))}
        </div>
        <p className="round-info">Round {currentRound - 1} of {totalRounds} complete</p>
      </div>
    </div>
  );

  const renderPlayingScreen = () => {
    if (gameState === 'answering') return renderAnsweringScreen();
    if (gameState === 'waiting-for-answer') return renderWaitingForAnswerScreen();
    if (gameState === 'voting') return renderVotingScreen();
    if (gameState === 'waiting-for-votes') return renderWaitingForVotesScreen();
    if (gameState === 'round-results') return renderRoundResultsScreen();
    
    // Default playing screen
    return (
      <div className="game-content playing">
        <div className="game-header">
          <div className="player-info">
            <span className="player-name-white">
              {gameData?.playerName}
            </span>
          </div>
          <div className="game-info">
            <span className="round-info">
              Round {currentRound} of {totalRounds}
            </span>
          </div>
          <button onClick={onLeave} className="leave-button">
            Leave
          </button>
        </div>
        
        {error && (
          <div className="error-display">
            {error}
          </div>
        )}
        
        <div className="status-message">
          <h2>🎯 Over Under Game</h2>
          <p>{message}</p>
          {scores.length > 0 && (
            <div className="current-scores">
              <h3>Current Scores:</h3>
              {scores.map(player => (
                <div key={player.playerId} className="score-item">
                  <span style={{ color: getPlayerColorHex(player.playerColor) }}>
                    {player.playerName}
                  </span>
                  <span>{player.score} pts</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  };

  const renderGameOverScreen = () => (
    <div className="game-content game-over">
      <div className="status-message">
        <h2>🎉 Over Under Complete!</h2>
        <p>{message}</p>
        <div className="final-scores">
          <h3>Final Results:</h3>
          {scores.map((player, index) => (
            <div key={player.playerId} className={`player-score ${index === 0 ? 'winner' : ''}`}>
              <span style={{ color: getPlayerColorHex(player.playerColor) }}>
                {index === 0 ? '🏆 ' : ''}{player.playerName}
              </span>
              <span>{player.totalScore} pts</span>
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
    <div className="game-screen over-under">
      {gameState === 'waiting' && renderWaitingScreen()}
      {(gameState === 'playing' || gameState === 'answering' || gameState === 'waiting-for-answer' || 
        gameState === 'voting' || gameState === 'waiting-for-votes' || gameState === 'round-results') && 
        renderPlayingScreen()}
      {gameState === 'game-over' && renderGameOverScreen()}
    </div>
  );
}

export default GameScreen;
