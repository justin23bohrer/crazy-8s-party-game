<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Crazy 8s Phone Client Project

This is a React-based phone client for Crazy 8s card game built with Vite. The project provides a mobile-friendly interface that allows players to join games and play cards using their phones.

## Key Components

- **JoinScreen**: Handles room code entry and player name input
- **GameScreen**: Main card game interface with hand management and card playing
- **App**: Root component managing navigation between join and game screens

## Design Guidelines

- Maintain mobile-first responsive design
- Use glassmorphism UI elements with backdrop filters
- Follow the established color scheme (purple gradient background)
- Ensure accessibility with proper focus states and keyboard navigation
- Keep the interface simple and intuitive for party game scenarios

## Code Patterns

- Use React hooks (useState, useEffect) for state management
- Implement proper form validation and user feedback
- Include loading states and error handling
- Add smooth animations and transitions for better UX
- Consider touch-friendly button sizes and spacing

## Future Enhancements

- WebSocket connection for real-time game communication
- Sound effects and haptic feedback
- Game-specific UI variations
- Player avatar/profile features
