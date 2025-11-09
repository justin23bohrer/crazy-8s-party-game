// Room Manager - Handles game rooms, players, and Over Under game state
const OverUnderGameLogic = require('./overUnderGameLogic');

class RoomManager {
  constructor() {
    this.rooms = new Map(); // { roomCode: room object }
    this.playerToRoom = new Map(); // { playerId: roomCode } for quick lookup
    this.gameLogic = new OverUnderGameLogic();
    
    // Available player colors (max 4 players)
    this.availableColors = ['red', 'blue', 'green', 'yellow'];
  }

  // Generate unique 4-letter room code
  generateRoomCode() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    let code;
    do {
      code = Array.from({ length: 4 }, () => 
        chars[Math.floor(Math.random() * chars.length)]
      ).join('');
    } while (this.rooms.has(code)); // Ensure uniqueness
    return code;
  }

  // Create new room
  createRoom(hostId) {
    const roomCode = this.generateRoomCode();
    const room = {
      hostId,
      players: new Map(), // { playerId: { name, id, connected: bool, color: string, score: 0 } }
      assignedColors: new Set(), // Track which colors are taken
      gameState: {
        phase: 'lobby', // lobby, playing, game-over
        currentRound: 1,
        totalRounds: 0,
        roundsCompleted: 0,
        currentAnswerer: null,
        currentQuestion: null,
        currentAnswer: null,
        correctAnswer: null,
        votes: new Map(),
        scores: new Map(),
        roundResults: [],
        votingTimeLeft: 30,
        isVotingActive: false,
        answererSubmitted: false,
        usedQuestions: new Set()
      },
      created: new Date(),
      maxPlayers: 4 // Max 4 players for color assignment (red, blue, green, yellow)
    };
    
    this.rooms.set(roomCode, room);
    this.playerToRoom.set(hostId, roomCode);
    
    console.log(`Over Under room created: ${roomCode} by host ${hostId}`);
    return roomCode;
  }

  // Assign a random available color to a player
  assignPlayerColor(room) {
    const availableColors = this.availableColors.filter(color => !room.assignedColors.has(color));
    
    if (availableColors.length === 0) {
      throw new Error('No colors available');
    }
    
    // Pick a random color from available ones
    const randomIndex = Math.floor(Math.random() * availableColors.length);
    const assignedColor = availableColors[randomIndex];
    
    room.assignedColors.add(assignedColor);
    return assignedColor;
  }

  // Release a player's color when they leave
  releasePlayerColor(room, color) {
    if (color) {
      room.assignedColors.delete(color);
    }
  }

  // Player joins room
  joinRoom(roomCode, playerId, playerName) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    if (room.players.size >= room.maxPlayers) {
      return { success: false, error: 'Room is full' };
    }

    if (room.gameState.phase !== 'lobby') {
      return { success: false, error: 'Game already in progress' };
    }

    // Check if name is already taken
    for (const [id, player] of room.players) {
      if (player.name.toLowerCase() === playerName.toLowerCase()) {
        return { success: false, error: 'Name already taken' };
      }
    }

    // Assign a color to the player
    let playerColor;
    try {
      playerColor = this.assignPlayerColor(room);
    } catch (error) {
      return { success: false, error: 'No available colors (room full)' };
    }

    const player = {
      id: playerId,
      name: playerName,
      connected: true,
      score: 0,
      color: playerColor,
      joinedAt: new Date(),
      isFirstPlayer: room.players.size === 0 // True if this is the first player
    };

    room.players.set(playerId, player);
    this.playerToRoom.set(playerId, roomCode);

    console.log(`Player ${playerName} (${playerId}) joined Over Under room ${roomCode} with color ${playerColor}${player.isFirstPlayer ? ' (FIRST PLAYER)' : ''}`);
    return { success: true, player, roomData: this.getRoomData(roomCode) };
  }

  // Get current game state for Over Under display
  getCurrentGameState(roomCode, playerId = null) {
    const room = this.rooms.get(roomCode);
    if (!room) return null;

    const gameState = this.gameLogic.getCurrentGameState(room.gameState, room.players);
    
    // Add player-specific information if playerId provided
    if (playerId) {
      gameState.isAnswerer = this.gameLogic.isPlayerAnswerer(room.gameState, playerId);
      gameState.canVote = this.gameLogic.canPlayerVote(room.gameState, playerId);
      gameState.playerVote = room.gameState.votes.get(playerId) || null;
    }
    
    return gameState;
  }



  // Start Over Under game
  startOverUnderGame(roomCode, requesterId) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    // Check if requester is host OR first player
    const requesterPlayer = room.players.get(requesterId);
    const isAuthorized = room.hostId === requesterId || (requesterPlayer && requesterPlayer.isFirstPlayer);
    
    if (!isAuthorized) {
      return { success: false, error: 'Not authorized - only host or first player can start the game' };
    }

    if (room.players.size < 2) {
      return { success: false, error: 'Need at least 2 players to start' };
    }

    // Initialize Over Under game
    room.gameState = this.gameLogic.createGame(room.players);
    room.gameState.totalRounds = room.players.size; // One round per player
    
    // Initialize player scores
    for (const [playerId, player] of room.players) {
      room.gameState.scores.set(playerId, 0);
      player.score = 0;
    }

    console.log(`Over Under game started in room ${roomCode} with ${room.players.size} players, ${room.gameState.totalRounds} rounds`);
    return { 
      success: true, 
      gameState: room.gameState,
      totalRounds: room.gameState.totalRounds
    };
  }

  // Start next round
  nextRound(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    const result = this.gameLogic.nextRound(room.gameState, room.players);
    
    if (result.success) {
      console.log(`Round ${room.gameState.currentRound} started in room ${roomCode}`);
      return {
        success: true,
        question: result.question,
        answerer: result.answerer,
        roundNumber: result.roundNumber
      };
    } else if (result.gameOver) {
      return { success: false, gameOver: true };
    } else {
      return { success: false, error: 'Failed to start next round' };
    }
  }

  // Handle answerer submitting their guess
  handleSubmitAnswer(roomCode, playerId, answer) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    const result = this.gameLogic.submitAnswer(room.gameState, playerId, answer);
    
    if (result.success) {
      const player = room.players.get(playerId);
      return {
        success: true,
        answer: result.answer,
        message: result.message,
        question: room.gameState.currentQuestion.question,
        answererName: player ? player.name : 'Unknown'
      };
    }
    
    return result;
  }

  // Handle other players voting
  handleSubmitVote(roomCode, playerId, vote) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    const result = this.gameLogic.submitVote(room.gameState, playerId, vote);
    
    if (result.success) {
      const allVotesIn = this.gameLogic.areAllVotesIn(room.gameState, room.players);
      
      return {
        success: true,
        vote: result.vote,
        message: result.message,
        votesSubmitted: room.gameState.votes.size,
        totalVotesNeeded: room.players.size - 1,
        allVotesIn: allVotesIn
      };
    }
    
    return result;
  }

  // Calculate round results
  calculateRoundResults(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    const results = this.gameLogic.calculateScores(room.gameState, room.players);
    const roundEnd = this.gameLogic.endRound(room.gameState);
    
    // Update player scores for quick access
    for (const [playerId, score] of room.gameState.scores) {
      const player = room.players.get(playerId);
      if (player) {
        player.score = score;
      }
    }

    return {
      success: true,
      question: results.question,
      playerAnswer: results.playerAnswer,
      correctAnswer: results.correctAnswer,
      correctVote: results.correctVote,
      winners: results.winners,
      votes: results.votes,
      currentScores: Array.from(room.gameState.scores.entries()).map(([playerId, score]) => {
        const player = room.players.get(playerId);
        return {
          playerId: playerId,
          playerName: player ? player.name : 'Unknown',
          playerColor: player ? player.color : 'gray',
          score: score
        };
      }),
      gameComplete: roundEnd.gameComplete
    };
  }

  // Get final results
  getFinalResults(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    const results = this.gameLogic.getFinalResults(room.gameState, room.players);
    
    return {
      success: true,
      winner: results.winner,
      finalScores: results.finalScores,
      roundHistory: results.roundHistory
    };
  }



  // Get room data for clients (for Over Under)
  getRoomData(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return null;

    return {
      code: roomCode,
      playerCount: room.players.size,
      maxPlayers: room.maxPlayers,
      players: Array.from(room.players.values()).map(p => ({
        id: p.id,
        name: p.name,
        connected: p.connected,
        color: p.color,
        score: p.score || 0
      })),
      gameState: {
        phase: room.gameState.phase,
        currentRound: room.gameState.currentRound,
        totalRounds: room.gameState.totalRounds,
        currentAnswerer: room.gameState.currentAnswerer,
        isVotingActive: room.gameState.isVotingActive,
        votingTimeLeft: room.gameState.votingTimeLeft
      }
    };
  }

  // Get game state for a specific player (Over Under)
  getGameStateForPlayer(roomCode, playerId) {
    const room = this.rooms.get(roomCode);
    if (!room) return null;

    const gameState = this.getCurrentGameState(roomCode, playerId);
    
    return {
      ...gameState,
      roomCode: roomCode,
      playerId: playerId,
      players: Array.from(room.players.values()).map(p => ({
        name: p.name,
        color: p.color,
        score: p.score || 0,
        id: p.id
      }))
    };
  }

  // Handle player disconnect
  handleDisconnect(playerId) {
    const roomCode = this.playerToRoom.get(playerId);
    if (!roomCode) return;

    const room = this.rooms.get(roomCode);
    if (!room) return;

    // If host disconnects, end the game
    if (room.hostId === playerId) {
      console.log(`Host disconnected, closing room ${roomCode}`);
      this.rooms.delete(roomCode);
      // Remove all players from lookup
      for (const [pid] of room.players) {
        this.playerToRoom.delete(pid);
      }
      this.playerToRoom.delete(playerId);
      return { roomClosed: true, roomCode };
    }

    // Mark player as disconnected and release their color
    const player = room.players.get(playerId);
    if (player) {
      const playerName = player.name;
      const playerColor = player.color;
      
      // Release the player's color
      this.releasePlayerColor(room, playerColor);
      
      room.players.delete(playerId);
      this.playerToRoom.delete(playerId);
      console.log(`Player ${playerName} (${playerColor}) disconnected from room ${roomCode}`);
      return { roomCode, playerName, playerColor };
    }

    return null;
  }

  // Clean up old rooms (run periodically)
  cleanupOldRooms() {
    const maxAge = 2 * 60 * 60 * 1000; // 2 hours
    const now = new Date();

    for (const [roomCode, room] of this.rooms) {
      if (now - room.created > maxAge) {
        console.log(`Cleaning up old room: ${roomCode}`);
        // Remove all players from lookup
        for (const [playerId] of room.players) {
          this.playerToRoom.delete(playerId);
        }
        this.playerToRoom.delete(room.hostId);
        this.rooms.delete(roomCode);
      }
    }
  }

  // Get all rooms (for debugging)
  getAllRooms() {
    const rooms = [];
    for (const [code, room] of this.rooms) {
      rooms.push({
        code,
        playerCount: room.players.size,
        phase: room.gameState.phase,
        created: room.created
      });
    }
    return rooms;
  }

  // Get game state for main screen display (Over Under)
  getMainScreenGameState(room) {
    const currentGameState = this.gameLogic.getCurrentGameState(room.gameState, room.players);
    
    return {
      phase: currentGameState.phase,
      currentRound: currentGameState.currentRound,
      totalRounds: currentGameState.totalRounds,
      question: currentGameState.question,
      answerer: currentGameState.answerer,
      playerAnswer: currentGameState.playerAnswer,
      isVotingActive: currentGameState.isVotingActive,
      votingTimeLeft: currentGameState.votingTimeLeft,
      votesSubmitted: currentGameState.votesSubmitted,
      totalVotesNeeded: currentGameState.totalVotesNeeded,
      players: Array.from(room.players.values()).map(p => ({
        name: p.name,
        color: p.color,
        score: p.score || 0
      })),
      scores: currentGameState.scores
    };
  }

  // Restart Over Under game with same players
  restartGame(roomCode, requesterId) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    // Check if requester is host OR first player
    const requesterPlayer = room.players.get(requesterId);
    const isAuthorized = room.hostId === requesterId || (requesterPlayer && requesterPlayer.isFirstPlayer);
    
    if (!isAuthorized) {
      return { success: false, error: 'Not authorized - only host or first player can restart the game' };
    }

    if (room.players.size < 2) {
      return { success: false, error: 'Need at least 2 players to restart' };
    }

    console.log(`🔄 Restarting Over Under game in room ${roomCode} with same players`);

    // Reset game state with fresh Over Under game
    room.gameState = this.gameLogic.createGame(room.players);
    room.gameState.totalRounds = room.players.size;
    
    // Reset player scores
    for (const [playerId, player] of room.players) {
      room.gameState.scores.set(playerId, 0);
      player.score = 0;
    }

    console.log(`✅ Over Under game restarted successfully in room ${roomCode}`);
    return { success: true, gameState: room.gameState };
  }

  // Start new Over Under game with new players
  startNewGame(roomCode, requesterId) {
    const room = this.rooms.get(roomCode);
    if (!room) {
      return { success: false, error: 'Room not found' };
    }

    console.log(`👥 Starting new Over Under game with new players for room ${roomCode}`);

    // Generate new room code
    const newRoomCode = this.generateRoomCode();
    
    // Create new room for the host with Over Under state
    const newRoom = {
      hostId: requesterId,
      players: new Map(),
      assignedColors: new Set(),
      gameState: {
        phase: 'lobby',
        currentRound: 1,
        totalRounds: 0,
        roundsCompleted: 0,
        currentAnswerer: null,
        currentQuestion: null,
        currentAnswer: null,
        correctAnswer: null,
        votes: new Map(),
        scores: new Map(),
        roundResults: [],
        votingTimeLeft: 30,
        isVotingActive: false,
        answererSubmitted: false,
        usedQuestions: new Set()
      },
      created: new Date(),
      maxPlayers: 4
    };

    // Remove old room
    this.rooms.delete(roomCode);
    
    // Clear all player mappings for the old room
    for (const [playerId] of room.players) {
      this.playerToRoom.delete(playerId);
    }

    // Set up new room
    this.rooms.set(newRoomCode, newRoom);
    this.playerToRoom.set(requesterId, newRoomCode);

    console.log(`✅ New Over Under game created with room code: ${newRoomCode}`);
    return { success: true, newRoomCode: newRoomCode };
  }
}

module.exports = RoomManager;
