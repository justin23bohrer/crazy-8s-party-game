const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const cors = require('cors');
const path = require('path');
const RoomManager = require('./utils/roomManager');

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
  cors: {
    origin: ["http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173"],
    methods: ["GET", "POST"]
  }
});

// Initialize room manager
const roomManager = new RoomManager();

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, '../main-screen')));

// Serve main screen at root
app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, '../main-screen/index.html'));
});

// REST API endpoints
app.post('/api/create-room', (req, res) => {
  const hostId = req.body.hostId || `host_${Date.now()}`;
  const roomCode = roomManager.createRoom(hostId);
  res.json({ roomCode, hostId });
});

app.get('/api/room/:code', (req, res) => {
  const roomData = roomManager.getRoomData(req.params.code);
  if (roomData) {
    res.json(roomData);
  } else {
    res.status(404).json({ error: 'Room not found' });
  }
});

app.get('/api/rooms', (req, res) => {
  res.json(roomManager.getAllRooms());
});

// Socket.IO event handling
io.on('connection', (socket) => {
  console.log(`Client connected: ${socket.id}`);

  // Host creates room
  socket.on('create-room', (callback) => {
    try {
      const roomCode = roomManager.createRoom(socket.id);
      socket.join(roomCode);
      
      const roomData = roomManager.getRoomData(roomCode);
      
      // Support both callback and event patterns
      if (callback && typeof callback === 'function') {
        callback({ success: true, roomCode, roomData });
      } else {
        // Send response via event for Unity
        socket.emit('room-created', { roomCode: roomCode });
      }
      
      console.log(`Host ${socket.id} created room ${roomCode}`);
    } catch (error) {
      console.error('Error creating room:', error);
      if (callback && typeof callback === 'function') {
        callback({ success: false, error: error.message });
      } else {
        socket.emit('room-error', error.message);
      }
    }
  });

  // Player joins room
  socket.on('join-room', (data, callback) => {
    try {
      const { roomCode, playerName } = data;
      const result = roomManager.joinRoom(roomCode, socket.id, playerName);
      
      if (result.success) {
        socket.join(roomCode);
        
        // Notify all clients in room about new player
        io.to(roomCode).emit('player-joined', {
          playerName: result.player.name,
          playerColor: result.player.color,
          isFirstPlayer: result.player.isFirstPlayer,
          players: Array.from(roomManager.rooms.get(roomCode).players.values()).map(p => ({
            name: p.name,
            color: p.color,
            cardCount: p.cardCount || 0,
            isFirstPlayer: p.isFirstPlayer || false
          }))
        });
        
        callback({ 
          success: true, 
          roomData: result.roomData, 
          isFirstPlayer: result.player.isFirstPlayer,
          playerColor: result.player.color 
        });
      } else {
        callback({ success: false, error: result.error });
      }
    } catch (error) {
      console.error('Error joining room:', error);
      callback({ success: false, error: error.message });
    }
  });

  // Host starts game
  socket.on('start-game', (data, callback) => {
    try {
      // Handle both string roomCode and object with roomCode
      const roomCode = typeof data === 'string' ? data : data.roomCode;
      console.log(`Starting game for room: ${roomCode}`);
      
      const result = roomManager.startGame(roomCode, socket.id);
      
      if (result.success) {
        // Get the room to access player information
        const room = roomManager.rooms.get(roomCode);
        
        // Send individual game state to each player
        for (const [playerId] of room.players) {
          const playerSocket = io.sockets.sockets.get(playerId);
          if (playerSocket) {
            const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
            playerSocket.emit('game-started', {
              gameState: playerGameState
            });
          }
        }
        
        // Send overall game state to main screen (host)
        const mainScreenGameState = {
          currentPlayer: Array.from(room.players.values())[result.gameState.currentPlayer]?.name,
          topCard: result.gameState.lastPlayedCard,
          currentColor: result.gameState.currentColor,
          deckCount: result.gameState.deck.length,
          players: Array.from(room.players.values()).map(p => ({
            name: p.name,
            color: p.color,
            cardCount: p.cardCount
          }))
        };
        
        // Emit to all clients in room (including Unity main screen)
        io.to(roomCode).emit('game-started', {
          gameState: mainScreenGameState
        });
        
        // Send callback response if callback provided
        if (callback && typeof callback === 'function') {
          callback({ success: true });
        }
      } else {
        console.error(`Failed to start game: ${result.error}`);
        if (callback && typeof callback === 'function') {
          callback({ success: false, error: result.error });
        }
        // Also emit error to room
        io.to(roomCode).emit('game-error', { error: result.error });
      }
    } catch (error) {
      console.error('Error starting game:', error);
      if (callback && typeof callback === 'function') {
        callback({ success: false, error: error.message });
      }
      // Try to emit error to room if we have roomCode
      const roomCode = typeof data === 'string' ? data : data.roomCode;
      if (roomCode) {
        io.to(roomCode).emit('game-error', { error: error.message });
      }
    }
  });

  // Player submits action (answer, etc.)
  socket.on('player-action', (data, callback) => {
    try {
      const { roomCode, action } = data;
      const room = roomManager.rooms.get(roomCode);
      if (!room) return;
      
      // CHECK: Block player actions during animation
      if (roomManager.isAnimationBlocking(room)) {
        console.log('🚫 Blocking player action - animation in progress');
        callback({ success: false, error: 'Animation in progress' });
        return;
      }
      
      const result = roomManager.handlePlayerAction(roomCode, socket.id, action);
      
      if (result.success) {
        // Notify room about the action
        socket.to(roomCode).emit('player-action-received', {
          playerId: socket.id,
          action,
          result
        });
        
        // If all players submitted, move to next phase
        if (result.allSubmitted) {
          setTimeout(() => {
            endRound(roomCode);
          }, 1000);
        }
        
        callback({ success: true, message: result.message });
      } else {
        callback({ success: false, error: result.error });
      }
    } catch (error) {
      console.error('Error handling player action:', error);
      callback({ success: false, error: error.message });
    }
  });

  // Get room status
  socket.on('get-room-status', (roomCode, callback) => {
    const roomData = roomManager.getRoomData(roomCode);
    callback(roomData);
  });

  // Crazy 8s specific events
  socket.on('play-card', (data) => {
    try {
      const { roomCode, card, chosenColor } = data; // Extract chosenColor from data
      const room = roomManager.rooms.get(roomCode);
      if (!room) return;

      // CHECK: Block card playing during animation, BUT ALLOW 8-card color choices
      if (roomManager.isAnimationBlocking(room)) {
        // If this is an 8-card with chosenColor, allow it (completing previous 8-card play)
        if (card.rank === '8' && chosenColor) {
          console.log('✅ Allowing 8-card color choice during animation');
        } else {
          console.log('🚫 Blocking card play - animation in progress');
          return; // Silently reject non-8-card plays during animation
        }
      }

      const playerIndex = roomManager.getPlayerIndex(room, socket.id);
      if (playerIndex === -1) return;

      const cardIndex = room.gameState.playerHands[playerIndex].findIndex(c => 
        c.color === card.color && c.rank === card.rank
      );

      const result = roomManager.handlePlayCard(room, playerIndex, cardIndex, chosenColor); // Pass chosenColor to handlePlayCard
      
      if (result.success) {
        // CRITICAL: If an 8 was played, set animation lock IMMEDIATELY
        if (card.rank === '8') {
          console.log('🎬 8 card played - setting animation lock IMMEDIATELY');
          roomManager.startAnimationLock(roomCode, 3300); // 3.3 seconds for spiral animation
          
          // Send animation lock state to all players IMMEDIATELY
          for (const [playerId] of room.players) {
            const playerSocket = io.sockets.sockets.get(playerId);
            if (playerSocket) {
              const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
              playerSocket.emit('game-state-updated', {
                gameState: {
                  ...playerGameState,
                  isAnimating: true // Explicitly set this to ensure it's received
                }
              });
            }
          }
        }

        // If an 8 was played with a chosen color, emit color-chosen event
        if (card.rank === '8' && chosenColor) {
          io.to(roomCode).emit('color-chosen', {
            playerName: room.players.get(socket.id).name,
            color: chosenColor,
            card: card,
            gameState: roomManager.getMainScreenGameState(room)
          });
        }
        
        // Notify all players about the card played
        io.to(roomCode).emit('card-played', {
          playerName: room.players.get(socket.id).name,
          card: card,
          gameState: roomManager.getMainScreenGameState(room)
        });

        // Send updated game state to each player individually
        for (const [playerId] of room.players) {
          const playerSocket = io.sockets.sockets.get(playerId);
          if (playerSocket) {
            const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
            playerSocket.emit('game-state-updated', {
              gameState: playerGameState
            });
          }
        }

        // Check for win condition
        if (room.gameState.playerHands[playerIndex].length === 0) {
          const winner = room.players.get(socket.id).name;
          io.to(roomCode).emit('game-ended', {
            winner: winner,
            players: Array.from(room.players.values()).map(p => ({
              name: p.name,
              cardCount: p.cardCount,
              color: p.color
            }))
          });
        }
      }
    } catch (error) {
      console.error('Error playing card:', error);
    }
  });

  socket.on('draw-card', (data) => {
    try {
      const { roomCode } = data;
      const room = roomManager.rooms.get(roomCode);
      if (!room) return;

      // CHECK: Block card drawing during animation
      if (roomManager.isAnimationBlocking(room)) {
        console.log('🚫 Blocking card draw - animation in progress');
        return; // Silently reject the draw
      }

      const playerIndex = roomManager.getPlayerIndex(room, socket.id);
      if (playerIndex === -1) return;

      const result = roomManager.handleDrawCard(room, playerIndex);
      
      if (result.success) {
        // Notify all players about the card drawn
        io.to(roomCode).emit('card-drawn', {
          playerName: room.players.get(socket.id).name,
          gameState: roomManager.getMainScreenGameState(room)
        });

        // Send updated game state to each player individually
        for (const [playerId] of room.players) {
          const playerSocket = io.sockets.sockets.get(playerId);
          if (playerSocket) {
            const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
            playerSocket.emit('game-state-updated', {
              gameState: playerGameState
            });
          }
        }
      }
    } catch (error) {
      console.error('Error drawing card:', error);
    }
  });

  socket.on('choose-color', (data) => { // Changed from choose-suit to choose-color
    try {
      const { roomCode, color } = data; // Changed from suit to color
      const room = roomManager.rooms.get(roomCode);
      if (!room) return;

      room.gameState.chosenColor = color; // Changed from chosenSuit to chosenColor
      room.gameState.currentColor = color; // Changed from currentSuit to currentColor

      // NOTE: Animation lock is already set when the 8 card was played

      // Notify all players about the color choice (includes animation state)
      io.to(roomCode).emit('color-chosen', { // Changed from suit-chosen to color-chosen
        playerName: room.players.get(socket.id).name,
        color: color, // Changed from suit to color
        gameState: roomManager.getMainScreenGameState(room)
      });

      // Send updated game state to each player individually (includes animation lock)
      for (const [playerId] of room.players) {
        const playerSocket = io.sockets.sockets.get(playerId);
        if (playerSocket) {
          const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
          playerSocket.emit('game-state-updated', {
            gameState: playerGameState
          });
        }
      }
    } catch (error) {
      console.error('Error choosing color:', error);
    }
  });

  // Handle animation completion from Unity main screen
  socket.on('animation-complete', () => {
    console.log('🎬 Animation completion received from Unity main screen');
    
    try {
      const roomCode = Array.from(roomManager.rooms.keys()).find(code => {
        const room = roomManager.rooms.get(code);
        return room && room.gameState.isAnimating;
      });
      
      if (roomCode) {
        console.log(`🎬 Clearing animation lock for room ${roomCode}`);
        roomManager.clearAnimationLock(roomCode);
        
        // Send updated game state to all players to unlock their UI
        const room = roomManager.rooms.get(roomCode);
        if (room) {
          for (const [playerId] of room.players) {
            const playerSocket = io.sockets.sockets.get(playerId);
            if (playerSocket) {
              const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
              playerSocket.emit('game-state-updated', {
                gameState: playerGameState
              });
            }
          }
        }
      } else {
        console.log('🎬 No active animation found to clear');
      }
    } catch (error) {
      console.error('Error handling animation completion:', error);
    }
  });

  // Handle disconnect
  socket.on('disconnect', () => {
    console.log(`Client disconnected: ${socket.id}`);
    
    const result = roomManager.handleDisconnect(socket.id);
    if (result) {
      if (result.roomClosed) {
        // Notify all players that room was closed
        io.to(result.roomCode).emit('room-closed', {
          message: 'Host disconnected, room closed'
        });
      } else if (result.roomCode) {
        // Notify room that player left
        io.to(result.roomCode).emit('player-left', {
          playerName: result.playerName,
          playerColor: result.playerColor,
          players: Array.from(roomManager.rooms.get(result.roomCode)?.players.values() || []).map(p => ({
            name: p.name,
            color: p.color,
            cardCount: p.cardCount
          }))
        });
      }
    }
  });
});

// Game timer functions
function startGameTimer(roomCode) {
  const timer = setInterval(() => {
    const roomData = roomManager.getRoomData(roomCode);
    if (!roomData || roomData.gameState.phase !== 'playing') {
      clearInterval(timer);
      return;
    }

    if (roomData.gameState.timeLeft <= 0) {
      clearInterval(timer);
      endRound(roomCode);
      return;
    }

    // Decrease time and emit update
    const room = roomManager.rooms.get(roomCode);
    if (room) {
      room.gameState.timeLeft--;
      io.to(roomCode).emit('timer-update', {
        timeLeft: room.gameState.timeLeft
      });
    }
  }, 1000);
}

function endRound(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  room.gameState.phase = 'round-end';
  
  // Collect all responses
  const responses = Array.from(room.gameState.responses.entries()).map(([playerId, response]) => {
    const player = room.players.get(playerId);
    return {
      playerId,
      playerName: player ? player.name : 'Unknown',
      text: response.text,
      submittedAt: response.submittedAt
    };
  });

  // Emit round results
  io.to(roomCode).emit('round-ended', {
    responses,
    roundNumber: room.gameState.round
  });

  // Start next round after delay
  setTimeout(() => {
    startNextRound(roomCode);
  }, 5000);
}

function startNextRound(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  room.gameState.round++;
  
  // End game after 3 rounds
  if (room.gameState.round > 3) {
    endGame(roomCode);
    return;
  }

  // Start new round
  room.gameState.phase = 'playing';
  room.gameState.currentPrompt = roomManager.getRandomPrompt();
  room.gameState.timeLeft = 30;
  room.gameState.responses.clear();

  io.to(roomCode).emit('round-started', {
    gameState: room.gameState
  });

  // Start timer for new round
  startGameTimer(roomCode);
}

function endGame(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  room.gameState.phase = 'game-over';

  // Calculate final scores (for now, just participation points)
  const finalScores = Array.from(room.players.values()).map(player => ({
    playerId: player.id,
    playerName: player.name,
    score: player.score || Math.floor(Math.random() * 100) // Random score for demo
  })).sort((a, b) => b.score - a.score);

  io.to(roomCode).emit('game-ended', {
    finalScores,
    winner: finalScores[0]
  });
}

// Cleanup old rooms every hour
setInterval(() => {
  roomManager.cleanupOldRooms();
}, 60 * 60 * 1000);

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
  console.log(`🃏 Crazy 8s Game Server running on port ${PORT}`);
  console.log(`📱 Phone clients should connect to: http://localhost:${PORT}`);
  console.log(`🖥️  Main screen available at: http://localhost:${PORT}`);
});
