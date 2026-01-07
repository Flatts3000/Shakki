# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Shakki is a Unity 6 (6000.3.2f1) 2D chess game project using the Universal Render Pipeline (URP). It's a "Chess Balatro-like" roguelike where players compete in score-race chess matches with upgradeable pieces, passive abilities, and consumable items. The app is mobile-first, desktop second.

See [docs/game-design.md](docs/game-design.md) for complete game design documentation and [docs/mvp-plan.md](docs/mvp-plan.md) for MVP scope.

## Development Workflow

Tasks are split between Claude and user to minimize Unity Editor interaction:

- **[C] Claude tasks**: Writing scripts, creating editor automation, code-based UI
- **[U] User tasks**: Running menu items, entering Play mode, device testing

### Scene Setup

Run **Shakki > Setup Scene** from the Unity menu to auto-configure the game scene.

## Unity Project Structure

- **Assets/**: Game assets, scripts, scenes, and resources
  - **Scripts/**: All C# game code
    - **Core/**: Chess logic (Board, MoveGenerator, GameState)
    - **UI/**: Visual components (BoardView, HUD)
    - **Editor/**: Unity Editor automation scripts
  - **Scenes/**: Unity scene files
  - **Settings/**: URP and rendering settings
- **Packages/**: Unity package dependencies (managed via manifest.json)
- **ProjectSettings/**: Unity project configuration

## Key Technologies

- **Unity 6** with **Universal Render Pipeline (URP)** for 2D rendering
- **Input System** package (com.unity.inputsystem) for player input handling
- **2D packages**: Animation, Sprite, SpriteShape, Tilemap for 2D game development

## Development Commands

Open the project in Unity Hub or directly in Unity 6000.3.x. Scripts should be placed in `Assets/Scripts/` (create this folder as needed).

### Building
Build via Unity Editor: File → Build Settings → Build

### Testing
Unity Test Framework is available. Create tests in `Assets/Tests/` using NUnit-style test classes with `[Test]` attributes.

Run tests via: Window → General → Test Runner

## Code Conventions

- C# scripts should use PascalCase for public members, camelCase for private fields
- MonoBehaviour scripts go in `Assets/Scripts/`
- Use Unity's new Input System (InputSystem_Actions.inputactions exists in Assets/)

## Code Quality Standards

All decisions must reflect what a **professional game developer with Unity experience** would do:

- No hacky code or half-fixes
- Proper architecture and separation of concerns
- Use Unity best practices (ScriptableObjects for data, events for decoupling, object pooling where needed)
- Write maintainable, readable code that scales
- Handle edge cases properly
- If unsure between approaches, choose the one that ships in real games
