// Room Manager - Handles game rooms, players, and Crazy 8s game state
const Crazy8sGameLogic = require('./crazy8sGameLogic');

class RoomManager {
  constructor() {
    this.rooms = new Map(); // { roomCode: room object }
    this.playerToRoom = new Map(); // { playerId: roomCode } for quick lookup
    this.gameLogic = new Crazy8sGameLogic();
    
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
      players: new Map(), // { playerId: { name, id, connected: bool, cardCount: 0, color: string } }
      assignedColors: new Set(), // Track which colors are taken
      gameState: {
        phase: 'lobby', // lobby, playing, game-over
        currentPlayer: 0,
        deck: [],
        discardPile: [],
        playerHands: {},
        currentColor: null, // Changed from currentSuit to currentColor
        chosenColor: null, // Changed from chosenSuit to chosenColor - When an 8 is played
        lastPlayedCard: null,
        turnCount: 0,
        isAnimating: false, // Track if spiral animation is playing - blocks all player actions
        animationEndTime: null // When animation will end
      },
      created: new Date(),
      maxPlayers: 4 // Max 4 players for color assignment (red, blue, green, yellow)
    };
    
    this.rooms.set(roomCode, room);
    this.playerToRoom.set(hostId, roomCode);
    
    console.log(`Crazy 8s room created: ${roomCode} by host ${hostId}`);
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
      cardCount: 0,
      color: playerColor,
      joinedAt: new Date(),
      isFirstPlayer: room.players.size === 0 // True if this is the first player
    };

    room.players.set(playerId, player);
    this.playerToRoom.set(playerId, roomCode);

    console.log(`Player ${playerName} (${playerId}) joined Crazy 8s room ${roomCode} with color ${playerColor}${player.isFirstPlayer ? ' (FIRST PLAYER)' : ''}`);
    return { success: true, player, roomData: this.getRoomData(roomCode) };
  }

  // Handle player action (play card, draw card, choose suit)
  handlePlayerAction(roomCode, playerId, action) {
    const room = this.rooms.get(roomCode);
    if (!room) return { success: false, error: 'Room not found' };

    if (room.gameState.phase !== 'playing') {
      return { success: false, error: 'Game not in progress' };
    }

    // Check if spiral animation is blocking actions
    if (this.isAnimationBlocking(room)) {
      return { success: false, error: 'Please wait for the animation to complete' };
    }

    const playerIndex = this.getPlayerIndex(room, playerId);
    if (playerIndex === -1) {
      return { success: false, error: 'Player not found' };
    }

    if (playerIndex !== room.gameState.currentPlayer) {
      return { success: false, error: 'Not your turn' };
    }

    switch (action.type) {
      case 'play_card':
        return this.handlePlayCard(room, playerIndex, action.cardIndex, action.chosenColor); // Changed from chosenSuit to chosenColor
      case 'draw_card':
        return this.handleDrawCard(room, playerIndex);
      case 'choose_color': // Changed from choose_suit to choose_color
        return this.handleChooseColor(room, playerIndex, action.color); // Changed from action.suit to action.color
      default:
        return { success: false, error: 'Unknown action type' };
    }
  }

  // Check if spiral animation is currently blocking player actions
  isAnimationBlocking(room) {
    if (!room.gameState.isAnimating) {
      return false;
    }

    // Check if animation should have ended by now
    if (room.gameState.animationEndTime && Date.now() > room.gameState.animationEndTime) {
      // Animation time expired, clear the lock
      room.gameState.isAnimating = false;
      room.gameState.animationEndTime = null;
      console.log('🎬 Animation lock expired and cleared');
      return false;
    }

    // Animation is still active
    return true;
  }

  // Start spiral animation lock (called when 8 card color is chosen)
  startAnimationLock(roomCode, durationMs = 3300) { // 3.3 seconds for spiral animation
    const room = this.rooms.get(roomCode);
    if (!room) return;

    room.gameState.isAnimating = true;
    room.gameState.animationEndTime = Date.now() + durationMs;
    console.log(`🎬 Started animation lock for ${durationMs}ms in room ${roomCode}`);
  }

  // Clear animation lock (can be called manually if needed)
  clearAnimationLock(roomCode) {
    const room = this.rooms.get(roomCode);
    if (!room) return;

    room.gameState.isAnimating = false;
    room.gameState.animationEndTime = null;
    console.log(`🎬 Cleared animation lock in room ${roomCode}`);
  }

  // Start Crazy 8s game
  startGame(roomCode, requesterId) {
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

    // Initialize Crazy 8s game
    const deck = this.gameLogic.createDeck();
    const playerCount = room.players.size;
    const { hands, remainingDeck } = this.gameLogic.dealInitialHands(deck, playerCount);
    
    // Set up starting card (not an 8)
    const startCard = this.gameLogic.findValidStartCard(remainingDeck);
    
    room.gameState.phase = 'playing';
    room.gameState.currentPlayer = 0;
    room.gameState.deck = remainingDeck;
    room.gameState.discardPile = [startCard];
    room.gameState.playerHands = hands;
    room.gameState.currentColor = startCard.color; // Changed from currentSuit to currentColor
    room.gameState.chosenColor = null; // Changed from chosenSuit to chosenColor
    room.gameState.lastPlayedCard = startCard;
    room.gameState.turnCount = 0;

    // Update player card counts for UI
    let playerIndex = 0;
    for (const [playerId, player] of room.players) {
      player.cardCount = hands[playerIndex].length;
      playerIndex++;
    }

    console.log(`Crazy 8s game started in room ${roomCode} with ${playerCount} players`);
    return { success: true, gameState: room.gameState };
  }

  // Helper to get player index from ID
  getPlayerIndex(room, playerId) {
    let index = 0;
    for (const [id] of room.players) {
      if (id === playerId) return index;
      index++;
    }
    return -1;
  }

  // Handle playing a card
  handlePlayCard(room, playerIndex, cardIndex, chosenColor = null) { // Changed from chosenSuit to chosenColor
    const playerHand = room.gameState.playerHands[playerIndex];
    const card = playerHand[cardIndex];
    const topCard = room.gameState.lastPlayedCard;

    if (!card) {
      return { success: false, error: 'Invalid card index' };
    }

    // Check if card can be played
    const currentColor = room.gameState.chosenColor || room.gameState.currentColor; // Changed from currentSuit to currentColor
    if (!this.gameLogic.canPlayCard(card, topCard, currentColor)) {
      return { success: false, error: 'Cannot play this card' };
    }

    // Remove card from player's hand
    playerHand.splice(cardIndex, 1);
    room.gameState.discardPile.push(card);
    room.gameState.lastPlayedCard = card;
    room.gameState.turnCount++;

    // Update player card count
    const players = Array.from(room.players.values());
    players[playerIndex].cardCount = playerHand.length;

    // Handle 8s (wild cards)
    if (card.rank === '8') {
      if (chosenColor && this.gameLogic.isValidColor(chosenColor)) { // Changed from chosenSuit to chosenColor and isValidSuit to isValidColor
        room.gameState.chosenColor = chosenColor; // Changed from chosenSuit to chosenColor
        room.gameState.currentColor = chosenColor; // Changed from currentSuit to currentColor
      } else {
        // Need to choose color
        return { 
          success: true, 
          needColorChoice: true, // Changed from needSuitChoice to needColorChoice
          gameOver: this.gameLogic.hasWon(playerHand),
          winner: this.gameLogic.hasWon(playerHand) ? players[playerIndex].name : null
        };
      }
    } else {
      // Regular card - update current color
      room.gameState.currentColor = card.color; // Changed from suit to color and currentSuit to currentColor
      room.gameState.chosenColor = null; // Changed from chosenSuit to chosenColor
    }

    // Check for win
    if (this.gameLogic.hasWon(playerHand)) {
      room.gameState.phase = 'game-over';
      return { 
        success: true, 
        gameOver: true, 
        winner: players[playerIndex].name 
      };
    }

    // Move to next player
    room.gameState.currentPlayer = this.gameLogic.getNextPlayer(
      room.gameState.currentPlayer, 
      room.players.size
    );

    return { success: true };
  }

  // Handle drawing a card
  handleDrawCard(room, playerIndex) {
    if (room.gameState.deck.length === 0) {
      // Reshuffle discard pile into deck (keep top card)
      const topCard = room.gameState.discardPile.pop();
      room.gameState.deck = this.gameLogic.shuffleDeck(room.gameState.discardPile);
      room.gameState.discardPile = [topCard];
    }

    if (room.gameState.deck.length === 0) {
      return { success: false, error: 'No cards left to draw' };
    }

    const drawnCard = room.gameState.deck.pop();
    room.gameState.playerHands[playerIndex].push(drawnCard);
    
    const players = Array.from(room.players.values());
    players[playerIndex].cardCount++;

    // Move to next player after drawing
    room.gameState.currentPlayer = this.gameLogic.getNextPlayer(
      room.gameState.currentPlayer, 
      room.players.size
    );

    return { success: true, drawnCard };
  }

  // Handle choosing color after playing an 8
  handleChooseColor(room, playerIndex, color) { // Changed from handleChooseSuit to handleChooseColor and suit to color
    if (!this.gameLogic.isValidColor(color)) { // Changed from isValidSuit to isValidColor
      return { success: false, error: 'Invalid color choice' }; // Changed error message
    }

    room.gameState.chosenColor = color; // Changed from chosenSuit to chosenColor
    room.gameState.currentColor = color; // Changed from currentSuit to currentColor

    // NOTE: Animation lock is now handled in server.js before sending updates
    // This ensures the lock is set before any game state updates reach players

    // Move to next player
    room.gameState.currentPlayer = this.gameLogic.getNextPlayer(
      room.gameState.currentPlayer, 
      room.players.size
    );

    return { success: true };
  }

  // Get room data for clients (for Crazy 8s)
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
        cardCount: p.cardCount || 0
      })),
      gameState: {
        phase: room.gameState.phase,
        currentPlayer: room.gameState.currentPlayer,
        currentColor: room.gameState.currentColor, // Changed from currentSuit to currentColor
        chosenColor: room.gameState.chosenColor, // Changed from chosenSuit to chosenColor
        topCard: room.gameState.lastPlayedCard,
        deckCount: room.gameState.deck?.length || 0
      }
    };
  }

  // Get game state for a specific player
  getGameStateForPlayer(roomCode, playerId) {
    const room = this.rooms.get(roomCode);
    if (!room) return null;

    const playerIndex = this.getPlayerIndex(room, playerId);
    if (playerIndex === -1) return null;

    return {
      playerHand: room.gameState.playerHands[playerIndex] || [],
      topCard: room.gameState.lastPlayedCard,
      currentColor: room.gameState.chosenColor || room.gameState.currentColor, // Changed from currentSuit to currentColor
      currentPlayer: room.gameState.currentPlayer,
      players: Array.from(room.players.values()).map(p => ({ 
        name: p.name, 
        cardCount: p.cardCount || 0 
      })),
      canDraw: room.gameState.deck?.length > 0,
      isYourTurn: playerIndex === room.gameState.currentPlayer,
      needSuitChoice: false, // Will be set true when an 8 is played
      isAnimating: room.gameState.isAnimating || false // Include animation state for UI blocking
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

  // Get game state for main screen display
  getMainScreenGameState(room) {
    return {
      currentPlayer: Array.from(room.players.values())[room.gameState.currentPlayer]?.name,
      topCard: room.gameState.lastPlayedCard,
      currentColor: room.gameState.chosenColor || room.gameState.currentColor, // Changed from currentSuit to currentColor
      deckCount: room.gameState.deck.length,
      players: Array.from(room.players.values()).map(p => ({
        name: p.name,
        cardCount: p.cardCount,
        color: p.color
      })),
      isAnimating: room.gameState.isAnimating || false // Include animation state
    };
  }
}

module.exports = RoomManager;
