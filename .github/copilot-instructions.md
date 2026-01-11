# VR RTS Game - AI Coding Guidelines

## Project Overview
This is a VR-enabled multiplayer Real-Time Strategy game built in Unity, featuring custom **Swordfish** framework for navigation, networking, and game systems.

## Architecture Patterns

### Core Framework Structure
- **Swordfish Namespace**: Custom framework providing navigation (`Swordfish.Navigation`), audio, utilities, and core types
- **Grid-Based World System**: All navigation uses a grid-based world with `Cell`, `Grid`, and `World` classes in `Swordfish.Navigation`
- **V2 Pattern**: Core game classes use "V2" suffix (`UnitV2`, `ActorV2`, `SoldierV2`, `VillagerV2`) indicating the current architecture iteration

### Key System Boundaries
- **RTSCore/**: Core game logic for units, buildings, factions, and data structures
- **Tech/**: Technology tree system with `TechTree`, `TechNode`, upgrades, and research
- **Swordfish/**: Framework layer providing navigation, utilities, audio, and base classes
- **GameMaster**: Central singleton providing database access and game-wide resources

### Data Architecture
- **ScriptableObjects for Configuration**: `Faction`, `TechTree`, `UnitData`, `BuildingData` use ScriptableObject pattern
- **Database Pattern**: `AudioDatabase`, `ResourceNodeDatabase`, `BuildingDatabase`, `UnitDatabase` provide centralized data access
- **Attribute System**: Units use flexible attribute system with `AttributeType` enum and modifiers for stats

### Navigation System
- **Grid-Based**: World divided into `Cell` objects managed by `Grid` class
- **Layer-Based Movement**: `NavigationLayers` enum controls what can move where (water, land, etc.)
- **Pathfinding**: Custom A* implementation in `Path.Find()` with occupation checking
- **Body Registration**: All movable objects inherit from `Body` and register with `World`

### Class Hierarchy
- **ActorV2**: Base class for all movable entities, inherits from `Body`, implements `IActor`
  - Provides behavior tree integration, animation control, and movement logic
  - All units (`UnitV2`, `SoldierV2`, `VillagerV2`) and `Fauna` inherit from this
  - Contains data bindings for state management (`Order`, `Target`, `Destination`, etc.)
- **Body**: Foundation class for grid-occupying objects with faction, attributes, and damage system

### AI Behavior System
- **Behavior Trees**: Comprehensive AI system using `Swordfish.Library.BehaviorTrees`
  - Node types: `BehaviorAction`, `BehaviorCondition`, `BehaviorCompositor`, `BehaviorDecorator`
  - Each ActorV2 implements `BehaviorTreeFactory()` to define AI logic
  - Examples: `SoldierBehaviorTree`, `VillagerBehaviorTree`, `PriestBehaviorTree`
  - Centralized ticking via `PlayerManager.TickActorBehaviorTree()`

### VR Building System
- **ThrowableBuilding**: VR interaction for building placement via hand throwing
- **BuildMenu**: Hand-mounted UI with tabs and slots for building selection  
- **Construction Pipeline**: ThrowableBuilding → Constructible → Built Structure
- **Grid Validation**: Buildings check `Cell.occupied` and `NavigationLayers` for placement
- **Multi-Stage Construction**: `Constructible` has construction stages before completion

## Development Patterns

### Networking Architecture
- **Unity Netcode**: Built on Unity's Netcode for GameObjects
- **Command Line Control**: `NetworkCommandLine` parses `-mode` args (server, host, client)
- **Launch Scripts**: `Build/launch_host.bat`, `Build/launch_client.bat` for testing
- **Faction-Based Multiplayer**: All entities belong to a `Faction` with alliance relationships

### Naming Conventions
- **V2 Classes**: Current architecture (`UnitV2`, `ActorV2`) - prefer these over legacy versions
- **Swordfish Prefix**: Framework classes use `Swordfish` namespace consistently
- **Database Suffix**: Data containers use `Database` suffix (`UnitDatabase`, `BuildingDatabase`)

### Multiplayer Architecture
- **Host/Client Model**: Use `Build/launch_host.bat` and `Build/launch_client.bat` for testing
- **Faction-Based**: All entities belong to a `Faction` with alliance/enemy relationships
- **NetworkCommandLine**: Handles multiplayer command parsing and execution

### VR Integration
- **SteamVR Framework**: Uses Valve's SteamVR for VR interactions
- **Hand-Based UI**: Menu systems designed for VR hand controllers
- **PlayerManager**: Central hub managing VR player state, resources, and hand interactions

## Critical File Locations

### Core Systems
- `Assets/Scripts/PlayerManager.cs` - VR player controller and resource management
- `Assets/Scripts/GameMaster.cs` - Central singleton for database access and game state
- `Assets/Scripts/RTSCore/` - Core game entities and data structures
- `Assets/Swordfish/Navigation/World.cs` - Grid-based navigation system

### Data Configuration
- `Assets/Scripts/Tech/TechTree.cs` - Technology progression system
- `Assets/Scripts/RTSCore/Faction.cs` - Player faction definitions
- Database classes in `RTSCore/` for units, buildings, resources

### Framework Utilities
- `Assets/Swordfish/Singleton.cs` - Singleton pattern implementation
- `Assets/Swordfish/Navigation/` - Grid navigation and pathfinding
- `Assets/Swordfish/Util/` - Math utilities, coordinates, and helper functions

## Development Workflow

### Building and Testing
```bash
# Build is already configured - use Unity Editor or build process
# Test multiplayer with provided batch files:
./Build/launch_host.bat     # Start as host
./Build/launch_client.bat   # Connect as client
./Build/launch_both.bat     # Launch both for testing
```

### Key Debugging Patterns
- **Grid Visualization**: `World.showDebugGrid` enables visual grid debugging
- **Faction Debugging**: Use `GameMaster.Factions` to inspect faction relationships
- **Navigation Issues**: Check `Cell.occupied` and `NavigationLayers` for movement problems

## Integration Points
- **SteamVR**: All VR interactions route through SteamVR action system
- **Swordfish Framework**: Custom navigation, audio, and utility systems
- **Unity Networking**: Multiplayer built on Unity's networking with custom command layer
- **ScriptableObject Data**: All game configuration stored as ScriptableObjects for runtime and editor access

When adding new features, follow the V2 naming pattern, use the Swordfish framework for navigation/utilities, and leverage the existing database pattern for data management.