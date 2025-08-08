import { useState, useEffect } from 'react';
import JoinScreen from './JoinScreen';
import GameScreen from './GameScreen';
import socketService from './SocketService';
import './index.css';

function App() {
  const [gameData, setGameData] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState('disconnected');
  const [error, setError] = useState(null);

  useEffect(() => {
    // Connect to WebSocket server
    socketService.connect();

    // Set up event listeners
    socketService.on('connected', () => {
      setConnectionStatus('connected');
      setError(null);
    });

    socketService.on('disconnected', () => {
      setConnectionStatus('disconnected');
      setIsConnected(false);
    });

    socketService.on('connection_error', (error) => {
      setConnectionStatus('error');
      setError('Failed to connect to server');
    });

    socketService.on('room-closed', (data) => {
      setError('Room was closed by host');
      handleLeave();
    });

    // Cleanup on unmount
    return () => {
      socketService.disconnect();
    };
  }, []);

  const handleJoin = async (joinData) => {
    try {
      setError(null);
      const response = await socketService.joinRoom(joinData.roomCode, joinData.playerName);
      
      // Set game data and mark as connected - include isFirstPlayer from server response
      setGameData({
        ...joinData,
        socketId: socketService.getSocketId(),
        isFirstPlayer: response.isFirstPlayer || false // Add the first player flag from server
      });
      setIsConnected(true);
      
      console.log('Joined game:', response);
      console.log('Is first player:', response.isFirstPlayer);
    } catch (error) {
      console.error('Failed to join game:', error);
      setError(error.message);
    }
  };

  const handleLeave = () => {
    // Reset to join screen
    setGameData(null);
    setIsConnected(false);
    setError(null);
    
    console.log('Left game');
  };

  return (
    <div className="app">
      {/* Connection status indicator */}
      <div className={`connection-indicator ${connectionStatus}`}>
        {connectionStatus === 'connected' && '🟢 Connected'}
        {connectionStatus === 'disconnected' && '🔴 Disconnected'}
        {connectionStatus === 'error' && '⚠️ Connection Error'}
      </div>

      {/* Error message */}
      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      {!isConnected ? (
        <JoinScreen 
          onJoin={handleJoin} 
          connectionStatus={connectionStatus}
          error={error}
        />
      ) : (
        <GameScreen 
          gameData={gameData} 
          onLeave={handleLeave}
          socketService={socketService}
        />
      )}
    </div>
  );
}

export default App;
