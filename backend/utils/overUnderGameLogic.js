// Over Under Game Logic - Handles trivia questions, scoring, and game flow

class OverUnderGameLogic {
  constructor() {
    // Sample trivia questions with numeric answers
    this.triviaQuestions = [
      { question: "How many U.S. presidents have there been?", answer: 46, category: "History" },
      { question: "What year was the iPhone first released?", answer: 2007, category: "Technology" },
      { question: "How many bones are in the human body?", answer: 206, category: "Science" },
      { question: "What is the speed of light in miles per second (rounded)?", answer: 186282, category: "Science" },
      { question: "How many countries are in the United Nations?", answer: 193, category: "Geography" },
      { question: "What year did World War II end?", answer: 1945, category: "History" },
      { question: "How many days are in a leap year?", answer: 366, category: "General" },
      { question: "How many strings does a standard guitar have?", answer: 6, category: "Music" },
      { question: "What is the boiling point of water in Fahrenheit?", answer: 212, category: "Science" },
      { question: "How many time zones are there in the continental United States?", answer: 4, category: "Geography" },
      { question: "How many chambers does a human heart have?", answer: 4, category: "Science" },
      { question: "What year was Netflix founded?", answer: 1997, category: "Technology" },
      { question: "How many planets are in our solar system?", answer: 8, category: "Science" },
      { question: "What is the legal drinking age in the United States?", answer: 21, category: "General" },
      { question: "How many sides does a hexagon have?", answer: 6, category: "Math" },
      { question: "What year did the Berlin Wall fall?", answer: 1989, category: "History" },
      { question: "How many minutes are in a full day?", answer: 1440, category: "Math" },
      { question: "How many cards are in a standard deck?", answer: 52, category: "General" },
      { question: "What is the freezing point of water in Celsius?", answer: 0, category: "Science" },
      { question: "How many letters are in the English alphabet?", answer: 26, category: "General" },
      { question: "What year did man first land on the moon?", answer: 1969, category: "History" },
      { question: "How many inches are in a foot?", answer: 12, category: "Math" },
      { question: "How many years are in a century?", answer: 100, category: "General" },
      { question: "What is the number of degrees in a circle?", answer: 360, category: "Math" },
      { question: "How many Great Lakes are there?", answer: 5, category: "Geography" },
      { question: "What year was Google founded?", answer: 1998, category: "Technology" },
      { question: "How many milliseconds are in one second?", answer: 1000, category: "Math" },
      { question: "What is the highest possible score in bowling?", answer: 300, category: "Sports" },
      { question: "How many continents are there?", answer: 7, category: "Geography" },
      { question: "What year did the Titanic sink?", answer: 1912, category: "History" }
    ];
  }

  // Create a new game state for Over Under
  createGame(players) {
    const playerList = Array.from(players.values());
    const totalRounds = playerList.length; // One round per player
    
    return {
      phase: 'playing', // lobby, playing, game-over
      currentRound: 1,
      totalRounds: totalRounds,
      roundsCompleted: 0,
      currentAnswerer: null, // Player who answers current question
      currentQuestion: null,
      currentAnswer: null, // Answerer's guess
      correctAnswer: null, // Actual correct answer
      votes: new Map(), // playerId -> 'over' or 'under'
      scores: new Map(), // playerId -> total score
      roundResults: [], // History of each round's results
      votingTimeLeft: 30, // 30 seconds to vote
      isVotingActive: false,
      answererSubmitted: false,
      usedQuestions: new Set() // Track used questions to avoid repeats
    };
  }

  // Start a new round
  nextRound(gameState, players) {
    if (gameState.roundsCompleted >= gameState.totalRounds) {
      return { success: false, gameOver: true };
    }

    const playerList = Array.from(players.values());
    
    // Select answerer for this round (cycle through players)
    const answererIndex = gameState.roundsCompleted % playerList.length;
    gameState.currentAnswerer = playerList[answererIndex].id;
    
    // Get a random question that hasn't been used
    const availableQuestions = this.triviaQuestions.filter(q => 
      !gameState.usedQuestions.has(q.question)
    );
    
    if (availableQuestions.length === 0) {
      // If we've used all questions, reset the pool
      gameState.usedQuestions.clear();
    }
    
    const randomQuestion = availableQuestions[Math.floor(Math.random() * availableQuestions.length)];
    gameState.currentQuestion = randomQuestion;
    gameState.usedQuestions.add(randomQuestion.question);
    
    // Reset round state
    gameState.currentAnswer = null;
    gameState.correctAnswer = randomQuestion.answer;
    gameState.votes.clear();
    gameState.isVotingActive = false;
    gameState.answererSubmitted = false;
    gameState.votingTimeLeft = 30;
    
    return { 
      success: true, 
      question: randomQuestion.question,
      answerer: playerList[answererIndex],
      roundNumber: gameState.currentRound
    };
  }

  // Handle answerer submitting their guess
  submitAnswer(gameState, playerId, answer) {
    if (gameState.currentAnswerer !== playerId) {
      return { success: false, error: 'You are not the answerer for this round' };
    }
    
    if (gameState.answererSubmitted) {
      return { success: false, error: 'Answer already submitted' };
    }
    
    if (typeof answer !== 'number' || isNaN(answer)) {
      return { success: false, error: 'Answer must be a valid number' };
    }
    
    gameState.currentAnswer = answer;
    gameState.answererSubmitted = true;
    gameState.isVotingActive = true;
    gameState.votingTimeLeft = 30; // Reset voting timer
    
    return { 
      success: true, 
      answer: answer,
      message: 'Answer submitted! Other players can now vote.'
    };
  }

  // Handle other players voting over/under
  submitVote(gameState, playerId, vote) {
    if (gameState.currentAnswerer === playerId) {
      return { success: false, error: 'Answerer cannot vote' };
    }
    
    if (!gameState.isVotingActive) {
      return { success: false, error: 'Voting is not currently active' };
    }
    
    if (vote !== 'over' && vote !== 'under') {
      return { success: false, error: 'Vote must be "over" or "under"' };
    }
    
    gameState.votes.set(playerId, vote);
    
    return { 
      success: true, 
      vote: vote,
      message: `Voted ${vote}!`
    };
  }

  // Check if all votes are in or time is up
  areAllVotesIn(gameState, players) {
    const playerList = Array.from(players.values());
    const expectedVotes = playerList.length - 1; // All players except answerer
    return gameState.votes.size >= expectedVotes;
  }

  // Calculate scores after voting ends
  calculateScores(gameState, players) {
    const playerList = Array.from(players.values());
    const correctAnswer = gameState.correctAnswer;
    const playerAnswer = gameState.currentAnswer;
    
    // Determine if actual answer is over or under the player's guess
    let correctVote;
    if (correctAnswer > playerAnswer) {
      correctVote = 'over';
    } else if (correctAnswer < playerAnswer) {
      correctVote = 'under';
    } else {
      correctVote = 'exact'; // Exact match - special case
    }
    
    const results = {
      question: gameState.currentQuestion.question,
      playerAnswer: playerAnswer,
      correctAnswer: correctAnswer,
      correctVote: correctVote,
      winners: [],
      votes: []
    };
    
    // Initialize scores if not already done
    for (const player of playerList) {
      if (!gameState.scores.has(player.id)) {
        gameState.scores.set(player.id, 0);
      }
    }
    
    // Score the votes
    for (const player of playerList) {
      if (player.id === gameState.currentAnswerer) {
        // Answerer gets points if someone voted correctly
        // (Incentive for answerer to give a reasonable guess)
        results.votes.push({
          playerId: player.id,
          playerName: player.name,
          vote: 'answerer',
          correct: false,
          pointsEarned: 0
        });
        continue;
      }
      
      const playerVote = gameState.votes.get(player.id);
      if (!playerVote) {
        // Didn't vote - no points
        results.votes.push({
          playerId: player.id,
          playerName: player.name,
          vote: 'no vote',
          correct: false,
          pointsEarned: 0
        });
        continue;
      }
      
      let isCorrect = false;
      let pointsEarned = 0;
      
      if (correctVote === 'exact') {
        // If exact match, everyone gets points for participation
        isCorrect = true;
        pointsEarned = 100;
      } else if (playerVote === correctVote) {
        // Correct vote gets 150 points
        isCorrect = true;
        pointsEarned = 150;
        results.winners.push(player.name);
      }
      
      if (pointsEarned > 0) {
        const currentScore = gameState.scores.get(player.id) || 0;
        gameState.scores.set(player.id, currentScore + pointsEarned);
      }
      
      results.votes.push({
        playerId: player.id,
        playerName: player.name,
        vote: playerVote,
        correct: isCorrect,
        pointsEarned: pointsEarned
      });
    }
    
    // Store round results
    gameState.roundResults.push(results);
    
    return results;
  }

  // End current round and prepare for next
  endRound(gameState) {
    gameState.roundsCompleted++;
    gameState.currentRound++;
    gameState.isVotingActive = false;
    
    // Check if game is complete
    if (gameState.roundsCompleted >= gameState.totalRounds) {
      gameState.phase = 'game-over';
      return { gameComplete: true };
    }
    
    return { gameComplete: false };
  }

  // Get final scores and determine winner
  getFinalResults(gameState, players) {
    const playerList = Array.from(players.values());
    const finalScores = [];
    
    for (const player of playerList) {
      finalScores.push({
        playerId: player.id,
        playerName: player.name,
        playerColor: player.color,
        totalScore: gameState.scores.get(player.id) || 0
      });
    }
    
    // Sort by score (highest first)
    finalScores.sort((a, b) => b.totalScore - a.totalScore);
    
    return {
      finalScores: finalScores,
      winner: finalScores[0],
      roundHistory: gameState.roundResults
    };
  }

  // Get current game state for display
  getCurrentGameState(gameState, players) {
    const playerList = Array.from(players.values());
    const answerer = playerList.find(p => p.id === gameState.currentAnswerer);
    
    return {
      phase: gameState.phase,
      currentRound: gameState.currentRound,
      totalRounds: gameState.totalRounds,
      question: gameState.currentQuestion?.question,
      answerer: answerer ? answerer.name : null,
      answererId: gameState.currentAnswerer,
      playerAnswer: gameState.currentAnswer,
      isVotingActive: gameState.isVotingActive,
      votingTimeLeft: gameState.votingTimeLeft,
      votesSubmitted: gameState.votes.size,
      totalVotesNeeded: playerList.length - 1, // All except answerer
      scores: Array.from(gameState.scores.entries()).map(([playerId, score]) => {
        const player = playerList.find(p => p.id === playerId);
        return {
          playerId: playerId,
          playerName: player ? player.name : 'Unknown',
          playerColor: player ? player.color : 'gray',
          score: score
        };
      })
    };
  }

  // Check if a player can vote
  canPlayerVote(gameState, playerId) {
    return gameState.isVotingActive && 
           gameState.currentAnswerer !== playerId && 
           !gameState.votes.has(playerId);
  }

  // Check if a player is the current answerer
  isPlayerAnswerer(gameState, playerId) {
    return gameState.currentAnswerer === playerId && !gameState.answererSubmitted;
  }

  // Get random question for testing
  getRandomQuestion() {
    return this.triviaQuestions[Math.floor(Math.random() * this.triviaQuestions.length)];
  }
}

module.exports = OverUnderGameLogic;
