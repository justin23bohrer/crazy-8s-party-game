import { io } from 'socket.io-client';

class SocketService {
  constructor() {
    this.socket = null;
    this.connected = false;
    this.eventListeners = new Map();
  }

  connect(serverUrl = 'http://localhost:3000') {
    console.log('Connecting to server:', serverUrl);
    
    this.socket = io(serverUrl, {
      transports: ['websocket', 'polling'],
      timeout: 5000
    });

    this.socket.on('connect', () => {
      console.log('Connected to server:', this.socket.id);
      this.connected = true;
      this.emit('connected', this.socket.id);
    });
    
    // Debug: Log all incoming events
    this.socket.onAny((eventName, ...args) => {
      console.log(`📡 PHONE: Received event '${eventName}':`, args);
    });

    this.socket.on('disconnect', (reason) => {
      console.log('Disconnected from server:', reason);
      this.connected = false;
      this.emit('disconnected', reason);
    });

    this.socket.on('connect_error', (error) => {
      console.error('Connection error:', error);
      this.connected = false;
      this.emit('connection_error', error);
    });

    // Game event listeners
    this.socket.on('game-started', (data) => {
      console.log('Game started:', data);
      this.emit('game-started', data);
    });

    this.socket.on('game-state-updated', (data) => {
      console.log('Game state updated:', data);
      this.emit('game-state-updated', data);
    });



    this.socket.on('show-question', (data) => {
      console.log('Question received:', data);
      this.emit('show-question', data);
    });

    this.socket.on('voting-phase', (data) => {
      console.log('Voting phase started:', data);
      this.emit('voting-phase', data);
    });

    this.socket.on('vote-submitted', (data) => {
      console.log('Vote submitted update:', data);
      this.emit('vote-submitted', data);
    });

    this.socket.on('voting-timer-update', (data) => {
      console.log('Voting timer update:', data);
      this.emit('voting-timer-update', data);
    });

    this.socket.on('round-results', (data) => {
      console.log('Round results:', data);
      this.emit('round-results', data);
    });

    this.socket.on('update-scoreboard', (data) => {
      console.log('Scoreboard update:', data);
      this.emit('update-scoreboard', data);
    });

    this.socket.on('game-over', (data) => {
      console.log('🏆 Game over received in SocketService:', data);
      this.emit('game-over', data);
    });

    this.socket.on('player-action', (data) => {
      console.log('Player action:', data);
      this.emit('player-action', data);
    });

    this.socket.on('timer-update', (data) => {
      this.emit('timer-update', data);
    });

    this.socket.on('room-closed', (data) => {
      console.log('Room closed:', data);
      this.emit('room-closed', data);
    });

    this.socket.on('error', (data) => {
      console.log('Game error:', data);
      this.emit('error', data);
    });

    return this.socket;
  }

  disconnect() {
    if (this.socket) {
      this.socket.disconnect();
      this.socket = null;
      this.connected = false;
    }
  }

  // Join room
  joinRoom(roomCode, playerName) {
    return new Promise((resolve, reject) => {
      if (!this.connected) {
        reject(new Error('Not connected to server'));
        return;
      }

      this.socket.emit('join-room', { roomCode, playerName }, (response) => {
        if (response.success) {
          resolve(response);
        } else {
          reject(new Error(response.error));
        }
      });
    });
  }

  // Over Under specific methods - using direct emit for real-time actions
  emitGameAction(event, data) {
    if (this.socket) {
      this.socket.emit(event, data);
    }
  }

  // Event listener management
  on(event, callback) {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event).push(callback);
  }

  off(event, callback) {
    if (this.eventListeners.has(event)) {
      const listeners = this.eventListeners.get(event);
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  emit(event, data) {
    if (this.eventListeners.has(event)) {
      this.eventListeners.get(event).forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error('Error in event listener:', error);
        }
      });
    }
  }

  getSocketId() {
    return this.socket ? this.socket.id : null;
  }
}

// Create singleton instance
const socketService = new SocketService();

export default socketService;
