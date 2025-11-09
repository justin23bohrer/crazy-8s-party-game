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

  // Host starts Over Under game
  socket.on('start-game', (data, callback) => {
    try {
      // Handle both string roomCode and object with roomCode
      const roomCode = typeof data === 'string' ? data : data.roomCode;
      console.log(`Starting Over Under game for room: ${roomCode}`);
      
      const result = roomManager.startOverUnderGame(roomCode, socket.id);
      
      if (result.success) {
        // Send game started confirmation to all players
        io.to(roomCode).emit('game-started', {
          message: 'Over Under game started!',
          totalRounds: result.totalRounds
        });

        // Start the first round immediately
        setTimeout(() => {
          startNextOverUnderRound(roomCode);
        }, 2000); // 2 second delay to let players see the start message
        
        // Send callback response if callback provided
        if (callback && typeof callback === 'function') {
          callback({ success: true });
        }
      } else {
        console.error(`Failed to start Over Under game: ${result.error}`);
        if (callback && typeof callback === 'function') {
          callback({ success: false, error: result.error });
        }
        // Also emit error to room
        io.to(roomCode).emit('game-error', { error: result.error });
      }
    } catch (error) {
      console.error('Error starting Over Under game:', error);
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

  // Over Under specific events
  socket.on('submit-answer', (data, callback) => {
    try {
      const { roomCode, answer } = data;
      console.log(`Player ${socket.id} submitted answer: ${answer} for room ${roomCode}`);
      
      const result = roomManager.handleSubmitAnswer(roomCode, socket.id, answer);
      
      if (result.success) {
        // Notify all players that answerer has submitted their guess
        io.to(roomCode).emit('voting-phase', {
          question: result.question,
          playerAnswer: result.answer,
          answerer: result.answererName,
          votingTimeLeft: 30
        });
        
        // Start voting timer
        startVotingTimer(roomCode);
        
        if (callback) callback({ success: true, message: result.message });
      } else {
        console.error(`Failed to submit answer: ${result.error}`);
        if (callback) callback({ success: false, error: result.error });
      }
    } catch (error) {
      console.error('Error handling submit answer:', error);
      if (callback) callback({ success: false, error: error.message });
    }
  });

  socket.on('submit-vote', (data, callback) => {
    try {
      const { roomCode, vote } = data;
      console.log(`Player ${socket.id} voted: ${vote} for room ${roomCode}`);
      
      const result = roomManager.handleSubmitVote(roomCode, socket.id, vote);
      
      if (result.success) {
        // Notify room about the vote (without revealing who voted what)
        io.to(roomCode).emit('vote-submitted', {
          votesSubmitted: result.votesSubmitted,
          totalVotesNeeded: result.totalVotesNeeded
        });
        
        // Check if all votes are in
        if (result.allVotesIn) {
          console.log(`All votes received for room ${roomCode}, ending round`);
          setTimeout(() => {
            endVotingRound(roomCode);
          }, 1000); // Brief delay before showing results
        }
        
        if (callback) callback({ success: true, message: result.message });
      } else {
        console.error(`Failed to submit vote: ${result.error}`);
        if (callback) callback({ success: false, error: result.error });
      }
    } catch (error) {
      console.error('Error handling submit vote:', error);
      if (callback) callback({ success: false, error: error.message });
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

// Over Under game timer and round management functions
function startVotingTimer(roomCode) {
  const timer = setInterval(() => {
    const room = roomManager.rooms.get(roomCode);
    if (!room || !room.gameState.isVotingActive) {
      clearInterval(timer);
      return;
    }

    room.gameState.votingTimeLeft--;
    
    // Send timer update to all players
    io.to(roomCode).emit('voting-timer-update', {
      timeLeft: room.gameState.votingTimeLeft
    });

    // Check if time is up or all votes are in
    if (room.gameState.votingTimeLeft <= 0) {
      clearInterval(timer);
      console.log(`Voting time expired for room ${roomCode}`);
      endVotingRound(roomCode);
    }
  }, 1000);
}

function endVotingRound(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  console.log(`Ending voting round for room ${roomCode}`);
  
  // Calculate scores and get results
  const results = roomManager.calculateRoundResults(roomCode);
  
  if (results.success) {
    // Send round results to all players
    io.to(roomCode).emit('round-results', {
      question: results.question,
      playerAnswer: results.playerAnswer,
      correctAnswer: results.correctAnswer,
      correctVote: results.correctVote,
      winners: results.winners,
      votes: results.votes,
      scores: results.currentScores
    });

    // Update Unity screen with scoreboard
    io.to(roomCode).emit('update-scoreboard', {
      scores: results.currentScores,
      roundComplete: true
    });

    // Check if game is complete
    if (results.gameComplete) {
      setTimeout(() => {
        endOverUnderGame(roomCode);
      }, 5000); // Show results for 5 seconds before ending game
    } else {
      // Start next round after delay
      setTimeout(() => {
        startNextOverUnderRound(roomCode);
      }, 5000);
    }
  }
}

function startNextOverUnderRound(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  console.log(`Starting next round for room ${roomCode}`);
  
  const result = roomManager.nextRound(roomCode);
  
  if (result.success) {
    // Send new question to all players
    io.to(roomCode).emit('show-question', {
      question: result.question,
      answerer: result.answerer.name,
      answererId: result.answerer.id,
      roundNumber: result.roundNumber,
      totalRounds: room.gameState.totalRounds
    });

    console.log(`Round ${result.roundNumber} started - ${result.answerer.name} is the answerer`);
  } else if (result.gameOver) {
    endOverUnderGame(roomCode);
  }
}

function endOverUnderGame(roomCode) {
  const room = roomManager.rooms.get(roomCode);
  if (!room) return;

  console.log(`Ending Over Under game for room ${roomCode}`);
  
  const finalResults = roomManager.getFinalResults(roomCode);
  
  if (finalResults.success) {
    // Set game phase to game-over
    room.gameState.phase = 'game-over';
    
    // Send final results to all players
    io.to(roomCode).emit('game-over', {
      winner: finalResults.winner,
      finalScores: finalResults.finalScores,
      roundHistory: finalResults.roundHistory
    });

    console.log(`Game over! Winner: ${finalResults.winner.playerName} with ${finalResults.winner.totalScore} points`);
  }
}

// Cleanup old rooms every hour
setInterval(() => {
  roomManager.cleanupOldRooms();
}, 60 * 60 * 1000);

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
  console.log(`� Over Under Game Server running on port ${PORT}`);
  console.log(`📱 Phone clients should connect to: http://localhost:${PORT}`);
  console.log(`🖥️  Main screen available at: http://localhost:${PORT}`);
});
