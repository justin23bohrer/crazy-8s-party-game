# � Over Under - Trivia Guessing Party Game

A Jackbox-style multiplayer trivia game where players guess numeric answers and vote whether the actual answer is "over" or "under" their guess. Features Unity-powered main screen, React phone clients, and Node.js backend with WebSocket communication.

## � Game Overview

**Over Under** is a party game for 2-4 players where:
- Players join using their phones with a 4-letter room code
- Each player gets one turn as the "answerer" per game
- The answerer guesses a numeric answer to a trivia question
- Other players vote whether the real answer is "over" or "under" the guess
- Players earn points for correct votes (150 points each)
- The player with the most points wins!

## 🏗️ Architecture

The game consists of three components:

### 1. **Unity Host Screen** (Display/TV)
- Shows game status, questions, and results
- Displays scoreboard and winner animations
- **Status: ⚠️ NEEDS IMPLEMENTATION** (You handle this part)

### 2. **Phone Clients** (React + Vite)
- Players use phones to join and play
- Shows questions, answer input, voting buttons
- Displays scores and results
- **Status: ✅ COMPLETE**

### 3. **Backend Server** (Node.js + Socket.IO)
- Manages rooms, players, and game state
- Handles trivia questions and scoring
- Coordinates communication between Unity and phones
- **Status: ✅ COMPLETE**

## 🎯 Game Flow

1. **Lobby Phase**: Players join with room code
2. **Game Start**: Host/first player starts the game
3. **Round Loop** (one round per player):
   - Random player selected as answerer
   - Question displayed to all players
   - Answerer submits numeric guess
   - Other players vote "Over" or "Under" (30 seconds)
   - Results shown with correct answer and scores
4. **Game End**: Final scoreboard and winner announced

## � Setup Instructions

### Prerequisites
- Node.js (v14 or higher)
- NPM or Yarn

### Backend Setup
```bash
cd backend
npm install
npm start
# Server runs on http://localhost:3000
```

### Phone Client Setup
```bash
cd phone-client
npm install
npm run dev
# Client runs on http://localhost:5173
```

### Testing the Game
1. Start backend server
2. Start phone client
3. Open multiple browser tabs to `http://localhost:5173`
4. Join the same room code from different tabs
5. First player can start the game

## 📁 File Structure

```
over-under/
├── backend/
│   ├── server.js              # Main server with Socket.IO events
│   ├── package.json          # Backend dependencies
│   └── utils/
│       ├── overUnderGameLogic.js  # Game rules and scoring
│       └── roomManager.js     # Room and player management
├── phone-client/
│   ├── src/
│   │   ├── GameScreen.jsx    # Main game UI for phones
│   │   ├── SocketService.js  # Socket.IO client wrapper
│   │   └── ...
│   ├── package.json          # Frontend dependencies
│   └── vite.config.js        # Vite configuration
├── OverUnderMainScreen/      # Unity project (your part)
└── README.md                 # This file
```

## � Socket Events (For Unity Integration)

### 🎨 **Visual Excellence**
- **Modern UI**: Clean, polished interface with glassmorphism effects
- **Smooth Animations**: Card movements, player transitions, color changes
- **Dynamic Backgrounds**: Animated clouds and particle effects
- **Responsive Layout**: Adapts to different screen sizes and resolutions

### 🎮 **Game Management**
- **Room Creation**: Generate unique 4-letter room codes
- **Player Lobby**: Real-time player list with join notifications
- **Game State Display**: Current player turn, deck count, active color
- **Win Celebrations**: Animated winner announcements

### 🔧 **Technical Implementation**
- **Socket.IO Unity**: Real-time WebSocket communication
- **Modular Architecture**: Separate managers for different systems
- **Component-Based**: Clean separation of concerns
- **Performance Optimized**: 60 FPS target with background processing

### 📊 **Manager System Overview**

| Manager | Purpose |
|---------|---------|
| `GameManager` | Core game coordination and initialization |
| `NetworkManager` | Socket.IO communication with backend |
| `UIManager` | Visual updates and color transitions |  
| `GameStateManager` | Game state tracking and validation |
| `PlayerManager` | Player list management and display |
| `CardAnimationManager` | Card movement and visual effects |

## 🚀 Features

### Events Unity Should Listen For:
- `room-created` - Room was created, contains room code
- `player-joined` - New player joined, update player list
- `game-started` - Game began, show start screen
- `show-question` - Display question and current answerer
- `voting-phase` - Show player's guess, start voting countdown
- `round-results` - Display results with correct answer and winners
- `update-scoreboard` - Update score display
- `game-over` - Show final results and winner

### Events Unity Should Emit:
- `create-room` - Create new game room
- `host-join-room` - Join room as host to receive updates
- `start-game` - Start the game (or let first player do this)

### Example Unity Socket Usage:
```csharp
// Listen for room creation
socket.On("room-created", (data) => {
    string roomCode = data["roomCode"].str;
    DisplayRoomCode(roomCode);
});

// Listen for questions
socket.On("show-question", (data) => {
    string question = data["question"].str;
    string answerer = data["answerer"].str;
    DisplayQuestion(question, answerer);
});

// Create room
socket.Emit("create-room");
```

## 🎲 Sample Questions

The game includes 30 trivia questions like:
- "How many U.S. presidents have there been?" (Answer: 46)
- "What year was the iPhone first released?" (Answer: 2007)  
- "How many bones are in the human body?" (Answer: 206)

Questions are randomly selected and not repeated during a game.

## � What You Need to Build (Unity)

1. **Room Display**: Show 4-letter room code for players to join
2. **Player List**: Display joined players with their colors
3. **Question Display**: Show current trivia question clearly
4. **Answerer Highlight**: Indicate who is currently answering
5. **Voting Countdown**: 30-second timer for voting phase
6. **Results Screen**: Show player's guess, correct answer, and winners
7. **Scoreboard**: Display current scores with player colors
8. **Winner Animation**: Celebrate the final winner
9. **Host Controls**: Buttons for starting new games or getting new players

## 🐛 Debugging

### Backend Logs
The server logs all major events. Look for:
```
🎯 Over Under Game Server running on port 3000
Player [name] joined Over Under room [code]
Round X started - [player] is the answerer
```

### Phone Client Logs  
Open browser developer tools and check console for:
```
📡 PHONE: Received event 'show-question'
🔧 PHONE: Over Under event listeners set up
```

## 🤝 Testing Checklist

- [ ] Multiple phones can join the same room
- [ ] Questions appear for all players
- [ ] Only answerer can submit numeric answers
- [ ] Only non-answerers can vote Over/Under
- [ ] Voting timer counts down correctly
- [ ] Scores update after each round
- [ ] Game ends after all players answer once
- [ ] Host controls work for restarting/new players

---

Ready to test! Start the backend and phone client, then build your Unity display to complete the experience. 🎮