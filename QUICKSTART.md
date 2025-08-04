# � Crazy 8s Card Game - Quick Start Guide

## 🏗️ Complete Architecture

You now have a fully functional real-time multiplayer Crazy 8s card game with:

### ✅ Phone Client (React + Vite + Socket.IO)
- Mobile-optimized interface for players
- Real-time WebSocket connection
- Join rooms with 4-letter codes
- Play cards and draw from deck

### ✅ Backend Server (Node.js + Express + Socket.IO)  
- WebSocket server for real-time communication
- Room management with unique codes
- Complete Crazy 8s game logic
- Player management and turn tracking

### ✅ Main Screen (HTML + JavaScript + Socket.IO)
- Host interface for TV/computer display
- Shows room codes and player status
- Displays prompts and responses
- Game flow management

## 🚀 How to Run Everything

### 1. Start the Backend Server
```bash
cd backend
npm install
npm start
```
Server will start on http://localhost:3000

### 2. Start the Phone Client (Development)
```bash
cd phone-client  
npm install
npm run dev
```
Client will start on http://localhost:5173

### 3. Open Main Screen
Navigate to http://localhost:3000 in your browser (the backend serves the main screen)

## 🎮 How to Play

### Setup (Host):
1. Open http://localhost:3000 on your TV/computer
2. Click "Create Room" - you'll get a 4-letter code
3. Click "Start Game" when players have joined

### Join (Players):
1. Open http://localhost:5173 on your phone
2. Enter the 4-letter room code from the main screen  
3. Enter your name and join
4. Wait for the host to start the game

### Gameplay:
1. Host starts the game from main screen
2. Players see prompts on their phones
3. Players type and submit answers (30 seconds)
4. Main screen shows all responses
5. Game continues for 3 rounds
6. Final scores displayed

## 🔧 Technical Features

### Real-time Communication:
- WebSocket connections for instant updates
- Automatic reconnection handling
- Game state synchronization across all clients

### Game Features:
- 4-letter room codes (A-Z)
- Up to 8 players per room
- 30-second answer timer  
- 3 rounds per game
- Random prompts from question pool
- Score tracking and leaderboard

### Mobile Optimized:
- Touch-friendly interface
- Responsive design
- Connection status indicators
- Error handling and user feedback

## 🛠️ Development Notes

### File Structure:
```
jackbox-attempt/
├── backend/                 # Node.js server
│   ├── server.js           # Main server file
│   ├── utils/roomManager.js # Game logic
│   └── package.json
├── phone-client/           # React client
│   ├── src/
│   │   ├── App.jsx         # Main app
│   │   ├── JoinScreen.jsx  # Room joining
│   │   ├── GameScreen.jsx  # Gameplay
│   │   └── SocketService.js # WebSocket handling
│   └── package.json
├── main-screen/            # Host interface  
│   ├── index.html          # Main display
│   ├── main-screen.js      # Game management
│   └── style.css           # Styling
└── README.md
```

### WebSocket Events:
- `create-room` - Host creates game room
- `join-room` - Player joins with code/name
- `start-game` - Host starts the game
- `player-action` - Player submits answer
- `game-started` - Game begins notification
- `round-ended` - Round results
- `game-ended` - Final scores

### Production Deployment:
- Backend can be deployed to Heroku, Railway, or AWS
- Phone client should be built (`npm run build`) and served statically
- Main screen is served by the backend automatically
- Update Socket.IO connection URLs for production

## 🐛 Troubleshooting

### Connection Issues:
- Make sure backend is running on port 3000
- Check that phone client connects to correct backend URL
- Verify firewall/network settings for WebSocket connections

### Game Issues:
- Rooms auto-cleanup after 2 hours of inactivity
- Maximum 8 players per room
- Names must be unique within a room
- Games cannot be joined once started

## 🎯 Next Features to Add

- [ ] Voice chat integration
- [ ] Custom question sets  
- [ ] Player avatars
- [ ] Game categories (trivia, drawing, etc.)
- [ ] Tournament mode
- [ ] Replay system
- [ ] Mobile app versions

## 🔗 URLs Summary

- **Backend API**: http://localhost:3000
- **Main Screen**: http://localhost:3000  
- **Phone Client**: http://localhost:5173
- **API Docs**: http://localhost:3000/api/rooms (debug)

Ready to party! 🎉
