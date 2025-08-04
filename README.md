# Crazy 8s Card Game

A complete real-time multiplayer Crazy 8s card game with phone clients, main screen display, and WebSocket backend.

## 🏗️ Architecture

```
┌─────────────────┐    WebSocket    ┌─────────────────┐
│   Phone Client  │ ←──────────────→ │     Backend     │
│   (Players)     │                 │   (Node.js +    │
└─────────────────┘                 │   Socket.IO)    │
                                    │                 │
┌─────────────────┐    WebSocket    │                 │
│  Main Screen    │ ←──────────────→ │                 │
│   (Host TV)     │                 └─────────────────┘
└─────────────────┘
```

## 📁 Project Structure

```
crazy-8s-game/
├── phone-client/           # React phone client (players)
│   ├── src/
│   │   ├── App.jsx
│   │   ├── JoinScreen.jsx
│   │   ├── GameScreen.jsx
│   │   ├── SocketService.js
│   │   └── index.css
│   └── package.json
├── backend/               # Node.js WebSocket server
│   ├── server.js
│   ├── utils/
│   │   ├── roomManager.js
│   │   └── crazy8sGameLogic.js
│   └── package.json
├── main-screen/          # Host display client
│   ├── index.html
│   ├── main-screen.js
│   └── style.css
└── README.md
```

## 🚀 Features

### Phone Client (✅ Complete)
- Room code entry (4-letter codes)
- Player name input
- Real-time card game interaction
- Card playing and drawing
- Mobile-optimized UI

### Backend (🔨 To Build)
- Room creation with unique codes
- WebSocket communication
- Player management
- Game state synchronization
- Real-time updates

### Main Screen (🔨 To Build)
- Game host interface
- Live player display
- Game state visualization
- Round management
- Score tracking

## 🛠️ Tech Stack

- **Frontend**: React + Vite (phone client)
- **Backend**: Node.js + Express + Socket.IO
- **Communication**: WebSockets for real-time sync
- **Styling**: Modern CSS with glassmorphism effects

## 📡 WebSocket Events

### Client → Server
- `create-room`: Host creates new game room
- `join-room`: Player joins with room code
- `player-action`: Player submits answer/action
- `start-game`: Host starts the game
- `disconnect`: Handle player leaving

### Server → Client
- `room-created`: Returns room code to host
- `player-joined`: Notify all when player joins
- `game-state-update`: Sync game state
- `round-start`: New round begins
- `round-end`: Round results
- `error`: Error messages

## 🎮 Game Flow

1. **Setup**: Host opens main screen, creates room
2. **Join**: Players scan QR code or enter room code
3. **Lobby**: Players see waiting room, host sees player list
4. **Game**: Rounds of prompts and responses
5. **Results**: Scoring and winner announcement

## 🚀 Getting Started

### **Option 1: Two Terminal Windows (Recommended)**

**Terminal 1 - Backend Server:**
```bash
cd backend
npm install
npm start
```
Server runs on: http://localhost:3000

**Terminal 2 - Phone Client:**
```bash
cd phone-client
npm install
npm run dev
```
Client runs on: http://localhost:5173

**Browser - Main Screen:**
Open http://localhost:3000 (served by backend)

### **Option 2: PowerShell with Background Processes**
```powershell
# Start backend in background
cd backend
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm start"

# Start phone client in background  
cd ../phone-client
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev"

# Open main screen
start http://localhost:3000
```

### **Option 3: Single Terminal with Concurrently**
```bash
# Install concurrently globally (one time)
npm install -g concurrently

# From project root, run both simultaneously
concurrently "cd backend && npm start" "cd phone-client && npm run dev"
```

## 🎮 **How to Play (Step by Step)**

1. **Start Both Servers** (using any option above)
2. **Host Setup:**
   - Open http://localhost:3000 on computer/TV
   - Click "Create Room" → get 4-letter code
3. **Players Join:**
   - Open http://localhost:5173 on phones
   - Enter room code + name → join game
4. **Start Game:**
   - Host clicks "Start Game" when ready
   - Players answer prompts in real-time!

## 🔗 **URL Summary**
- **Main Screen (Host)**: http://localhost:3000
- **Phone Client (Players)**: http://localhost:5173
- **Backend API**: http://localhost:3000/api/rooms

## ✅ **Status Update**

1. ✅ Phone client complete (React + Socket.IO)
2. ✅ Backend complete (Node.js + Express + Socket.IO)  
3. ✅ Main screen complete (HTML + CSS + JavaScript)
4. ✅ Real-time WebSocket communication
5. ✅ Game logic and scoring implemented
6. 🔨 Ready for production deployment

## 📝 Development Notes

- Rooms stored in memory (Map structure)
- 4-letter room codes (A-Z only)
- Supports multiple concurrent games
- Mobile-first responsive design
- Real-time state synchronization