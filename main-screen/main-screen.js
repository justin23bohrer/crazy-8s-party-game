// Main Screen JavaScript for Crazy 8s Game Host Interface

class MainScreen {
    constructor() {
        this.socket = null;
        this.roomCode = null;
        this.gameState = 'lobby'; // lobby, playing, game-over
        this.players = new Map();
        this.currentScreen = 'lobby-screen';
        this.gameData = {
            currentPlayer: null,
            topCard: null,
            currentSuit: null,
            deckCount: 0
        };
        
        try {
            this.initializeElements();
            this.connectToServer();
            this.bindEvents();
        } catch (error) {
            console.error('Error in MainScreen constructor:', error);
        }
    }

    initializeElements() {
        // Screen elements
        this.lobbyScreen = document.getElementById('lobby-screen');
        this.gameScreen = document.getElementById('game-screen');
        this.resultsScreen = document.getElementById('results-screen');
        this.gameOverScreen = document.getElementById('game-over-screen');

        // Lobby elements
        this.roomCodeDisplay = document.getElementById('room-code-display');
        this.createRoomBtn = document.getElementById('create-room-btn');
        this.startGameBtn = document.getElementById('start-game-btn');
        this.playerCount = document.getElementById('player-count');
        this.playersGrid = document.getElementById('players-grid');
        this.joinUrl = document.getElementById('join-url');

        // Game elements (Crazy 8s specific)
        this.currentPlayerDisplay = document.getElementById('current-player');
        this.currentSuitDisplay = document.getElementById('current-suit');
        this.deckCountDisplay = document.getElementById('deck-count');
        this.topCardDisplay = document.getElementById('top-card-display');
        this.gamePlayersGrid = document.getElementById('game-players-grid');
        this.gameLog = document.getElementById('game-log');

        // Game over elements
        this.finalScoresList = document.getElementById('final-scores-list');
        this.playAgainBtn = document.getElementById('play-again-btn');

        // Status elements
        this.connectionStatus = document.getElementById('connection-status');
        this.statusText = document.getElementById('status-text');

        // Set the join URL
        if (this.joinUrl) {
            this.joinUrl.textContent = `${window.location.hostname}:5173`;
        }
        
        console.log('Elements initialized');
    }

    connectToServer() {
        this.updateStatus('Connecting to server...');
        
        console.log('Attempting to connect to server...');
        this.socket = io('http://localhost:3000', {
            transports: ['websocket', 'polling'],
            timeout: 5000
        });

        this.socket.on('connect', () => {
            console.log('Connected to server:', this.socket.id);
            this.updateStatus('Connected');
            this.hideConnectionStatus();
        });

        this.socket.on('disconnect', (reason) => {
            console.log('Disconnected:', reason);
            this.updateStatus('Disconnected - ' + reason);
            this.showConnectionStatus();
        });

        this.socket.on('connect_error', (error) => {
            console.error('Connection error:', error);
            this.updateStatus('Connection failed');
            this.showConnectionStatus();
        });

        // Crazy 8s game events
        this.socket.on('room-created', (data) => this.handleRoomCreated(data));
        this.socket.on('player-joined', (data) => this.handlePlayerJoined(data));
        this.socket.on('player-left', (data) => this.handlePlayerLeft(data));
        this.socket.on('game-started', (data) => this.handleGameStarted(data));
        this.socket.on('game-state-updated', (data) => this.handleGameStateUpdated(data));
        this.socket.on('card-played', (data) => this.handleCardPlayed(data));
        this.socket.on('card-drawn', (data) => this.handleCardDrawn(data));
        this.socket.on('suit-chosen', (data) => this.handleSuitChosen(data));
        this.socket.on('game-ended', (data) => this.handleGameEnded(data));
        this.socket.on('room-closed', (data) => this.handleRoomClosed(data));
    }

    bindEvents() {
        if (this.createRoomBtn) {
            this.createRoomBtn.addEventListener('click', () => {
                this.createRoom();
            });
        } else {
            console.error('Create room button not found!');
        }
        
        if (this.startGameBtn) {
            this.startGameBtn.addEventListener('click', () => this.startGame());
        }
        
        if (this.playAgainBtn) {
            this.playAgainBtn.addEventListener('click', () => this.playAgain());
        }
    }

    // Room Management
    createRoom() {
        console.log('Create room button clicked');
        console.log('Socket connected:', this.socket?.connected);
        
        if (!this.socket || !this.socket.connected) {
            alert('Not connected to server. Please refresh the page.');
            return;
        }
        
        this.socket.emit('create-room', (response) => {
            console.log('Create room response:', response);
            if (response && response.success) {
                this.roomCode = response.roomCode;
                this.roomCodeDisplay.textContent = response.roomCode;
                this.updatePlayersList(response.roomData?.players || []);
                console.log('Room created and UI updated:', this.roomCode);
            } else {
                console.error('Failed to create room:', response?.error || 'No response');
                alert('Failed to create room: ' + (response?.error || 'No response from server'));
            }
        });
    }

    startGame() {
        if (!this.roomCode) {
            alert('No room created');
            return;
        }

        this.socket.emit('start-game', { roomCode: this.roomCode }, (response) => {
            if (!response.success) {
                console.error('Failed to start game:', response.error);
                alert('Failed to start game: ' + response.error);
            }
        });
    }

    playAgain() {
        this.switchToScreen('lobby');
        this.createRoom();
    }

    // Event Handlers
    handleRoomCreated(data) {
        this.roomCode = data.roomCode;
        
        if (this.roomCodeDisplay) {
            this.roomCodeDisplay.textContent = data.roomCode;
        } else {
            console.error('roomCodeDisplay element not found!');
        }
        
        this.updatePlayersList(data.players || []);
        console.log('Room created:', data.roomCode);
    }

    handlePlayerJoined(data) {
        console.log('Player joined:', data);
        this.updatePlayersList(data.players);
        this.addGameLogMessage(`${data.playerName} joined the game`);
    }

    handlePlayerLeft(data) {
        console.log('Player left:', data);
        this.updatePlayersList(data.players);
        this.addGameLogMessage(`${data.playerName} left the game`);
    }

    handleGameStarted(data) {
        console.log('Game started:', data);
        this.gameState = 'playing';
        this.switchToScreen('game');
        this.updateGameState(data.gameState);
        this.addGameLogMessage('Crazy 8s game started!');
    }

    handleGameStateUpdated(data) {
        console.log('Game state updated:', data);
        this.updateGameState(data.gameState);
    }

    handleCardPlayed(data) {
        console.log('Card played:', data);
        this.addGameLogMessage(`${data.playerName} played ${this.formatCard(data.card)}`);
        this.updateGameState(data.gameState);
    }

    handleCardDrawn(data) {
        console.log('Card drawn:', data);
        this.addGameLogMessage(`${data.playerName} drew a card`);
        this.updateGameState(data.gameState);
    }

    handleSuitChosen(data) {
        console.log('Suit chosen:', data);
        this.addGameLogMessage(`${data.playerName} chose ${this.getSuitSymbol(data.suit)}`);
        this.gameData.currentSuit = data.suit;
        this.updateGameDisplay();
    }

    handleGameEnded(data) {
        console.log('Game ended:', data);
        this.gameState = 'game-over';
        this.switchToScreen('game-over');
        this.addGameLogMessage(`Game Over! ${data.winner} wins!`);
        this.updateFinalScores(data.players);
    }

    handleRoomClosed(data) {
        console.log('Room closed:', data);
        alert('Room has been closed');
        this.switchToScreen('lobby');
        this.resetRoom();
    }

    // UI Updates
    updateGameState(gameState) {
        if (gameState.players) {
            this.updatePlayersList(gameState.players);
        }
        
        if (gameState.currentPlayer) {
            this.gameData.currentPlayer = gameState.currentPlayer;
        }
        
        if (gameState.topCard) {
            this.gameData.topCard = gameState.topCard;
        }
        
        if (gameState.currentSuit) {
            this.gameData.currentSuit = gameState.currentSuit;
        }
        
        if (gameState.deckCount !== undefined) {
            this.gameData.deckCount = gameState.deckCount;
        }
        
        this.updateGameDisplay();
    }

    updateGameDisplay() {
        // Update current player
        if (this.currentPlayerDisplay) {
            this.currentPlayerDisplay.textContent = this.gameData.currentPlayer || '-';
        }
        
        // Update current suit
        if (this.currentSuitDisplay) {
            this.currentSuitDisplay.innerHTML = this.gameData.currentSuit ? 
                this.getSuitSymbol(this.gameData.currentSuit) : '-';
        }
        
        // Update deck count
        if (this.deckCountDisplay) {
            this.deckCountDisplay.textContent = this.gameData.deckCount || '-';
        }
        
        // Update top card
        if (this.topCardDisplay && this.gameData.topCard) {
            const cardHtml = `<div class="card ${this.getSuitColor(this.gameData.topCard.suit)}">
                ${this.formatCard(this.gameData.topCard)}
            </div>`;
            this.topCardDisplay.innerHTML = cardHtml;
        }
    }

    updatePlayersList(players) {
        // Update lobby players list
        if (this.playersGrid) {
            if (players.length === 0) {
                this.playersGrid.innerHTML = '<div class="no-players">Waiting for players to join...</div>';
            } else {
                this.playersGrid.innerHTML = players.map(player => 
                    `<div class="player-card">
                        <div class="player-name">${player.name}</div>
                        <div class="player-status">Ready</div>
                    </div>`
                ).join('');
            }
        }

        // Update game players list
        if (this.gamePlayersGrid && this.gameState === 'playing') {
            this.gamePlayersGrid.innerHTML = players.map(player => {
                const isCurrentPlayer = player.name === this.gameData.currentPlayer;
                return `<div class="player-card ${isCurrentPlayer ? 'current-player' : ''}">
                    <div class="player-name">${player.name}</div>
                    <div class="player-cards">${player.cardCount || 0} cards</div>
                </div>`;
            }).join('');
        }

        // Update player count
        if (this.playerCount) {
            this.playerCount.textContent = `(${players.length})`;
        }

        // Enable/disable start button
        if (this.startGameBtn) {
            this.startGameBtn.disabled = players.length < 2;
        }

        // Store players
        this.players.clear();
        players.forEach(player => {
            this.players.set(player.name, player);
        });
    }

    updateFinalScores(players) {
        if (!this.finalScoresList) return;
        
        // Sort players by card count (lowest wins in Crazy 8s)
        const sortedPlayers = [...players].sort((a, b) => a.cardCount - b.cardCount);
        
        this.finalScoresList.innerHTML = sortedPlayers.map((player, index) => {
            const place = index === 0 ? '🏆' : `${index + 1}.`;
            return `<div class="score-item">
                <span class="place">${place}</span>
                <span class="name">${player.name}</span>
                <span class="score">${player.cardCount} cards</span>
            </div>`;
        }).join('');
    }

    // Helper functions
    formatCard(card) {
        if (!card) return '';
        return `${card.rank}${this.getSuitSymbol(card.suit)}`;
    }

    getSuitSymbol(suit) {
        const symbols = {
            'hearts': '♥️',
            'diamonds': '♦️',
            'clubs': '♣️',
            'spades': '♠️'
        };
        return symbols[suit] || suit;
    }

    getSuitColor(suit) {
        return (suit === 'hearts' || suit === 'diamonds') ? 'red' : 'black';
    }

    addGameLogMessage(message) {
        if (!this.gameLog) return;
        
        const messageElement = document.createElement('div');
        messageElement.className = 'log-message';
        messageElement.textContent = `${new Date().toLocaleTimeString()}: ${message}`;
        
        this.gameLog.appendChild(messageElement);
        this.gameLog.scrollTop = this.gameLog.scrollHeight;
        
        // Keep only last 50 messages
        while (this.gameLog.children.length > 50) {
            this.gameLog.removeChild(this.gameLog.firstChild);
        }
    }

    // Screen Management
    switchToScreen(screenName) {
        // Hide all screens
        document.querySelectorAll('.screen').forEach(screen => {
            screen.classList.remove('active');
        });

        // Show target screen
        let targetScreen;
        switch (screenName) {
            case 'lobby':
                targetScreen = this.lobbyScreen;
                break;
            case 'game':
                targetScreen = this.gameScreen;
                break;
            case 'results':
                targetScreen = this.resultsScreen;
                break;
            case 'game-over':
                targetScreen = this.gameOverScreen;
                break;
        }

        if (targetScreen) {
            targetScreen.classList.add('active');
            this.currentScreen = screenName;
        }
    }

    resetRoom() {
        this.roomCode = null;
        this.roomCodeDisplay.textContent = '----';
        this.players.clear();
        this.updatePlayersList([]);
        this.startGameBtn.disabled = true;
    }

    // Connection Status
    updateStatus(message) {
        if (this.statusText) {
            this.statusText.textContent = message;
        }
    }

    showConnectionStatus() {
        if (this.connectionStatus) {
            this.connectionStatus.style.display = 'block';
        }
    }

    hideConnectionStatus() {
        if (this.connectionStatus) {
            this.connectionStatus.style.display = 'none';
        }
    }
}

// Initialize the main screen when the page loads
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM Content Loaded - initializing main screen');
    try {
        window.mainScreen = new MainScreen();
        console.log('Main screen initialized successfully');
    } catch (error) {
        console.error('Error initializing main screen:', error);
    }
});
