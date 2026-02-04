# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**NoNameTag** is a Rocket.Unturned server plugin that manages player name tags with customizable display effects based on permissions. It provides:

- Custom name tags with prefixes, suffixes, and colored names
- Permission-based display effects (VIP, MVP, etc.)
- Chat message formatting with colored names
- Customizable death messages
- Configuration-driven permission groups

## Architecture

### Core Components

**Plugin Entry Point** (`NoNameTagPlugin.cs`)
- Main plugin class extending `RocketPlugin<NoNameTagConfiguration>`
- Manages service lifecycle (load/unload)
- Registers event handlers for player connections, disconnections, chat, and deaths
- Coordinates between services

**Configuration** (`NoNameTagConfiguration.cs`)
- XML-serializable configuration class
- Defines permission groups with priorities
- Configurable display effects (prefix, colors, suffix)
- Death message formats for different death causes
- Priority modes: `FirstMatch` or `HighestPriority`

### Service Layer

**PermissionService** (`Services/PermissionService.cs`)
- Resolves player permissions against configured groups
- Implements 5-minute cache for permission checks
- Supports priority-based permission selection
- Cache cleanup on player disconnect

**NameTagManager** (`Services/NameTagManager.cs`)
- Manages player display effects in memory
- Applies/removes effects to players
- Handles cache cleanup (max 1000 entries)
- Refreshes all online players

**NameTagDisplayService** (`Services/NameTagDisplayService.cs`)
- Modifies player nicknames in-game
- Stores original names for restoration
- Applies formatted names with colors and prefixes
- Handles delayed name tag application (waiting for player initialization)

**DeathMessageService** (`Services/DeathMessageService.cs`)
- Formats death messages based on death cause
- Supports 13 different death types (player kill, zombie, animal, vehicle, etc.)
- Broadcasts formatted messages to all players
- Uses colored names in death messages

### Data Models

**DisplayEffectConfig** (`Models/DisplayEffectConfig.cs`)
- Defines visual appearance: prefix, prefix color, name color, suffix, suffix color

**PermissionGroupConfig** (`Models/PermissionGroupConfig.cs`)
- Links permission string to display effect
- Includes priority for multi-permission scenarios

**DeathMessageConfig** (`Models/DeathMessageConfig.cs`)
- 13 configurable message formats for different death causes

### Utilities

**NameFormatter** (`Utilities/NameFormatter.cs`)
- Formats player names with Unity color tags
- Converts hex colors to RGB values

**ConfigValidator** (`Utilities/ConfigValidator.cs`)
- Validates hex colors, permissions, priorities
- Ensures configuration integrity on load

**PluginLogger** (`Utilities/Logger.cs`)
- Structured logging with categories (Plugin, Permission, NameTag, DeathMessage, Command, Configuration)
- Debug mode support with stack traces

**Constants** (`Utilities/Constants.cs`)
- Centralized constants for colors, priorities, permissions, cache settings

### Commands

**NameTagCommand** (`Commands/NameTagCommand.cs`)
- `/nametag reload` - Reload configuration and refresh all players
- `/nametag refresh [player]` - Refresh display for specific or all players
- `/nametag check [player]` - View player's current permission group and display effect
- Requires `nonametag.admin` permission

## Build Commands

### Building the Project
```bash
# Using Visual Studio
Ctrl + Shift + B

# Or build via command line (if MSBuild is available)
msbuild NoNameTag.slnx /p:Configuration=Debug
```

### Output Location
- Debug build: `bin/Debug/net48/NoNameTag.dll`
- Release build: `bin/Release/net48/NoNameTag.dll`

### Deployment
Copy the compiled DLL to your server's `Rocket/Plugins` folder.

## Testing

### Manual Testing
1. Start the Unturned server with Rocket installed
2. Check console for plugin loading message:
   ```
   [loading] NoNameTag
   [timestamp] [Info] NoNameTag >> NoNameTag 1.0.0 has been loaded!
   ```
3. Connect as a player with configured permissions (e.g., `nonametag.vip`)
4. Verify name tag appears with prefix and colors
5. Test chat messages to see colored names
6. Test death messages for different death causes

### Configuration Testing
1. Edit `Rocket/Plugins/NoNameTag/NoNameTagConfiguration.xml`
2. Use `/nametag reload` in-game or console
3. Verify changes take effect immediately

## Development Notes

### Service Initialization Order
1. `PermissionService` - Loads permission configuration
2. `NameTagManager` - Depends on PermissionService
3. `DeathMessageService` - Depends on NameTagManager
4. `NameTagDisplayService` - Depends on NameTagManager

### Event Flow
1. **Player Connect** → ApplyDisplayEffect → ApplyNameTag (delayed)
2. **Player Chat** → FormatChatMessage → Broadcast with colored name
3. **Player Death** → FormatDeathMessage → Broadcast with colored names
4. **Player Disconnect** → RemoveDisplayEffect → RemoveNameTag → Clear cache

### Important Implementation Details

**Delayed Name Tag Application** (`NoNameTagPlugin.cs:152-167`)
- Waits for player channel initialization
- Checks `playerID.nickName` availability
- Prevents race conditions during player spawn

**Cache Management**
- Permission cache: 5-minute expiration
- Player effect cache: max 1000 entries
- Automatic cleanup of offline players

**Color Format**
- Uses Unity `<color=#RRGGBB>text</color>` format
- Hex colors can include or exclude `#` prefix
- Default color: `#FFFFFF` (white)

### Configuration Validation
- Hex colors validated with regex: `^#?[0-9A-Fa-f]{6}$`
- Permissions validated with regex: `^[a-zA-Z0-9_.]+$`
- Priority range: 0-1000
- Validation occurs on plugin load

## Common Tasks

### Adding a New Permission Group
1. Edit `NoNameTagConfiguration.xml`
2. Add new `<PermissionGroup>` entry with:
   - `permission` attribute: e.g., `"nonametag.admin"`
   - `priority` attribute: e.g., `50`
   - `<DisplayEffect>` with prefix, colors, suffix
3. Use `/nametag reload` to apply changes

### Modifying Death Messages
1. Edit `NoNameTagConfiguration.xml`
2. Update `<DeathMessage>` section with desired formats
3. Use placeholders: `{victim}`, `{killer}`
4. Use `/nametag reload` to apply changes

### Debugging
1. Enable `DebugMode: true` in configuration
2. Check console for detailed logs with categories:
   - `[PLUGIN]` - General plugin operations
   - `[PERM]` - Permission checks
   - `[TAG]` - Name tag operations
   - `[DEATH]` - Death message handling
   - `[CMD]` - Command execution
   - `[CONFIG]` - Configuration operations

## Dependencies

- **Rocket.Unturned** (v3.x) - Plugin framework
- **SDG.Unturned** - Unturned game assemblies
- **Steamworks** - Steam integration
- **UnityEngine** - Unity engine types

## File Structure

```
NoNameTag/
├── NoNameTagPlugin.cs          # Main plugin entry point
├── NoNameTagConfiguration.cs   # Configuration class
├── NoNameTag.csproj            # Project file
├── NoNameTag.slnx              # Solution file
├── README.md                   # User documentation
├── CLEANUP_REPORT.md           # Code cleanup history
├── Commands/
│   └── NameTagCommand.cs       # /nametag command handler
├── Models/
│   ├── DisplayEffectConfig.cs  # Visual effect configuration
│   ├── PermissionGroupConfig.cs # Permission group definition
│   └── DeathMessageConfig.cs   # Death message formats
├── Services/
│   ├── IPermissionService.cs
│   ├── INameTagManager.cs
│   ├── IDeathMessageService.cs
│   ├── INameTagDisplayService.cs
│   ├── PermissionService.cs
│   ├── NameTagManager.cs
│   ├── DeathMessageService.cs
│   └── NameTagDisplayService.cs
└── Utilities/
    ├── ConfigValidator.cs      # Configuration validation
    ├── Constants.cs            # Global constants
    ├── Logger.cs               # Structured logging
    └── NameFormatter.cs        # Name formatting utilities
```

## Configuration Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<NoNameTagConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Enabled>true</Enabled>
  <DebugMode>false</DebugMode>
  <DefaultNameColor>#FFFFFF</DefaultNameColor>
  <ApplyToChatMessages>true</ApplyToChatMessages>
  <ApplyToNameTags>true</ApplyToNameTags>
  <PriorityMode>HighestPriority</PriorityMode>
  <PermissionGroups>
    <PermissionGroup permission="nonametag.vip" priority="10">
      <DisplayEffect>
        <Prefix>[VIP] </Prefix>
        <PrefixColor>#FFD700</PrefixColor>
        <NameColor>#00FF00</NameColor>
        <Suffix></Suffix>
        <SuffixColor>#FFFFFF</SuffixColor>
      </DisplayEffect>
    </PermissionGroup>
    <PermissionGroup permission="nonametag.mvp" priority="20">
      <DisplayEffect>
        <Prefix>[MVP] </Prefix>
        <PrefixColor>#FF4500</PrefixColor>
        <NameColor>#00BFFF</NameColor>
        <Suffix> *</Suffix>
        <SuffixColor>#FF4500</SuffixColor>
      </DisplayEffect>
    </PermissionGroup>
  </PermissionGroups>
  <DeathMessage>
    <Enabled>true</Enabled>
    <Format>{killer} 击杀了 {victim}</Format>
    <SelfKillFormat>{victim} 自杀了</SelfKillFormat>
    <ZombieFormat>{victim} 被僵尸击杀</ZombieFormat>
    <!-- ... other death message formats ... -->
  </DeathMessage>
</NoNameTagConfiguration>
```

## Notes

- The plugin uses XML serialization for configuration
- All configuration changes require `/nametag reload` to take effect
- Permission checks are cached for 5 minutes to improve performance
- Name tags are applied with a 1-frame delay to ensure player initialization
- Original player names are stored and restored on disconnect
