# Conductor - Enhanced Music Timing System

## Overview

The Conductor is a sophisticated music timing system that provides precise beat detection, time signature management, and musical event handling for rhythm-based games and applications.

## Key Features

- **Precise Timing**: High-precision beat, step, and measure detection
- **Runtime Configuration**: Fully configurable timing parameters and offsets
- **Time Signature Support**: Dynamic time signature changes during playback
- **Event System**: Comprehensive callbacks for musical events
- **Performance Monitoring**: Built-in timing accuracy tracking
- **Offset Management**: Multiple offset types for perfect synchronization

## Basic Usage

### Simple Song Mapping

```csharp
// Get conductor instance
var conductor = Conductor.Instance;

// Create and map a song
var song = PhobiaSound.Create(audioClip, 0.8f, true);
conductor.MapSong(song, 120f); // 120 BPM

// Subscribe to events
conductor.OnBeatHit += () => Debug.Log("Beat!");
conductor.OnMeasureHit += () => Debug.Log("Measure!");
```

### Advanced Configuration

```csharp
// Create custom configuration
var config = Conductor.ConductorConfig.CreateHighPrecisionConfig();
config.instrumentalOffset = -50f; // ms
config.inputOffset = 20f; // ms

// Map song with configuration
conductor.MapSong(song, 140f, config);
```

### Time Changes Support

```csharp
// Create time changes for complex songs
var timeChanges = new List<Conductor.TimeChange>
{
    Conductor.TimeChange.Create(0f, 120f, 4, 4),      // Start: 120 BPM, 4/4
    Conductor.TimeChange.Create(30000f, 140f, 4, 4),  // 30s: 140 BPM, 4/4
    Conductor.TimeChange.Create(60000f, 160f, 3, 4)   // 60s: 160 BPM, 3/4
};

conductor.MapSong(song, 120f, timeChanges);
```

## Configuration Options

### ConductorConfig Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `defaultBpm` | float | Default BPM for songs | 120f |
| `timeSignatureNumerator` | int | Time signature numerator | 4 |
| `timeSignatureDenominator` | int | Time signature denominator | 4 |
| `instrumentalOffset` | float | Instrumental sync offset (ms) | 0f |
| `formatOffset` | float | Audio format offset (ms) | 0f |
| `inputOffset` | float | Input lag offset (ms) | 0f |
| `audioVisualOffset` | float | Audio-visual sync offset (ms) | 0f |
| `enableHighPrecision` | bool | Enable high-precision timing | true |
| `precisionThreshold` | float | Precision threshold (ms) | 1f |
| `enableEventCaching` | bool | Cache events for performance | true |

### Preset Configurations

```csharp
// Default configuration
var defaultConfig = ConductorConfig.CreateDefault();

// High-precision for rhythm games
var precisionConfig = ConductorConfig.CreateHighPrecisionConfig();

// Performance-optimized
var performanceConfig = ConductorConfig.CreatePerformanceConfig();
```

## Event System

### Available Events

```csharp
// Basic timing events
conductor.OnStepHit += () => { /* 16th note */ };
conductor.OnBeatHit += () => { /* Quarter note */ };
conductor.OnMeasureHit += () => { /* Full measure (Whole Note) */ };

// Advanced events
conductor.OnBpmChanged += (newBpm) => Debug.Log($"BPM: {newBpm}");
conductor.OnTimeChangeHit += (timeChange) => Debug.Log($"Time change: {timeChange.bpm}");
```

### Event Timing

- **Step**: 16th note (4 steps per beat)
- **Beat**: Quarter note (based on BPM)
- **Measure**: Full measure (based on time signature)

## Timing Properties

### Current Position

```csharp
// Get current musical position
int currentStep = conductor.currentStep;
int currentBeat = conductor.currentBeat;
int currentMeasure = conductor.currentMeasure;

// Get precise timing
float songPosition = conductor.songPosition; // milliseconds
float bpm = conductor.bpm;
```

### Offset Management

```csharp
// Individual offsets
conductor.instrumentalOffset = -30f;  // Instrumental sync
conductor.inputOffset = 15f;          // Input lag compensation
conductor.audioVisualOffset = 10f;    // A/V sync

// Combined offset (read-only)
float totalOffset = conductor.combinedOffset;
```

## Advanced Features

### Runtime Time Changes

```csharp
// Add time change during playback
conductor.AddTimeChange(45000f, 130f, 4, 4); // At 45s, change to 130 BPM
```

### Performance Monitoring

```csharp
// Get timing accuracy
float accuracy = conductor.GetTimingAccuracy();
Debug.Log($"Timing accuracy: {accuracy:P}");
```

### Configuration Updates

```csharp
// Update configuration at runtime
var newConfig = conductor.Config;
newConfig.enableHighPrecision = false;
conductor.UpdateConfig(newConfig);
```

## Integration Examples

### Rhythm Game Integration

```csharp
public class RhythmGame : MonoBehaviour
{
    private Conductor conductor;
    
    void Start()
    {
        conductor = Conductor.Instance;
        
        // Configure for rhythm game
        var config = Conductor.ConductorConfig.CreateHighPrecisionConfig();
        config.inputOffset = GetInputLatency(); // Measure input lag
        
        // Load song with time changes
        var song = LoadSong("MySong");
        var timeChanges = LoadTimeChanges("MySong");
        conductor.MapSong(song, 128f, config, timeChanges);
        
        // Subscribe to events
        conductor.OnBeatHit += OnBeat;
        conductor.OnStepHit += OnStep;
    }
    
    void OnBeat()
    {
        // Visual beat indicator
        beatIndicator.Pulse();
    }
    
    void OnStep()
    {
        // Check for player input timing
        CheckInputTiming();
    }
}
```

### Music Visualization

```csharp
public class MusicVisualizer : MonoBehaviour
{
    void Start()
    {
        var conductor = Conductor.Instance;
        
        conductor.OnBeatHit += () => {
            // Trigger visual effects on beat
            particleSystem.Emit(10);
        };
        
        conductor.OnMeasureHit += () => {
            // Larger effect on measure
            cameraShake.Shake(0.5f);
        };
    }
}
```

## Best Practices

1. **Offset Calibration**: Measure and set appropriate offsets for your audio setup
2. **Time Change Preparation**: Pre-calculate time changes for complex songs
3. **Event Optimization**: Unsubscribe from unused events to improve performance
4. **Configuration Presets**: Use appropriate config presets for different scenarios
5. **Precision vs Performance**: Balance precision needs with performance requirements

## Troubleshooting

### Common Issues

**Events not firing:**

- Ensure the song is playing and mapped correctly
- Check that offsets aren't causing timing issues
- Verify time signature settings

**Timing drift:**

- Enable high-precision mode
- Adjust precision threshold
- Check for audio format issues

**Performance issues:**

- Use performance config preset
- Disable event caching if not needed
- Reduce precision threshold

### Debug Information

```csharp
// Log current timing state
Debug.Log($"Position: {conductor.songPosition}ms");
Debug.Log($"Beat: {conductor.currentBeat}, Step: {conductor.currentStep}");
Debug.Log($"BPM: {conductor.bpm}, Offset: {conductor.combinedOffset}");
```

The Conductor system provides a robust foundation for any music-timing dependent application with precise control and extensive customization options.
