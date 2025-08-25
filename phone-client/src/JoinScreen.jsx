import { useState, useEffect } from 'react';
import './index.css';

function JoinScreen({ onJoin, connectionStatus, error }) {
  const [roomCode, setRoomCode] = useState('');
  const [playerName, setPlayerName] = useState('');
  const [isJoining, setIsJoining] = useState(false);
  const [validationError, setValidationError] = useState('');

  // Clear fields when there's an error from the server
  useEffect(() => {
    if (error) {
      setRoomCode('');
      setPlayerName('');
      setIsJoining(false);
    }
  }, [error]);

  const validatePlayerName = (name) => {
    const trimmedName = name.trim();
    if (trimmedName.length <= 1) {
      return 'Name must be more than 1 character';
    }
    if (trimmedName.length > 7) {
      return 'Name must be 7 characters or less';
    }
    return null;
  };

  const handlePlayerNameChange = (e) => {
    let newName = e.target.value;
    
    // Enforce 7 character limit at the JavaScript level
    if (newName.length > 7) {
      newName = newName.substring(0, 7);
    }
    
    setPlayerName(newName);
    
    // Clear validation error when user starts typing
    if (validationError) {
      setValidationError('');
    }
  };

  const handleRoomCodeChange = (e) => {
    const newCode = e.target.value.toUpperCase();
    setRoomCode(newCode);
    
    // Clear validation error when user starts typing
    if (validationError) {
      setValidationError('');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    // Clear any previous validation errors
    setValidationError('');
    
    // Validate room code
    if (!roomCode.trim()) {
      setValidationError('Please enter a room code');
      return;
    }
    
    if (roomCode.trim().length !== 4) {
      setValidationError('Room code must be exactly 4 characters');
      setRoomCode('');
      return;
    }
    
    // Validate player name
    const nameValidationError = validatePlayerName(playerName);
    if (nameValidationError) {
      setValidationError(nameValidationError);
      setPlayerName('');
      return;
    }

    if (connectionStatus !== 'connected') {
      setValidationError('Not connected to server. Please wait and try again.');
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
      // Don't set validation error here - let the parent component handle server errors
    }
  };

  const isDisabled = isJoining || connectionStatus !== 'connected';
  const isFormValid = roomCode.trim().length === 4 && validatePlayerName(playerName) === null;

  return (
    <div className="join-screen">
      <div className="join-container">
        <div className="jackbox-logo">
          <h1>Donkeygames</h1>
          <p>Type in that room key and try not to lose!</p>
        </div>

        <form onSubmit={handleSubmit} className="join-form">
          <div className="input-group">
            <label htmlFor="roomCode">Room Code</label>
            <input
              id="roomCode"
              type="text"
              value={roomCode}
              onChange={handleRoomCodeChange}
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
              onChange={handlePlayerNameChange}
              placeholder="Enter your name"
              maxLength={7}
              disabled={isDisabled}
              className="player-name-input"
            />
          </div>
          
          <button 
            type="submit" 
            disabled={isDisabled || !isFormValid}
            className="join-button"
          >
            {isJoining ? 'Joining...' : 'Join Game'}
          </button>
        </form>

        {/* Show validation errors */}
        {validationError && (
          <div className="error-display validation-error">
            {validationError}
          </div>
        )}

        {/* Show server errors */}
        {error && (
          <div className="error-display server-error">
            {error}
          </div>
        )}
        
        <div className="help-text">
        </div>
      </div>
    </div>
  );
}

export default JoinScreen;
