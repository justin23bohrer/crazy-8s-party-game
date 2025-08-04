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

    this.socket.on('card-played', (data) => {
      console.log('Card played:', data);
      this.emit('card-played', data);
    });

    this.socket.on('card-drawn', (data) => {
      console.log('Card drawn:', data);
      this.emit('card-drawn', data);
    });

    this.socket.on('suit-chosen', (data) => {
      console.log('Suit chosen:', data);
      this.emit('suit-chosen', data);
    });

    this.socket.on('round-started', (data) => {
      console.log('Round started:', data);
      this.emit('round-started', data);
    });

    this.socket.on('round-ended', (data) => {
      console.log('Round ended:', data);
      this.emit('round-ended', data);
    });

    this.socket.on('game-ended', (data) => {
      console.log('Game ended:', data);
      this.emit('game-ended', data);
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

  // Crazy 8s specific methods - using direct emit for real-time actions
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
