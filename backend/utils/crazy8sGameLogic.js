// Crazy 8s Game Logic - Handles deck, cards, and game rules
class Crazy8sGameLogic {
  constructor() {
    // Using colors instead of traditional suits
    this.colors = ['red', 'blue', 'green', 'yellow'];
    this.ranks = ['1', '2', '3', '4', '5', '6', '7', '8', '9']; // Simplified rank system
    this.colorEmojis = {
      red: '🔴',
      blue: '🔵', 
      green: '🟢',
      yellow: '🟡'
    };
  }

  // Create a 36-card deck (4 colors × 9 ranks)
  createDeck() {
    const deck = [];
    
    this.colors.forEach(color => {
      this.ranks.forEach(rank => {
        deck.push({
          color: color,
          rank: rank,
          isWild: rank === '8', // 8s are wild cards that change color
          suit: color // Keep for backward compatibility, but now represents color
        });
      });
    });

    return this.shuffleDeck(deck);
  }

  // Shuffle deck using Fisher-Yates algorithm
  shuffleDeck(deck) {
    for (let i = deck.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [deck[i], deck[j]] = [deck[j], deck[i]];
    }
    return deck;
  }

  // Deal initial hands (7 cards each)
  dealInitialHands(deck, playerCount) {
    const hands = {};
    const cardsPerPlayer = 7;

    for (let player = 0; player < playerCount; player++) {
      hands[player] = [];
      for (let card = 0; card < cardsPerPlayer; card++) {
        hands[player].push(deck.pop());
      }
    }

    return { hands, remainingDeck: deck };
  }

  // Check if a card can be played
  canPlayCard(card, topCard, chosenColor = null) {
    // 8s (wild cards) can always be played
    if (card.rank === '8') return true;

    // If top card is an 8, check against chosen color
    if (topCard.rank === '8' && chosenColor) {
      return card.color === chosenColor;
    }

    // Regular cards: match color or rank
    return card.color === topCard.color || card.rank === topCard.rank;
  }

  // Get next player index
  getNextPlayer(currentPlayer, playerCount) {
    return (currentPlayer + 1) % playerCount;
  }

  // Check if player has won
  hasWon(hand) {
    return hand.length === 0;
  }

  // Calculate score for a hand (for scoring variant)
  calculateScore(hand) {
    return hand.reduce((score, card) => {
      if (card.rank === '8') return score + 50; // 8s worth 50 points
      return score + parseInt(card.rank) || 0; // Number cards worth face value
    }, 0);
  }

  // Find a valid starting card (not an 8)
  findValidStartCard(deck) {
    for (let i = 0; i < deck.length; i++) {
      if (deck[i].rank !== '8') {
        return deck.splice(i, 1)[0];
      }
    }
    // Fallback: return any card (shouldn't happen with a full deck)
    return deck.pop();
  }

  // Get card display info
  getCardDisplay(card) {
    return {
      ...card,
      display: card.rank,
      colorEmoji: this.colorEmojis[card.color],
      shortName: `${card.rank} ${this.colorEmojis[card.color]}`,
      colorClass: card.color
    };
  }

  // Get color choices for when an 8 is played
  getColorChoices() {
    return this.colors.map(color => ({
      color: color,
      emoji: this.colorEmojis[color],
      displayName: color.charAt(0).toUpperCase() + color.slice(1)
    }));
  }

  // Validate if it's a valid color choice
  isValidColor(color) {
    return this.colors.includes(color);
  }
}

module.exports = Crazy8sGameLogic;
