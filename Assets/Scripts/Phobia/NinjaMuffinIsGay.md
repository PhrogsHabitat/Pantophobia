# Pantophobia Systems Overview

## Input System

### Files
- **PhobiaInput.cs** - Low-level Unity Input System wrapper
- **PlayerControls.cs** - Unified controls interface (contains `Controls` class)
- **PhobiaControls.md** - Complete documentation

### Usage
```csharp
// Simple input checking anywhere in the codebase
if (Controls.isPressed("accept")) {
    HandleAccept();
}

// Add runtime actions
Controls.AddAction("level_special", "<Keyboard>/q");

// Convenience properties
if (Controls.UI_UP) { MoveUp(); }
```

### Key Features
- Single unified interface for all input
- No imports required - available throughout Phobia namespace
- Runtime action creation for level-specific controls
- Automatic initialization through Main.cs
- Built on Unity's native Input System

## Save System

### Files
- **PhobiaSave.cs** - Main save system with base class architecture

### Features
- Unity JsonUtility-based serialization
- Extensible base class for specialized save types
- Automatic initialization and settings application
- Progress tracking, options storage, and player data

### Usage
```csharp
// Access save data
var save = PhobiaSave.Instance;
save.CompleteLevel("levelName", score: 1000);

// Apply saved settings
save.ApplyAllSettings();
```

## Initialization

### Main.cs
Handles automatic system initialization:
1. Save systems initialization
2. Controls system setup with default actions
3. Registry initialization
4. Asset preloading
5. PlayState setup with saved data

### Startup Flow
```
PhobiaApplication.Awake()
    ↓
Main.Initialize()
    ↓
1. InitializeSaveSystems() - Load save data
2. InitializeInputSystem() - Setup Controls
3. InitializeRegistries() - Level registry, etc.
4. Initialize PlayState with saved data
5. Apply all saved settings
```

## Architecture Principles

### Simplicity
- Single file solutions where possible
- Clear, descriptive APIs
- Minimal configuration required

### Unity Integration
- Built on Unity's native systems
- Uses Unity's recommended patterns
- Maximum compatibility across platforms

### Extensibility
- Base classes for easy extension
- Runtime configuration support
- Modular design for specialized needs

### Clean Code
- Comprehensive documentation
- Consistent naming conventions
- Clear separation of concerns

## Best Practices

### Input Handling
- Use `Controls.isPressed()` for most game actions
- Add level-specific actions in `InitLevelSpecifics()`
- Check `Controls.IsReady` before adding custom actions
- Use convenience properties for common actions

### Save System
- Call `SaveToDisk()` after making important changes
- Use the base class architecture for specialized save types
- Apply settings through the provided integration methods

### General
- Follow the established patterns in existing code
- Use descriptive names for actions and variables
- Document public APIs and complex logic
- Test thoroughly across different scenarios

This system provides a solid foundation for Pantophobia's core functionality while maintaining flexibility for future expansion and customization.
