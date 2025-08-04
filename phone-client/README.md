# Crazy 8s Phone Client

A mobile-friendly React phone client for Crazy 8s card game built with Vite. This project provides an intuitive interface that allows players to join games and play cards using their phones.

## Features

- **Join Screen**: Clean interface for entering room codes and player names
- **Game Interface**: Interactive card game screen with hand management and card playing
- **Mobile-First Design**: Optimized for phone usage with touch-friendly controls
- **Glassmorphism UI**: Modern design with backdrop blur effects
- **Responsive Layout**: Works across different screen sizes
- **Real-time Feedback**: Visual indicators for game state and user actions

## Project Structure

```
PHONE-CLIENT/
├── public/
│   └── index.html        # Entry HTML for Vite
├── src/
│   ├── App.jsx           # Root component with join/game logic
│   ├── GameScreen.jsx    # Game UI after joining
│   ├── JoinScreen.jsx    # Room code + name input screen
│   ├── index.css         # Custom styling
│   └── main.jsx          # React/Vite entry point
├── package.json          # Dependencies and scripts
└── vite.config.js        # Vite configuration
```

## Getting Started

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Start the development server:**
   ```bash
   npm run dev
   ```

3. **Build for production:**
   ```bash
   npm run build
   ```

4. **Preview production build:**
   ```bash
   npm run preview
   ```

## Usage

1. Open the app on your phone's browser
2. Enter the 4-letter room code displayed on your TV screen
3. Enter your player name
4. Join the game and start playing!

## Development

The project uses:
- **React 18** for the UI framework
- **Vite** for fast development and building
- **CSS3** with custom properties and modern features
- **Mobile-first responsive design**

## Future Enhancements

- WebSocket integration for real-time game communication
- Sound effects and haptic feedback
- Multiple game type support
- Player avatars and profiles
- Game history and statistics
