# Shakki Unity Setup Guide

## Scene Setup

To set up the chess game in your Unity scene:

### 1. Create Game Objects

Create the following hierarchy in your scene:

```
GameManager (empty GameObject)
├── Board (empty GameObject)
```

### 2. Add Components

**GameManager object:**
- Add `GameManager` script

**Board object:**
- Add `BoardView` script
- Add `PieceSpriteGenerator` script (generates placeholder sprites)

### 3. Configure Camera

The main camera should be:
- Position: (0, 0, -10)
- Projection: Orthographic
- Size: 5 (adjust as needed to fit board)
- Background: Your preferred color

### 4. Link References

On the `GameManager` component:
- Drag the `Board` object to the `Board View` field

### 5. Play

Enter Play mode. You should see:
- An 8x8 chessboard with alternating colors
- All 32 pieces in starting positions
- Click a piece to select it (highlights legal moves)
- Click a legal move destination to move

## Controls

- **Click** on your piece to select it
- **Click** on highlighted square to move
- **Click** elsewhere to deselect
- After game ends, **click** anywhere to start new game

## Scripts Overview

| Script | Location | Purpose |
|--------|----------|---------|
| `ChessTypes.cs` | Core/ | Enums and structs (Piece, Square, Move) |
| `Board.cs` | Core/ | 8x8 board representation |
| `MoveGenerator.cs` | Core/ | Legal move generation |
| `GameState.cs` | Core/ | Game logic, check/checkmate detection |
| `BoardView.cs` | UI/ | Visual board rendering |
| `PieceSpriteGenerator.cs` | UI/ | Runtime placeholder sprites |
| `GameManager.cs` | Scripts/ | Main game controller |

## Next Steps

1. Replace placeholder sprites with real chess piece art
2. Add UI for score display and game status
3. Implement Shakki score-race rules (Session 2)
