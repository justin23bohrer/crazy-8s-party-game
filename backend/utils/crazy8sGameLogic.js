// Crazy 8s Game Logic - Handles deck, cards, and game rules
class Crazy8sGameLogic {
  constructor() {
    this.suits = ['hearts', 'diamonds', 'clubs', 'spades'];
    this.ranks = ['A', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K'];
    this.suitSymbols = {
      hearts: '♥️',
      diamonds: '♦️',
      clubs: '♣️',
      spades: '♠️'
    };
  }

  // Create a standard 52-card deck
  createDeck() {
    const deck = [];
    
    this.suits.forEach(suit => {
      this.ranks.forEach(rank => {
        deck.push({
          suit: suit,
          rank: rank,
          isWild: rank === '8',
          color: (suit === 'hearts' || suit === 'diamonds') ? 'red' : 'black'
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
  canPlayCard(card, topCard, chosenSuit = null) {
    // 8s (wild cards) can always be played
    if (card.rank === '8') return true;

    // If top card is an 8, check against chosen suit
    if (topCard.rank === '8' && chosenSuit) {
      return card.suit === chosenSuit;
    }

    // Regular cards: match suit or rank
    return card.suit === topCard.suit || card.rank === topCard.rank;
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
      if (['J', 'Q', 'K'].includes(card.rank)) return score + 10; // Face cards worth 10
      if (card.rank === 'A') return score + 1; // Aces worth 1
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
      suitSymbol: this.suitSymbols[card.suit],
      shortName: `${card.rank}${this.suitSymbols[card.suit]}`,
      colorClass: card.color
    };
  }

  // Get suit choices for when an 8 is played
  getSuitChoices() {
    return this.suits.map(suit => ({
      suit: suit,
      symbol: this.suitSymbols[suit],
      color: (suit === 'hearts' || suit === 'diamonds') ? 'red' : 'black'
    }));
  }

  // Validate if it's a valid suit choice
  isValidSuit(suit) {
    return this.suits.includes(suit);
  }
}

module.exports = Crazy8sGameLogic;
