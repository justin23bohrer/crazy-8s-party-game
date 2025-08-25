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

  // Host joins room (for new players scenario)
  socket.on('host-join-room', (data) => {
    try {
      const { roomCode } = data;
      console.log(`🏠 Host ${socket.id} joining room ${roomCode}`);
      
      // Verify room exists
      if (roomManager.rooms.has(roomCode)) {
        socket.join(roomCode);
        console.log(`✅ Host ${socket.id} successfully joined room ${roomCode}`);
      } else {
        console.error(`❌ Room ${roomCode} not found for host join`);
        socket.emit('room-error', `Room ${roomCode} not found`);
      }
    } catch (error) {
      console.error('Error in host-join-room:', error);
      socket.emit('room-error', error.message);
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
        // Handle winning 8 cards differently - no animation needed
        if (result.winningEight) {
          console.log('🏆 WINNING 8 DETECTED - Skipping spiral animation');
          
          // Send updated game state to each player individually FIRST (so they see card removed)
          for (const [playerId] of room.players) {
            const playerSocket = io.sockets.sockets.get(playerId);
            if (playerSocket) {
              const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
              playerSocket.emit('game-state-updated', {
                gameState: {
                  ...playerGameState,
                  isAnimating: true, // Lock UI during winner animation
                  phase: 'winner-animation' // Let phones know game is over
                }
              });
            }
          }
          
          // Notify all players about the card played (without animation)
          io.to(roomCode).emit('card-played', {
            playerName: room.players.get(socket.id).name,
            card: card,
            winningEight: true, // Flag to indicate no spiral animation needed
            gameState: roomManager.getMainScreenGameState(room)
          });

          // Start winner animation sequence immediately
          const winner = room.players.get(socket.id).name;
          console.log('🏆 WINNER DETECTED (winning 8) - Starting winner animation sequence');
          
          // Set animation lock for winner animation (8 seconds)
          roomManager.startAnimationLock(roomCode, 8000);
          
          // Set game phase to winner-animation
          room.gameState.phase = 'winner-animation';
          
          // Send winner event ONLY TO UNITY (main screen)
          const hostSocket = io.sockets.sockets.get(room.hostId);
          if (hostSocket) {
            hostSocket.emit('winner-detected', {
              winner: winner,
              winningEight: true, // Flag for Unity to know this was a winning 8
              players: Array.from(room.players.values()).map(p => ({
                name: p.name,
                cardCount: p.cardCount,
                color: p.color
              }))
            });
          }

          return; // Skip normal processing for winning 8
        }
        
        // CRITICAL: If an 8 was played (non-winning), set animation lock IMMEDIATELY
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
          
          console.log('🏆 WINNER DETECTED - Starting winner animation sequence');
          
          // 1. Set animation lock for winner animation (8 seconds)
          roomManager.startAnimationLock(roomCode, 8000); // 8 second winner animation
          
          // 2. Set game phase to winner-animation (NOT game-over yet)
          room.gameState.phase = 'winner-animation';
          
          // 3. Send winner event ONLY TO UNITY (main screen)
          const hostSocket = io.sockets.sockets.get(room.hostId);
          if (hostSocket) {
            hostSocket.emit('winner-detected', {
              winner: winner,
              players: Array.from(room.players.values()).map(p => ({
                name: p.name,
                cardCount: p.cardCount,
                color: p.color
              }))
            });
          }
          
          // 4. Send updated game state to phones (with animation lock)
          for (const [playerId] of room.players) {
            const playerSocket = io.sockets.sockets.get(playerId);
            if (playerSocket) {
              const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
              playerSocket.emit('game-state-updated', {
                gameState: {
                  ...playerGameState,
                  isAnimating: true, // Lock phone UI during winner animation
                  phase: 'winner-animation'
                }
              });
            }
          }
          
          console.log('🏆 Winner animation started - phones locked, Unity playing animation');
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
        const room = roomManager.rooms.get(roomCode);
        
        console.log(`🎬 Room ${roomCode} phase: ${room.gameState.phase}, isAnimating: ${room.gameState.isAnimating}`);
        
        // CHECK: Don't clear animation lock if we're in winner-animation phase
        if (room && room.gameState.phase === 'winner-animation') {
          console.log('🎬 Skipping animation clear - winner animation in progress');
          return;
        }
        
        console.log(`🎬 Clearing animation lock for room ${roomCode}`);
        roomManager.clearAnimationLock(roomCode);
        
        // Send updated game state to all players to unlock their UI
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

  // Handle first card flip animation completion from Unity main screen
  socket.on('first-card-flip-complete', (data) => {
    console.log('🎬 First card flip animation completion received from Unity main screen');
    
    try {
      let roomCode;
      
      // Try to extract room code from data if provided
      if (data && typeof data === 'string') {
        try {
          const parsed = JSON.parse(data);
          roomCode = parsed.roomCode;
        } catch {
          roomCode = data; // In case it's just a plain room code string
        }
      } else if (data && data.roomCode) {
        roomCode = data.roomCode;
      }
      
      // If no room code provided, find the active game room
      if (!roomCode) {
        roomCode = Array.from(roomManager.rooms.keys()).find(code => {
          const room = roomManager.rooms.get(code);
          return room && room.gameState.phase === 'playing' && room.gameState.isAnimating;
        });
      }
      
      if (roomCode) {
        console.log(`🎬 Handling first card flip completion for room ${roomCode}`);
        const result = roomManager.handleFirstCardFlipComplete(roomCode);
        
        if (result.success) {
          // Send updated game state to all players to re-enable their UI
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
            console.log(`🎬 First card flip complete - phones re-enabled for room ${roomCode}`);
          }
        }
      } else {
        console.log('🎬 No active first card flip animation found to complete');
      }
    } catch (error) {
      console.error('Error handling first card flip completion:', error);
    }
  });

  // Handle winner animation completion from Unity main screen
  socket.on('winner-animation-complete', (data) => {
    console.log('🏆 Winner animation completion received from Unity main screen');
    console.log('🏆 Raw data received:', data);
    
    try {
      // Parse the JSON string from Unity
      let parsedData;
      if (typeof data === 'string') {
        parsedData = JSON.parse(data);
      } else {
        parsedData = data;
      }
      
      const { roomCode, winner } = parsedData;
      console.log(`🏆 Parsed roomCode: ${roomCode}, winner: ${winner}`);
      
      const room = roomManager.rooms.get(roomCode);
      
      console.log(`🏆 Room ${roomCode} phase: ${room?.gameState?.phase}, isAnimating: ${room?.gameState?.isAnimating}`);
      
      if (room && room.gameState.phase === 'winner-animation') {
        console.log(`🏆 Completing winner animation for ${winner} in room ${roomCode}`);
        
        // 1. Clear animation lock
        roomManager.clearAnimationLock(roomCode);
        
        // 2. Set game phase to game-over
        room.gameState.phase = 'game-over';
        
        // 3. Send game-over event to ALL clients (phones + Unity)
        io.to(roomCode).emit('game-over', {
          winner: winner,
          players: Array.from(room.players.values()).map(p => ({
            name: p.name,
            cardCount: p.cardCount,
            color: p.color
          }))
        });
        
        console.log(`🏆 Game over state sent to all clients in room ${roomCode}`);
      } else {
        console.log('🏆 No active winner animation found to complete');
        console.log(`🏆 Room exists: ${!!room}, Room phase: ${room?.gameState?.phase}, Expected: winner-animation`);
      }
    } catch (error) {
      console.error('Error handling winner animation completion:', error);
    }
  });

  // Handle disconnect
  // Host control events - game restart functionality
  socket.on('host-restart-game', (data, callback) => {
    try {
      const { roomCode } = data;
      console.log(`🔄 Host requested restart for room: ${roomCode}`);
      
      const result = roomManager.restartGame(roomCode, socket.id);
      
      if (result.success) {
        const room = roomManager.rooms.get(roomCode);
        
        console.log(`🔄 Game restart successful - sending fresh game state to all clients`);
        
        // 1. Send game-started event to Unity (main screen)
        const hostSocket = io.sockets.sockets.get(room.hostId);
        if (hostSocket) {
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
          
          hostSocket.emit('game-started', {
            gameState: mainScreenGameState
          });
        }
        
        // 2. Send individual game state to each player (phones)
        for (const [playerId] of room.players) {
          const playerSocket = io.sockets.sockets.get(playerId);
          if (playerSocket) {
            const playerGameState = roomManager.getGameStateForPlayer(roomCode, playerId);
            playerSocket.emit('game-started', {
              gameState: playerGameState
            });
          }
        }
        
        console.log(`✅ Game restarted successfully for room: ${roomCode}`);
        
        if (callback && typeof callback === 'function') {
          callback({ success: true });
        }
      } else {
        console.error(`❌ Failed to restart game: ${result.error}`);
        if (callback && typeof callback === 'function') {
          callback({ success: false, error: result.error });
        }
      }
    } catch (error) {
      console.error('Error handling host restart game:', error);
      if (callback && typeof callback === 'function') {
        callback({ success: false, error: error.message });
      }
    }
  });

  socket.on('host-new-players', (data, callback) => {
    try {
      const { roomCode } = data;
      console.log(`👥 Host requested new players for room: ${roomCode}`);
      
      const currentRoom = roomManager.rooms.get(roomCode);
      if (!currentRoom) {
        console.log(`❌ Room ${roomCode} not found`);
        if (callback && typeof callback === 'function') {
          callback({ success: false, error: 'Room not found' });
        }
        return;
      }
      
      console.log(`🏠 Closing room ${roomCode} and creating new one`);
      
      // 1. Notify all current players that room is closing
      io.to(roomCode).emit('room-closed', {
        message: 'Host started a new game with new players',
        reason: 'new-players'
      });
      
      // 2. Create new room with same host
      const result = roomManager.startNewGame(roomCode, currentRoom.hostId);
      
      if (result.success) {
        console.log(`✅ New room created: ${result.newRoomCode}`);
        
        // 3. Send new room code to Unity (host) via callback AND socket event
        if (callback && typeof callback === 'function') {
          callback({ 
            success: true, 
            newRoomCode: result.newRoomCode 
          });
        }
        
        // 4. Also emit socket event to Unity with new room code
        const hostSocket = io.sockets.sockets.get(currentRoom.hostId);
        if (hostSocket) {
          console.log(`📡 Sending new-room-created event to Unity`);
          hostSocket.emit('new-room-created', {
            newRoomCode: result.newRoomCode,
            message: 'New game created with new players'
          });
        } else {
          console.error(`❌ Host socket not found for new room notification`);
        }
        
        console.log(`🏠 Host should now join room ${result.newRoomCode}`);
      } else {
        console.error(`❌ Failed to create new room: ${result.error}`);
        if (callback && typeof callback === 'function') {
          callback({ success: false, error: result.error });
        }
      }
    } catch (error) {
      console.error('Error handling host new players:', error);
      if (callback && typeof callback === 'function') {
        callback({ success: false, error: error.message });
      }
    }
  });

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
