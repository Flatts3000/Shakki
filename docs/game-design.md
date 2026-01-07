# Shakki

**AKA:** "Chess Balatro-like"

**Why the name:** Shakki is "chess" in Finnish.

---

## Match Rules (What a Game of Shakki Is)

### Overview

Shakki is literally chess at the rules level, wrapped in a roguelike meta loop. Each Level is exactly one chess game. There is no level replay or farming.

### Primary Win Condition (Score Race)

- Each match has a **Target Score** (defined by the current Level).
- You gain Score by capturing opponent pieces (base piece values, plus any modifiers).
- A match can end by score only at the end of a Round (see below) when:
  - either player's Score >= Target Score, or
  - a player is checkmated (standard chess).

### Loss Condition (Standard Chess)

- Checkmate ends the match immediately on the move that delivers mate.
- Checkmate overrides score timing (no "finish the round" if mate occurs).

### Round Structure (Fairness for "White Moves First")

To reduce first-move advantage in score-race endings:

- A **Round** = White makes a move, then Black makes a move.
- Score checks happen only after Black's move (end of the Round).
- If a player crosses Target Score during the Round, the Round completes, then:
  - If only one player is >= Target: they win.
  - If both are >= Target: higher Score wins.
  - If tied: Sudden-Death Round (first player to lead after a full Round).

### Move Limit

- Each Level also defines a **Round Limit**.
- If the Round Limit is reached and neither player has won by checkmate or Target Score:
  - higher Score wins; if tied -> one Sudden-Death Round.

---

## Piece Economy & Inventory (Balatro Deck, but Chess Pieces)

### Start of a Run / Baseline

Every player begins with a full standard chess set:
- 16 pieces, plastic, standard distribution.
- You are always restricted to a maximum of **16 pieces** in a match.

### Inventory and Loadout (No Overflow)

Shakki uses a simple model: you own up to 16 pieces total, and you field what you own.

- **Inventory = Loadout** (same thing).
- You must field all pieces you own, up to the 16-piece cap.
- When you acquire a new piece that would take you above 16, you must remove one first (sell, scrap, or otherwise discard).

### King Requirement (Literal Chess)

You must field exactly one (1) King. The King follows all standard chess rules:

- Only the King can be checked or checkmated.
- You may not make moves that leave your King in check.
- If you are checkmated, you lose.

### Between Matches (Meta Progression)

Between games, players purchase boxes to improve their set:
- better materials
- better versions of existing piece types
- (future) additional modifiers

---

## Auto-Deployment (Chess-Shaped Layouts for Weird Armies)

Shakki automatically places pieces to resemble a real chess setup even when the player's collection is non-standard (e.g., 0 pawns, 3 rooks, 5 queens).

### Deployment Zone

Pieces are placed within the first two ranks:
- back rank + pawn rank
- If a piece cannot fit into its ideal square(s), it spills into remaining legal squares using stable tie-break rules.

### Placement Goals (Priority Order)

1. Canonical chess positions when possible
2. Symmetry (mirror left/right whenever pairs exist)
3. Center-weighting for King/Queen
4. Pawns in front when present
5. Minimize weirdness (avoid strange placements unless forced)

### Deterministic Placement Algorithm (High-Level)

Given the owned set of pieces:

#### Step A - Place Pawns

Place up to 8 pawns on the pawn rank (files a->h), center-out symmetry:
- order: d, e, c, f, b, g, a, h
- If fewer than 8 pawns, leave gaps (do not fill gaps yet).

#### Step B - Place King / Queen

- Place King on e-file back rank if open; else nearest open square to e1 using distance ties:
  - prefer d1/f1, then c1/g1, then b1/h1, then pawn-rank centers.
- Place Queen on d-file back rank if open; else nearest open square to d1 using the same tie logic.

#### Step C - Place Rooks

- Place rooks on a/h back rank when possible.
- If unavailable, place inward while maintaining symmetry.

#### Step D - Place Bishops, then Knights

- Prefer bishops on c/f, knights on b/g when available.
- Extras fill remaining back-rank squares from outside-in, paired symmetrically when possible.

#### Step E - Place Any Remaining Pieces

- Fill remaining back-rank squares first (center-out).
- Then fill pawn-rank gaps (center-out).
- Always place mirrored pairs when possible.

### Determinism (Stable Ordering)

When multiple identical pieces exist, placement remains deterministic by sorting piece instances by:
1. Material tier
2. (future) modifier tier
3. stable ID

### Optional: Manual Nudge (TBD)

We may allow limited manual adjustment:
- Auto-place always runs first.
- Player may perform a single "rearrange" action pre-game:
  - swap up to two pieces' starting squares (or drag within the deployment zone).
- Ranked multiplayer can disable this or enforce a short time limit.

---

## Multiplayer Structure (Run-Based Progression, Balatro-Style)

### Key Constraints

- Multiplayer is the first release target.
- A run is the primary progression path.
- Each Level is exactly one game with a Target Score and Round Limit.
- Players are only matched against other players on the same Level.
- Single-player uses bots.

### Run Flow

1. Players start a run at Level 1 with a baseline set.
2. Winning a level advances you to Level N+1.
3. Losing a level ends the run immediately.
4. The player must start over from Level 1 on a new run.

This mirrors Balatro's structure: you're trying to string wins together under escalating constraints, not grind a tier.

### Matchmaking Model

Each Level functions as a matchmaking bucket:
- e.g., "Run - Level 7"
- Players queue into the bucket for their current run level.
- The game matches players within that bucket based on:
  - connection quality / region
  - queue time
  - (optional) hidden skill rating as a secondary tie-breaker

### ELO / Rating (Optional, Non-Progression)

If we include rating, it should not determine level access. Rating is used for:
- improving match quality over time
- leaderboards
- seasonal bragging rights

Rating does not prevent a player from attempting any run level they reach.

### Preventing "Stacked Inventory" Imbalance

Because players at the same Level must have comparable constraints, ranked multiplayer should enforce one of these:

**Option A (Recommended): Level Budget Cap**
- Each piece has a Power Cost based on material/mods.
- Each Level defines a Budget Cap.
- Your 16-piece set must fit under the cap for that Level.

**Option B: Run-Scoped Inventory**
- Your persistent collection exists, but a run uses a run-scoped set that starts baseline and upgrades only during that run.
- This is the closest to Balatro's feel (build grows during the run), and prevents long-term account advantage.

**Recommendation:** Prefer Option B for the most Balatro-like experience. Use Option A if you want persistent collection to matter in ranked without breaking fairness.

---

## Boxes of Pieces (Between Matches)

Because players start with a full set, boxes focus on upgrades and specialization, not acquiring basic types.

- A box contains a small set of random pieces (types + materials).
- You may:
  - keep a piece (replacing an existing one if at cap),
  - swap it into your set,
  - or sell it for Coins.
- (Future) merge/upgrade systems are possible but not required for MVP.

---

## Court of Nobles (Persistent Passives)

### Concept

Your Court is the collection of nobles, specialists, and retainers backing your run. Each Court member provides a persistent passive that shapes how you score, how you earn Coins, and how you approach positions—without changing chess legality.

**Court = your run identity.**

### Structure

- Players have **Court Slots** (start small, expand during the run).
- Each purchase adds a Court Member to your Court (if you have an open slot) or forces a replacement choice.
- Court Members are rarity-tiered (Common / Rare / Epic / Legendary).

### Design Rules (so it scales and stays readable)

- Each Court Member should be describable as one trigger + one payoff.
- Never enables illegal moves.
- Bonuses are mostly score multipliers, small Coins bumps, or shop manipulation.

### Court Member Families (for quantity + variety)

These families let you build 50+ compelling passives without inventing new mechanics:

| Family | Description |
|--------|-------------|
| Development | reward developing minors, connecting rooks, etc. |
| King Safety | castling incentives, punishing exposed kings |
| Center Control | captures/moves in central squares |
| Files & Diagonals | rook on open file, bishop long diagonal |
| Punish Blunders | hanging pieces, undefended captures |
| Trading Philosophy | trade up, simplify, "win by conversion" |
| Endgame Conversion | bonuses when pieces are low |
| Bounty Synergy | refill bounty charges, increase bounty multipliers slightly, etc. |

### Example Court Members (Illustrative)

- **The Marshal** - If you castle this game, gain +Coins and your next capture scores x2.
- **The Spymaster** - Captures of undefended pieces score x2.
- **The Chancellor** - Your first capture each game grants +Coins.
- **The Cartographer** - Captures in the center (d4/e4/d5/e5) score x2.
- **The Warden** - If your King is not checked for 6 Rounds, gain +Score.

*(Names are placeholders—mechanics are the important part.)*

---

## Medallions (Single-Use Clutch Tools)

### Concept

Medallions are single-use relics awarded by the Court—tokens you spend at the perfect moment. They are consumables that create clutch swings or reward executing "deep-cut" tactical motifs.

**Medallions = your mid-game spike / save tools.**

### Structure

- Players can hold **0-3 Medallions** at a time.
- Bought in the shop between levels.
- Activated during a match in clear timing windows:
  - before your move
  - after your capture
  - after opponent's capture
  - end of round
- Medallions never make illegal moves legal. They affect score, economy, risk, or variance.

### Medallion Design Templates (these scale fast)

To build 60+ without chaos, most Medallions should fit one of these patterns:

#### Template A: "Arm a Motif"

Next time you execute motif X, reward Y.

Examples:
- **Medallion of the Fork** - Next time your move attacks two enemy pieces, your next capture scores x3.
- **Medallion of the Pin** - Next time you capture a pinned piece, that capture scores x3.

#### Template B: "Mark a Target"

Your next capture of piece-type T is multiplied.

Examples:
- **Medallion of the Hunt** - Choose Pawn/Knight/Bishop/Rook/Queen; your next capture of that type scores x3.
- **Medallion of the Execution** - Your next capture scores x2.

#### Template C: "Insurance / Escape Valve"

Prevent disaster or convert a bad situation into a survivable one.

Examples:
- **Medallion of Mercy** - The first time a non-king piece would be captured this game, negate the bonus score the opponent would gain from that capture (or grant you compensation Coins).
- **Medallion of Ransom** - Instantly sacrifice one of your non-king pieces for Coins (piece is removed).

*(We'll tune these carefully—insurance is powerful.)*

### Teaching "Deep Cuts" Through Medallions

Medallions are the perfect delivery mechanism for chess jargon:
- Fork, Pin, Skewer, Discovered Attack, Zwischenzug, Deflection, Decoy, Remove the Defender, Back Rank threats.

Each Medallion can include a short optional tooltip + micro-demo.

---

## Bounties (Run-Long Progression Upgrades)

### What a Bounty Is

A Bounty is a run-long upgrade purchased in the shop between levels. Each bounty targets one enemy piece type, and multiplies the Score you gain when you capture that type.

- Bounties are **run-scoped** (reset when the run ends).
- There is no King bounty (Kings aren't captured; checkmate ends the game).

### Eligible Bounty Types

- Pawn Bounty
- Knight Bounty
- Bishop Bounty
- Rook Bounty
- Queen Bounty

### Scoring Rule

When you capture an enemy piece of type T:

```
CaptureScore = BaseValue(T) x Material/Modifier Effects x BountyMultiplier(T)
```

### Shop Behavior

#### How You Buy/Upgrade

- Each purchase increases the bounty's level by +1.
- Buying "Pawn Bounty" when you already have Pawn Bounty Level 2 upgrades it to Level 3 (x4).
- Prices scale with:
  - piece value (Queen > Pawn)
  - current level (Level 3 costs more than Level 1)

#### Offer Rate

To keep bounties feeling like a real build axis:
- Each shop shows 1-2 Bounty offers by default.
- (Optional later) Add a "Bounty reroll" shop action or a rare "Wildcard Bounty" that upgrades any bounty by +1.
