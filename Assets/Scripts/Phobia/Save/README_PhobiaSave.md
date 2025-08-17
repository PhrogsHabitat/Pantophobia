# Pantophobia Save System

A modular, FNF-inspired save system for Unity that uses Unity's built-in utilities for maximum compatibility and simplicity.

## Architecture Overview

The save system is built around a base class that provides core functionality, with specialized save classes for different types of data:

- **`PhobiaSaveBase`** - Base class providing core save/load infrastructure
- **`PhobiaSave`** - Main game save (progress, settings, player data)
- **`PhobiaControls`** - Input controls and keybindings
- **`PhobiaClientPrefs`** - Lightweight client preferences using PlayerPrefs

## Key Features

- 🎯 **FNF-Inspired Architecture** - Singleton pattern, property-based access, flush() method
- 🔧 **Unity Native** - Uses JsonUtility and PlayerPrefs for maximum compatibility
- 📦 **Modular Design** - Easy to extend for specialized save types
- 🚀 **Simple API** - Clean, easy-to-use methods following FNF patterns
- 😄 **Casual Style** - Humorous internal method names following Phobia conventions

## Quick Start

### Basic Usage

```csharp
// Main game save - automatically loads existing data or creates new
var save = PhobiaSave.Instance;

// Complete a level
save.CompleteLevel("testLevel", score: 1000, time: 120.5f, deaths: 2);

// Check if level is unlocked
if (save.Progress.IsLevelUnlocked("nextLevel"))
{
    // Load the level
}

// Apply all saved settings to Unity
save.ApplyAllSettings();
```

### Controls Save

```csharp
// Controls save - separate from main save
var controls = PhobiaControls.Instance;

// Set a key binding
controls.SetKeyBinding("JUMP", "<Keyboard>/space");

// Get a key binding
string jumpKey = controls.GetKeyBinding("JUMP");

// Apply to input system
controls.ApplyToPhobiaInput();
```

### Client Preferences

```csharp
// Client preferences - uses PlayerPrefs for lightweight storage
var prefs = PhobiaClientPrefs.Instance;

// Set preferences (auto-saves)
prefs.ShowFPS = true;
prefs.LastSelectedLevel = "bossLevel";

// Get preferences
bool showFps = prefs.ShowFPS;
```

## Creating Custom Save Classes

To create your own specialized save class, inherit from `PhobiaSaveBase`:

```csharp
public class PhobiaCustomSave : PhobiaSaveBase
{
    // 1. Define your data structure
    [Serializable]
    public class CustomData
    {
        public string customField = "default";
        // ... other fields
    }

    // 2. Singleton pattern
    private static PhobiaCustomSave _instance;
    public static PhobiaCustomSave Instance => _instance ??= new PhobiaCustomSave();

    // 3. Constructor
    private PhobiaCustomSave() : base("CustomSave.json", "1.0.0")
    {
        // Initialize your data
    }

    // 4. Override base methods
    protected override object GetDataForSaving() => yourData;
    protected override object LoadDataFromDisk()
    {
        // Load and return your data structure
    }
}
```

## Data Structures

### Main Save Data
- **ProgressData** - Level completion, scores, unlocks
- **OptionsData** - Audio, graphics, input settings
- **PlayerData** - Player name, favorites, achievements

### Storage Methods
- **File-based** - Uses JsonUtility for complex data (main save, controls)
- **PlayerPrefs** - Uses Unity's PlayerPrefs for simple settings (client prefs)

## FNF-Style Features

- **Singleton Access** - `PhobiaSave.Instance`
- **Property Access** - `save.Progress`, `save.Options`, `save.Player`
- **Flush Method** - `save.Flush()` to save immediately
- **Auto-Save** - Automatically saves when important changes are made

## Integration

The save system integrates seamlessly with existing Pantophobia systems:

- **PhobiaInput** - Saves and applies input settings
- **PlayState** - Initializes with saved level data
- **Unity Settings** - Applies graphics, audio, and other Unity settings

## Casual Naming Conventions

Following Phobia project style, internal methods use humorous names:
- `SwagShit()` methods for non-critical internal functions
- Casual comments and debug messages
- Fun but professional external API

## File Locations

Save files are stored in Unity's `Application.persistentDataPath`:
- `PhobiaSave.json` - Main game save
- `PhobiaControls.json` - Input controls
- PlayerPrefs registry - Client preferences

## Error Handling

The system includes robust error handling:
- Automatic fallback to defaults if save files are corrupted
- Graceful degradation when files are missing
- Detailed logging for debugging

## Extending the System

This base system is designed to be extended. You can easily create:
- **PhobiaStats** - Detailed gameplay statistics
- **PhobiaAchievements** - Achievement tracking
- **PhobiaSettings** - Advanced game settings
- **PhobiaProfiles** - Multiple player profiles

Each specialized save class can use different storage methods (files, PlayerPrefs, or even cloud saves) while maintaining a consistent API.
