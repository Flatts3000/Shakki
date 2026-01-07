# Shakki MVP Plan

This document outlines the Minimum Viable Product scope for Shakki.

---

## MVP Status: COMPLETE

All 9 working sessions have been completed. The game is ready for playtesting and asset integration.

**Build Options:**
- Windows: `Shakki > Build > Windows` (outputs to `Builds/Windows/`)
- Android: `Shakki > Build > Android (APK)`
- iOS: `Shakki > Build > iOS (Xcode Project)`

---

## MVP Goals

- Playable multiplayer chess with score-race mechanics
- Basic run progression (Level 1 through N)
- Core meta-systems: pieces, shop, one progression axis
- Mobile-first UI/UX

---

## Core Features (Must Have)

### Chess Engine

- [x] Standard chess rules (legal moves, check, checkmate, stalemate)
- [x] Piece movement and capture logic
- [x] Turn-based play (White/Black alternation)

### Score-Race System

- [x] Base piece values for scoring
- [x] Target Score per level
- [x] Round structure (score checks after Black's move)
- [x] Win condition: reach Target Score or checkmate
- [x] Round Limit with sudden-death tiebreaker

### Piece Inventory

- [x] 16-piece cap enforcement
- [x] Baseline starting set (standard chess pieces)
- [x] Piece swapping when acquiring new pieces

### Auto-Deployment

- [x] Deterministic placement algorithm
- [x] Handle non-standard army compositions

### Run Progression

- [x] Level advancement on win
- [x] Run ends on loss
- [x] Escalating Target Score / Round Limit per level

### Shop (Between Matches)

- [x] Basic box opening (random pieces)
- [x] Sell pieces for Coins
- [x] Coins persistence within run

### Multiplayer

- [x] Real-time match against another player
- [x] Level-based matchmaking buckets
- [x] Basic connection handling

### UI/UX (Mobile-First)

- [x] Touch-based piece selection and movement
- [x] Score display
- [x] Turn indicator
- [x] End-of-match results screen
- [x] Shop interface

---

## Working Sessions

Each session lists tasks as:
- **[C]** = Claude does this (code/scripts)
- **[U]** = User does this in Unity Editor

---

### Session 1: Chess Foundation [COMPLETE]

**Goal:** Playable local chess with standard rules

- [x] Set up project structure (Assets/Scripts/)
- [x] Implement board representation (8x8 grid, square coordinates)
- [x] Create piece data model (type, color, position)
- [x] Implement legal move generation for all piece types
- [x] Add check detection
- [x] Add checkmate/stalemate detection
- [x] Basic board visualization with runtime-generated sprites
- [x] Click-to-select, click-to-move input
- [x] Create Editor script to auto-setup scene (menu item)
- [U] Run menu item: Shakki > Setup Scene

**Deliverable:** Two players can play chess locally on one device

---

### Session 2: Score-Race Rules [COMPLETE]

**Goal:** Chess with Shakki's scoring system

- [x] Create ShakkiGameState extending GameState with score-race logic
- [x] Implement Round structure (White + Black = 1 Round)
- [x] Add Target Score and Round Limit to level config
- [x] Implement score-check timing (end of Round only)
- [x] Add win conditions: Target Score, checkmate, Round Limit
- [x] Add sudden-death tiebreaker logic
- [x] Create GameHUD script with score/round display (UI via code)
- [x] Update GameManager to use ShakkiGameState
- [U] Enter Play mode to test

**Deliverable:** Local match can end by score or checkmate

---

### Session 3: Inventory & Auto-Deployment [COMPLETE]

**Goal:** Non-standard armies with automatic placement

- [x] Create PieceInventory class (list of owned pieces)
- [x] Create InventoryPiece struct (type, material tier, ID)
- [x] Enforce 16-piece cap and exactly-one-King rule
- [x] Implement AutoDeployer with algorithm from design doc
- [x] Create unit tests for edge cases (0 pawns, 5 queens, etc.)
- [x] Create debug UI to modify inventory at runtime
- [x] Integrate with Board setup
- [U] Enter Play mode, use debug UI to test weird armies

**Deliverable:** Game correctly places any legal 16-piece army

---

### Session 4: Run Progression & Level System [COMPLETE]

**Goal:** Multi-level run with escalating difficulty

- [x] Create LevelConfig ScriptableObject (Target Score, Round Limit)
- [x] Create LevelDatabase with level progression curve
- [x] Create RunState class (current level, inventory, coins)
- [x] Create RunManager to handle level transitions
- [x] Create GameFlowController (states: Menu, Match, Shop, Results)
- [x] Create ResultsScreen UI (code-based Canvas)
- [x] Create LevelInfoHUD showing current level stats
- [x] Wire up win -> next level, loss -> run end
- [U] Enter Play mode, play through multiple levels

**Deliverable:** Player can progress through multiple levels until loss

---

### Session 5: Shop System [COMPLETE]

**Goal:** Between-match piece acquisition

- [x] Add Coins to RunState
- [x] Create ShopManager with box generation logic
- [x] Create BoxContents class (random piece selection)
- [x] Implement piece pricing and sell values
- [x] Create ShopScreen UI (code-based Canvas)
- [x] Implement keep/swap/sell actions
- [x] Add Coins earned from captures during match
- [x] Create "Continue" flow from shop to next match
- [U] Enter Play mode, win a match, test shop

**Deliverable:** Player can spend Coins to modify their army between levels

---

### Session 6: Mobile UI/UX [COMPLETE]

**Goal:** Touch-friendly interface

- [x] Refactor input to use Unity Input System actions
- [x] Add touch drag-to-move with visual feedback
- [x] Add move confirmation (tap destination to confirm)
- [x] Create responsive Canvas scaler settings
- [x] Implement safe area handling for notches
- [x] Add large touch targets for UI buttons
- [x] Create portrait-optimized layout
- [U] Build to device or use Device Simulator
- [U] Test touch controls

**Deliverable:** Game is playable on mobile devices

**Key Files:**
- `Assets/Scripts/UI/MobileUIConstants.cs` - Touch target sizes, font sizes
- `Assets/Scripts/UI/SafeAreaHandler.cs` - Notch/safe area handling

---

### Session 7: Networking Foundation [COMPLETE]

**Goal:** Basic client-server architecture

- [x] Add Unity Netcode for GameObjects package reference
- [x] Create NetworkGameManager with host/client logic
- [x] Create NetworkBoard for state sync
- [x] Implement ServerRpc for move submission
- [x] Implement ClientRpc for board updates
- [x] Create basic lobby UI (Host/Join buttons)
- [x] Add connection state handling and error display
- [U] Build two instances, test local network play

**Deliverable:** Two remote players can connect to same match

**Key Files:**
- `Assets/Scripts/Network/NetworkGameManager.cs` - Host/client management
- `Assets/Scripts/Network/NetworkBoard.cs` - Board state synchronization
- `Assets/Scripts/Network/LobbyUI.cs` - Host/Join interface

---

### Session 8: Multiplayer Match Flow [COMPLETE]

**Goal:** Complete networked match experience

- [x] Create MatchmakingManager with level-bucket logic
- [x] Integrate with Unity Relay for NAT traversal
- [x] Create matchmaking queue UI
- [x] Sync RunState between players (level verification)
- [x] Handle disconnections (forfeit logic)
- [x] Create rematch/continue flow
- [x] Add match result reporting to RunManager
- [U] Test matchmaking with two devices/builds

**Deliverable:** Two players can matchmake, play, and progress in runs

**Key Files:**
- `Assets/Scripts/Network/MatchmakingManager.cs` - Unity Lobby + Relay integration
- `Assets/Scripts/Network/MatchmakingUI.cs` - Queue status display
- `Assets/Scripts/Network/NetworkMatchController.cs` - Match flow, disconnections, rematch
- `Assets/Scripts/Network/NetworkPostMatchUI.cs` - Results and rematch UI

**Dependencies Added:**
- `com.unity.netcode.gameobjects` 2.3.0
- `com.unity.services.relay` 1.1.1
- `com.unity.services.lobby` 1.2.2
- `com.unity.services.authentication` 3.3.3

---

### Session 9: Integration & Polish [COMPLETE]

**Goal:** MVP-complete build

- [x] Create MainMenu scene with UI
- [x] Add scene loading/transitions
- [x] Performance profiling and optimization
- [x] Add basic audio manager (sound effect hooks)
- [x] Create build scripts for iOS/Android/Windows
- [U] Import piece sprite assets (or use generated)
- [U] Import audio assets
- [U] Configure build settings
- [U] Build and test on devices
- [U] Playtest and report bugs for fixing

**Deliverable:** Shippable MVP build

**Key Files:**
- `Assets/Scripts/UI/SceneTransitionManager.cs` - Fade transitions
- `Assets/Scripts/Audio/AudioManager.cs` - Music and SFX playback
- `Assets/Scripts/Audio/GameAudioHooks.cs` - Auto-connects game events to audio
- `Assets/Scripts/Core/PerformanceOptimizer.cs` - Frame rate, battery optimization
- `Assets/Scripts/Editor/BuildTools.cs` - Build menu items

**Audio Setup:**
Place audio files in `Assets/Resources/Audio/` named:
- `PieceMove.wav`, `PieceCapture.wav`, `Check.wav`, `Checkmate.wav`
- `ButtonClick.wav`, `CoinEarn.wav`, `LevelUp.wav`
- `MatchWin.wav`, `MatchLose.wav`, `ShopOpen.wav`, `ShopPurchase.wav`

---

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
│   └── Audio/          # Sound effect files (user provides)
├── Scenes/
│   └── SampleScene.unity
└── Tests/
    └── Editor/         # Unit tests
```

---

## How to Setup Scene

1. Open Unity project
2. Go to **Shakki > Reset Scene** (if needed)
3. Go to **Shakki > Setup Scene**
4. Enter Play mode to test

---

## How to Build

1. **Shakki > Build > Configure Player Settings** (one-time)
2. Choose build target:
   - **Shakki > Build > Windows** - Creates EXE
   - **Shakki > Build > Android (APK)** - Creates APK
   - **Shakki > Build > Android (AAB)** - For Play Store
   - **Shakki > Build > iOS** - Creates Xcode project
3. Builds output to `Builds/[Platform]/[Version]/`

---

## Deferred Features (Post-MVP)

### Meta Systems

- Court of Nobles (passive abilities)
- Medallions (consumables)
- Bounties (capture multipliers)
- Material tiers (piece upgrades beyond baseline)

### Progression

- ELO / rating system
- Leaderboards
- Seasonal content

### Polish

- Manual deployment nudge
- Piece animations and VFX
- Sound design
- Tutorial / onboarding

### Platform

- Desktop optimization
- Cross-platform play

---

## Open Questions

- Which balance option for ranked: Level Budget Cap or Run-Scoped Inventory?
- Bot difficulty tiers for single-player?
- Monetization model?

---

## Success Criteria

MVP is complete when:

1. ~~Two players can matchmake and complete a full chess match with score-race rules~~ **DONE**
2. ~~Winner advances, loser restarts from Level 1~~ **DONE**
3. ~~Shop allows basic piece acquisition between levels~~ **DONE**
4. ~~Playable on mobile devices~~ **DONE**

**MVP COMPLETE - Ready for playtesting and asset integration**
