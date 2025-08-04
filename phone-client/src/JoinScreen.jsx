import { useState } from 'react';
import './index.css';

function JoinScreen({ onJoin, connectionStatus, error }) {
  const [roomCode, setRoomCode] = useState('');
  const [playerName, setPlayerName] = useState('');
  const [isJoining, setIsJoining] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!roomCode.trim() || !playerName.trim()) {
      alert('Please enter both room code and player name');
      return;
    }

    if (connectionStatus !== 'connected') {
      alert('Not connected to server. Please wait and try again.');
      return;
    }

    setIsJoining(true);
    
    try {
      await onJoin({
        roomCode: roomCode.trim().toUpperCase(),
        playerName: playerName.trim()
      });
    } catch (error) {
      console.error('Join failed:', error);
      setIsJoining(false);
    }
  };

  const isDisabled = isJoining || connectionStatus !== 'connected';

  return (
    <div className="join-screen">
      <div className="join-container">
        <div className="jackbox-logo">
          <h1>CRAZY 8s</h1>
          <p>Phone Controller</p>
        </div>
        
        {connectionStatus !== 'connected' && (
          <div className="connection-warning">
            {connectionStatus === 'disconnected' && 'Connecting to server...'}
            {connectionStatus === 'error' && 'Server connection failed'}
          </div>
        )}

        <form onSubmit={handleSubmit} className="join-form">
          <div className="input-group">
            <label htmlFor="roomCode">Room Code</label>
            <input
              id="roomCode"
              type="text"
              value={roomCode}
              onChange={(e) => setRoomCode(e.target.value.toUpperCase())}
              placeholder="Enter 4-letter code"
              maxLength={4}
              disabled={isDisabled}
              className="room-code-input"
            />
          </div>
          
          <div className="input-group">
            <label htmlFor="playerName">Your Name</label>
            <input
              id="playerName"
              type="text"
              value={playerName}
              onChange={(e) => setPlayerName(e.target.value)}
              placeholder="Enter your name"
              maxLength={20}
              disabled={isDisabled}
              className="player-name-input"
            />
          </div>
          
          <button 
            type="submit" 
            disabled={isDisabled || !roomCode.trim() || !playerName.trim()}
            className="join-button"
          >
            {isJoining ? 'Joining...' : 'Join Game'}
          </button>
        </form>

        {error && (
          <div className="error-display">
            {error}
          </div>
        )}
        
        <div className="help-text">
          <p>Enter the 4-letter room code shown on your TV screen</p>
        </div>
      </div>
    </div>
  );
}

export default JoinScreen;
