# Changelog

## 1.5.0
- **Major NPC Healthbar Overhaul**
  - Migrated NPC healthbars from screen-space HUD to world-space UI elements positioned above each NPC's head
  - Added support for displaying multiple NPC healthbars simultaneously (all NPCs within configurable radius)
  - Implemented billboard behavior so healthbars always face the camera
  - Added automatic fade in/out based on NPC state (fades out when NPC is dead, knocked out, or in a building)
  - New configuration options:
    - `NPCWorldSpaceRadius`: Maximum distance to display NPC healthbars (default: 50m)
    - `NPCHeadOffset`: Vertical offset above NPC for healthbar position (default: 2m)
    - `NPCHealthBarScale`: Multiplier for NPC health bar size (base scale: 0.005, default: 1.0). Use simple values like 0.5, 1.0, 1.5, etc.
  - Improved performance with distance-based filtering using squared distance calculations
  - Enhanced lifecycle management for automatic cleanup when NPCs leave range or are destroyed

## 1.4.0
- Cartel Update
- Refactored health bar system and introduced a unified HealthBar class for improved functionality and maintainability
- Consolidated player, NPC, and multiplayer health bar management into a single unified system
- Enhanced logging and initialization processes
- Improved Mono compatibility and performance

## 1.3.0
- BETA Upload
- Added Mono support
- Slightly tweaked some logic

## 1.2.9
- BETA Upload
- Adds multiplayer support

## 1.2.5
- Finalized NPC tracking by capping distance health can be seen.

## 1.1
- Added nearest NPC health tracking and a few config options

## 1.0.1
- Added extra config options for animations and fixed an issue with healthbar fading under certain circumstances.
## 1.0.0
- Initial upload
