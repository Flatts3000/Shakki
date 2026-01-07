# Shakki

A chess roguelike game built with Unity 6. Combine strategic chess gameplay with roguelike progression - capture pieces to score points, advance through levels, and build your army in the shop.

## Features

- **Score-Race Chess** - Capture pieces to earn points. First to reach the target score wins, or checkmate your opponent.
- **Roguelike Progression** - Win matches to advance levels with increasing difficulty. Lose and start over.
- **Army Building** - Earn coins from captures, spend them in the shop to upgrade your 16-piece army.
- **Auto-Deployment** - Non-standard armies are automatically placed using smart positioning.
- **Multiplayer** - Online matchmaking with level-based brackets via Unity Relay.
- **Mobile-First** - Touch controls, safe area support, optimized for phones.

## Getting Started

### Requirements

- Unity 6 (6000.3.2f1 or later)
- Unity packages (auto-installed):
  - Netcode for GameObjects
  - Unity Relay, Lobby, Authentication
  - Input System
  - TextMeshPro

### Setup

1. Clone the repository
2. Open in Unity Hub
3. Go to **Shakki > Setup Scene** in the menu bar
4. Press Play to test

### Building

1. **Shakki > Build > Configure Player Settings** (one-time)
2. Choose your platform:
   - **Shakki > Build > Windows** - Creates EXE
   - **Shakki > Build > Android (APK)** - Creates APK
   - **Shakki > Build > Android (AAB)** - For Play Store
   - **Shakki > Build > iOS** - Creates Xcode project

Builds output to `Builds/[Platform]/[Version]/`

## How to Play

1. **Start Run** - Begin at Level 1 with a standard chess army
2. **Play Match** - Capture pieces to score points. Reach the target score or checkmate to win.
3. **Visit Shop** - Spend earned coins on new pieces or sell unwanted ones
4. **Advance** - Each level increases the target score and round limit
5. **Game Over** - Lose a match and your run ends. Start fresh from Level 1.

### Controls

- **Click/Tap** a piece to select it
- **Click/Tap** a highlighted square to move
- **Drag** a piece directly to its destination

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Chess engine, game state, run management
│   ├── UI/             # All UI components (programmatic)
│   ├── Network/        # Multiplayer networking
│   ├── Audio/          # Audio management
│   └── Editor/         # Editor tools and build scripts
├── Resources/
│   └── Audio/          # Sound effect files
├── Scenes/
│   └── SampleScene.unity
└── Tests/
    └── Editor/         # Unit tests
```

## Adding Audio

Place audio files in `Assets/Resources/Audio/` with these names:
- `PieceMove.wav`, `PieceCapture.wav`, `Check.wav`, `Checkmate.wav`
- `ButtonClick.wav`, `CoinEarn.wav`, `LevelUp.wav`
- `MatchWin.wav`, `MatchLose.wav`, `ShopOpen.wav`, `ShopPurchase.wav`

## Tech Stack

- **Engine**: Unity 6 with Universal Render Pipeline (URP)
- **Networking**: Unity Netcode for GameObjects + Relay + Lobby
- **Input**: Unity Input System
- **UI**: Programmatic UI (no prefabs) with TextMeshPro

## License

All rights reserved.
