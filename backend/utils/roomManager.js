// Room Manager - Handles game rooms, players, and Crazy 8s game state
const Crazy8sGameLogic = require('./crazy8sGameLogic');

class RoomManager {
  constructor() {
    this.rooms = new Map(); // { roomCode: room object }
    this.playerToRoom = new Map(); // { playerId: roomCode } for quick lookup
    this.gameLogic = new Crazy8sGameLogic();
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
      players: new Map(), // { playerId: { name, id, connected: bool, cardCount: 0 } }
      gameState: {
        phase: 'lobby', // lobby, playing, game-over
        currentPlayer: 0,
        deck: [],
        discardPile: [],
        playerHands: {},
        currentSuit: null,
        chosenSuit: null, // When an 8 is played
        lastPlayedCard: null,
        turnCount: 0
      },
      created: new Date(),
      maxPlayers: 6 // Reasonable limit for Crazy 8s
    };
    
    this.rooms.set(roomCode, room);
    this.playerToRoom.set(hostId, roomCode);
    
    console.log(`Crazy 8s room created: ${roomCode} by host ${hostId}`);
    return roomCode;
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

    const player = {
      id: playerId,
      name: playerName,
      connected: true,
      cardCount: 0,
      joinedAt: new Date(),
      isFirstPlayer: room.players.size === 0 // True if this is the first player
    };

    room.players.set(playerId, player);
    this.playerToRoom.set(playerId, roomCode);

    console.log(`Player ${playerName} (${playerId}) joined Crazy 8s room ${roomCode}${player.isFirstPlayer ? ' (FIRST PLAYER)' : ''}`);
    return { success: true, player, roomData: this.getRoomData(roomCode) };
  }

  // Handle player action (play card, draw card, choose suit)
  handlePlayerAction(roomCode, playerId, action) {
    const room = this.rooms.get(roomCode);
    if (!room) return { success: false, error: 'Room not found' };

    if (room.gameState.phase !== 'playing') {
      return { success: false, error: 'Game not in progress' };
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
        return this.handlePlayCard(room, playerIndex, action.cardIndex, action.chosenSuit);
      case 'draw_card':
        return this.handleDrawCard(room, playerIndex);
      case 'choose_suit':
        return this.handleChooseSuit(room, playerIndex, action.suit);
      default:
        return { success: false, error: 'Unknown action type' };
    }
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
    room.gameState.currentSuit = startCard.suit;
    room.gameState.chosenSuit = null;
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
  handlePlayCard(room, playerIndex, cardIndex, chosenSuit = null) {
    const playerHand = room.gameState.playerHands[playerIndex];
    const card = playerHand[cardIndex];
    const topCard = room.gameState.lastPlayedCard;

    if (!card) {
      return { success: false, error: 'Invalid card index' };
    }

    // Check if card can be played
    const currentSuit = room.gameState.chosenSuit || room.gameState.currentSuit;
    if (!this.gameLogic.canPlayCard(card, topCard, currentSuit)) {
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
      if (chosenSuit && this.gameLogic.isValidSuit(chosenSuit)) {
        room.gameState.chosenSuit = chosenSuit;
        room.gameState.currentSuit = chosenSuit;
      } else {
        // Need to choose suit
        return { 
          success: true, 
          needSuitChoice: true,
          gameOver: this.gameLogic.hasWon(playerHand),
          winner: this.gameLogic.hasWon(playerHand) ? players[playerIndex].name : null
        };
      }
    } else {
      // Regular card - update current suit
      room.gameState.currentSuit = card.suit;
      room.gameState.chosenSuit = null;
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

  // Handle choosing suit after playing an 8
  handleChooseSuit(room, playerIndex, suit) {
    if (!this.gameLogic.isValidSuit(suit)) {
      return { success: false, error: 'Invalid suit choice' };
    }

    room.gameState.chosenSuit = suit;
    room.gameState.currentSuit = suit;

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
        currentSuit: room.gameState.currentSuit,
        chosenSuit: room.gameState.chosenSuit,
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
      currentSuit: room.gameState.chosenSuit || room.gameState.currentSuit,
      currentPlayer: room.gameState.currentPlayer,
      players: Array.from(room.players.values()).map(p => ({ 
        name: p.name, 
        cardCount: p.cardCount || 0 
      })),
      canDraw: room.gameState.deck?.length > 0,
      isYourTurn: playerIndex === room.gameState.currentPlayer,
      needSuitChoice: false // Will be set true when an 8 is played
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

    // Mark player as disconnected
    const player = room.players.get(playerId);
    if (player) {
      const playerName = player.name;
      room.players.delete(playerId);
      this.playerToRoom.delete(playerId);
      console.log(`Player ${playerName} disconnected from room ${roomCode}`);
      return { roomCode, playerName };
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
      currentSuit: room.gameState.chosenSuit || room.gameState.currentSuit,
      deckCount: room.gameState.deck.length,
      players: Array.from(room.players.values()).map(p => ({
        name: p.name,
        cardCount: p.cardCount
      }))
    };
  }
}

module.exports = RoomManager;
