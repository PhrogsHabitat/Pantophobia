# PhobiaSprite - Enhanced Sprite Management System

## Overview
PhobiaSprite is a powerful, runtime-configurable sprite management system that handles texture loading, animations, and visual effects with intelligent caching and performance optimization.

## Key Features
- **Runtime Configuration**: Fully configurable without pre-setup requirements
- **Smart Caching**: Automatic sprite and atlas caching with performance tracking
- **Animation Support**: Integrated animator support with callback system
- **Visual Effects**: Built-in fading, color tweening, and alpha management
- **Performance Optimized**: Object pooling and cache management
- **Modular Design**: Extensible configuration system for different use cases

## Basic Usage

### Simple Sprite Creation
```csharp
// Create a basic sprite
var sprite = PhobiaSprite.Create(Vector3.zero, "Sprites/MySprite");

// Create with custom configuration
var config = PhobiaSprite.SpriteConfig.CreateAnimatedConfig();
var animatedSprite = PhobiaSprite.Create(Vector3.zero, "Sprites/Character", config);
```

### Solid Color Sprites
```csharp
// Create a solid color rectangle
var colorSprite = PhobiaSprite.CreateSolidColor(
    Vector3.zero, 
    new Vector2(100, 50), 
    Color.red
);
```

### Sparrow Atlas Support
```csharp
// Load from Sparrow atlas for animations
var sparrowSprite = PhobiaSprite.CreateSparrow(
    Vector3.zero, 
    "Atlases/CharacterAtlas", 
    "idle_001"
);
```

## Configuration Options

### SpriteConfig Properties
| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `enableCaching` | bool | Enable sprite caching | true |
| `autoDestroy` | bool | Auto-destroy when done | false |
| `defaultAlpha` | float | Initial alpha value | 1.0f |
| `tintColor` | Color | Base tint color | Color.white |
| `enableAnimations` | bool | Enable animation support | true |
| `animationSpeed` | float | Animation playback speed | 1.0f |
| `enableFading` | bool | Enable fade effects | true |
| `fadeSpeed` | float | Fade transition speed | 1.0f |
| `maxCacheSize` | int | Maximum cached sprites | 100 |

### Preset Configurations
```csharp
// Default configuration
var defaultConfig = SpriteConfig.CreateDefault();

// UI-optimized configuration
var uiConfig = SpriteConfig.CreateUIConfig();

// Animation-optimized configuration
var animConfig = SpriteConfig.CreateAnimatedConfig();
```

## Animation System

### Playing Animations
```csharp
// Play animation by name
sprite.PlayAnimation("idle");

// Set animation speed
sprite.SetAnimationSpeed(1.5f);

// Add completion callback
sprite.AddAnimationCompleteCallback("attack", () => {
    Debug.Log("Attack animation completed!");
});
```

### Animation Properties
```csharp
// Check if animation is playing
bool isPlaying = sprite.IsAnimationPlaying;

// Get current animation name
string currentAnim = sprite.CurrentAnimation;

// Stop current animation
sprite.StopAnimation();
```

## Visual Effects

### Fading Effects
```csharp
// Fade out over 2 seconds
sprite.FadeOut(2f);

// Fade in over 1 second
sprite.FadeIn(1f);

// Fade to specific alpha
sprite.FadeTo(0.5f, 1.5f);
```

### Color Management
```csharp
// Set color directly
sprite.SetColor(Color.red);

// Set alpha only
sprite.SetAlpha(0.8f);

// Tween to new color
sprite.TweenToColor(Color.blue, 2f);

// Reset to base color
sprite.ResetColor();
```

## Performance Features

### Cache Management
```csharp
// Clear all cached sprites
PhobiaSprite.ClearCache();

// Get cache performance stats
var (hits, misses, hitRate) = PhobiaSprite.GetCacheStats();
Debug.Log($"Cache hit rate: {hitRate:P}");
```

### Runtime Configuration Updates
```csharp
// Update configuration at runtime
var newConfig = SpriteConfig.CreateUIConfig();
newConfig.enableAnimations = false;
sprite.UpdateConfig(newConfig);
```

## Utility Methods

### Position and Cloning
```csharp
// Get screen position
Vector2 screenPos = sprite.GetScreenPosition();

// Clone sprite with same configuration
PhobiaSprite clone = sprite.Clone();

// Check visibility
bool isVisible = sprite.IsVisible;
```

## Integration Notes

### With Other Phobia Systems
- **PhobiaCamera**: Automatically works with camera positioning
- **LevelProp**: Can be used as the visual component for level props
- **PhobiaModel**: Integrates for 2D sprite-based models

### Performance Considerations
- Enable caching for frequently used sprites
- Use object pooling for UI elements
- Consider disabling animations for static UI elements
- Monitor cache hit rates for optimization

### Best Practices
1. Use appropriate configuration presets for different sprite types
2. Enable caching for sprites that will be reused
3. Use solid color sprites for simple UI elements
4. Implement proper cleanup when destroying sprites
5. Monitor performance with cache statistics

## Error Handling
The system includes comprehensive error handling for:
- Missing sprite files
- Invalid atlas references
- Null configuration parameters
- Animation controller issues

All errors are logged with descriptive messages to aid in debugging.
