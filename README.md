# 🎮 Crazy 8s Multiplayer Card Game

A complete real-time multiplayer Crazy 8s card game featuring Unity-powered main screen, React phone clients, and Node.js backend with WebSocket communication.

## 🏗️ Three-Tier Architecture

```
┌─────────────────┐    WebSocket    ┌─────────────────┐    WebSocket    ┌─────────────────┐
│   Phone Client  │ ←─────────────→ │     Backend     │ ←─────────────→ │   Unity Client  │
│   (React Web)   │                 │   (Node.js +    │                 │ (Main Screen/TV)│
│   Players Join  │                 │   Socket.IO +   │                 │  Game Display   │
│   & Play Cards  │                 │  Game Logic)    │                 │  & Host Control │
└─────────────────┘                 └─────────────────┘                 └─────────────────┘
```

## 🎯 Game Components

### 📱 **Phone Client** (React + Vite + Socket.IO)
- **Purpose**: Player interface for joining and playing
- **Technology**: React, Vite, Socket.IO-client
- **Features**: Mobile-optimized card game interface
- **Players**: Each player uses their phone to play

### 🖥️ **Unity Main Screen** (Unity 2022.3+ LTS)
- **Purpose**: Host display for TV/computer screen
- **Technology**: Unity Engine with Socket.IO Unity package
- **Features**: Beautiful game visualization, real-time updates
- **Host**: Single screen showing game state to all players

### ⚡ **Backend Server** (Node.js + Express + Socket.IO)
- **Purpose**: Game logic and real-time communication hub
- **Technology**: Node.js, Express, Socket.IO
- **Features**: Room management, game state, card logic

## 📁 Detailed Project Structure

```
jackbox-attempt/
├── 📱 phone-client/              # React phone client (players)
│   ├── src/
│   │   ├── App.jsx              # Main app component
│   │   ├── JoinScreen.jsx       # Room joining interface  
│   │   ├── GameScreen.jsx       # Card playing interface
│   │   ├── SocketService.js     # WebSocket communication
│   │   └── index.css           # Mobile-optimized styling
│   ├── package.json
│   └── vite.config.js          # Vite build configuration
│
├── 🖥️ Crazy8sMainScreen/         # Unity main screen (host)
│   ├── Assets/
│   │   ├── 🎮 Core Game Scripts
│   │   │   ├── GameManager.cs        # Main game controller
│   │   │   ├── NetworkManager.cs     # Socket.IO communication
│   │   │   ├── GameStateManager.cs   # Game state tracking
│   │   │   └── UIManager.cs          # UI updates & effects
│   │   ├── 🃏 Card System
│   │   │   ├── CardController.cs     # Individual card behavior
│   │   │   ├── CardDisplay.cs        # Card visual display
│   │   │   └── CardAnimationManager.cs # Card animations
│   │   ├── 👥 Player Management
│   │   │   ├── PlayerManager.cs      # Player tracking
│   │   │   ├── LobbyPlayerManager.cs # Lobby player display
│   │   │   └── PlayerPositionManager.cs # Player positioning
│   │   ├── 🎨 Visual Effects
│   │   │   ├── CloudMover.cs        # Background animations
│   │   │   ├── FunDotManager.cs     # Particle effects
│   │   │   └── SpiralAnimationController.cs # Win animations
│   │   ├── Scenes/
│   │   │   └── SampleScene.unity    # Main game scene
│   │   └── Prefabs/                # Reusable game objects
│   ├── Crazy8sMainScreen.sln       # Visual Studio solution
│   └── Assembly-CSharp.csproj      # C# project file
│
├── ⚡ backend/                    # Node.js server
│   ├── server.js                  # Express + Socket.IO server
│   ├── utils/
│   │   ├── roomManager.js         # Room creation & management
│   │   └── crazy8sGameLogic.js    # Complete Crazy 8s rules
│   └── package.json
│
├── 📖 Documentation
│   ├── README.md                  # This file
│   ├── QUICKSTART.md             # Quick setup guide
```

## 🚀 Unity Main Screen Features

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

### 📱 Phone Client (✅ Complete)
- **Room Joining**: 4-letter code entry with validation
- **Player Names**: Custom name input with character limits
- **Card Interface**: Visual card hand with play/draw actions
- **Real-time Updates**: Instant game state synchronization
- **Mobile Optimized**: Touch-friendly responsive design

### 🖥️ Unity Main Screen (✅ Complete)  
- **Host Dashboard**: Room creation and game management
- **Live Player Display**: Real-time lobby and game participants
- **Game Visualization**: Current state, turn indicator, deck count
- **Visual Effects**: Background animations and color transitions
- **Winner Celebrations**: Animated victory sequences

### ⚡ Backend Server (✅ Complete)
- **Room Management**: Unique code generation and validation
- **WebSocket Hub**: Real-time bidirectional communication  
- **Game Logic**: Complete Crazy 8s rules implementation
- **Player Tracking**: Join/leave handling and state management
- **Error Handling**: Graceful disconnection and reconnection

## 🛠️ Tech Stack

- **Unity Main Screen**: Unity 2022.3+ LTS, C#, Socket.IO Unity
- **Phone Client**: React 18, Vite 4, Socket.IO-client
- **Backend**: Node.js, Express, Socket.IO
- **Communication**: WebSockets for real-time synchronization
- **Styling**: Modern CSS with glassmorphism effects

## 📡 WebSocket Events

### Unity Client ↔ Server
```csharp
// Unity sends
socket.Emit("create-room");
socket.Emit("start-game", roomCode);

// Unity receives  
socket.On("room-created", data => HandleRoomCreated(data));
socket.On("game-state-update", data => HandleGameStateUpdate(data));
socket.On("player-joined", data => HandlePlayerJoined(data));
socket.On("game-over", data => HandleGameOver(data));
```

### Phone Client ↔ Server
```javascript
// Phone sends
socket.emit('join-room', { roomCode, playerName });
socket.emit('play-card', { cardId, chosenColor });
socket.emit('draw-card');

// Phone receives
socket.on('room-joined', data => updateGameState(data));
socket.on('card-played', data => handleCardPlayed(data));
socket.on('game-over', data => showWinner(data));
```

## 🎮 Game Flow

1. **🏠 Setup**: Open Unity main screen, click "Create Room"
2. **📱 Join**: Players visit phone client, enter room code + name
3. **⏳ Lobby**: Unity shows player list, host clicks "Start Game"
4. **🎴 Gameplay**: Players play cards from phones, Unity shows updates
5. **🏆 Victory**: Unity displays winner with celebration animation

## 🚀 Getting Started

### **Prerequisites**
- **Unity**: 2022.3 LTS or later
- **Node.js**: v16 or later  
- **Modern Browser**: For phone clients

### **Quick Start (3 Steps)**

**Step 1: Start Backend Server**
```powershell
cd backend
npm install
npm start
```
✅ Server running on http://localhost:3000

**Step 2: Launch Unity Main Screen**  
```powershell
# Open Unity Hub → Open Project
# Navigate to: Crazy8sMainScreen/
# Press Play button in Unity Editor
```
✅ Unity client connected to backend

**Step 3: Start Phone Client**
```powershell  
cd phone-client
npm install  
npm run dev
```
✅ Phone client on http://localhost:5173

### **🎮 How to Play (Step by Step)**

1. **Unity Setup:**
   - Unity editor running with scene loaded
   - Click "Create Room" → displays 4-letter code
   
2. **Players Join:**
   - Open http://localhost:5173 on phones
   - Enter room code + player name
   - Unity shows players joining in real-time
   
3. **Start Game:**
   - Unity host clicks "Start Game"
   - Unity displays first player and game state
   - Players see their cards on phones
   
4. **Play Game:**
   - Players tap cards on phones to play
   - Unity shows card animations and updates
   - Game continues until someone wins
   
5. **Victory:**
   - Unity displays winner with celebration
   - Option to start new game

## 🔗 **Development URLs**
- **Unity Main Screen**: Runs in Unity Editor (localhost backend connection)
- **Phone Client**: http://localhost:5173  
- **Backend API**: http://localhost:3000
- **Socket.IO Connection**: ws://localhost:3000/socket.io/

## 🏗️ **Unity Development Setup**

### **Required Unity Packages**
```json
{
  "com.unity.textmeshpro": "3.0.6",
  "com.unity.inputsystem": "1.5.1", 
  "com.unity.render-pipelines.universal": "14.0.8"
}
```

### **Socket.IO Unity Package**
Install via Package Manager:
```
https://github.com/itisnajim/SocketIOUnity.git
```

### **Build Settings**
- **Platform**: PC, Mac & Linux Standalone
- **Target**: Windows x64 (or your platform)
- **Optimization**: Release mode for production builds

## ✅ **Project Status**

| Component | Status | Features |
|-----------|--------|----------|
| 📱 **Phone Client** | ✅ Complete | Join rooms, play cards, real-time sync |
| 🖥️ **Unity Main Screen** | ✅ Complete | Room creation, game display, animations |
| ⚡ **Backend Server** | ✅ Complete | WebSocket hub, game logic, room management |
| 🌐 **Real-time Sync** | ✅ Working | Bidirectional WebSocket communication |
| 🎮 **Game Logic** | ✅ Complete | Full Crazy 8s rules implementation |
| 🎨 **Visual Polish** | ✅ Complete | Animations, effects, responsive design |

**Ready for production deployment and multiplayer gaming!**

## 📝 Development Notes

### **Unity Architecture**
- **Singleton Pattern**: GameManager provides global access
- **Event-Driven**: Managers communicate via C# events
- **Component-Based**: Each system is a separate MonoBehaviour
- **Thread-Safe**: Socket events queued for main thread execution

### **Performance Considerations**
- **60 FPS Target**: Optimized rendering and animations
- **Background Processing**: Continues running when window loses focus
- **Memory Management**: Proper object cleanup and pooling
- **Network Efficiency**: Minimal data transfer with JSON serialization

### **Deployment Options**
- **Standalone Builds**: Windows/Mac/Linux executables
- **WebGL Build**: Browser-based Unity client (alternative)
- **Mobile Export**: Android/iOS builds possible with UI adjustments
