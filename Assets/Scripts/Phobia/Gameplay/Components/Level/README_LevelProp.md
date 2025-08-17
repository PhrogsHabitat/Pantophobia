# LevelProp - Enhanced Level Prop Management System

## Overview

LevelProp is a comprehensive prop management system for level objects that provides sprite-based rendering, animations, state management, and interaction handling with runtime configuration support.

## Key Features

- **Runtime Configuration**: Fully configurable prop behavior without pre-setup
- **Sprite Integration**: Built-in PhobiaSprite integration for rendering
- **State Management**: Advanced state tracking with events
- **Animation Support**: Integrated animation system with callbacks
- **Interaction System**: Configurable interaction handling
- **Effect System**: Fade effects and visual enhancements
- **Object Pooling**: Performance optimization support

## Basic Usage

### Simple Prop Creation

```csharp
// Create a basic prop
var prop = LevelProp.Create("MyProp", new Vector2(100, 50));

// Load texture
prop.LoadTexture("Textures/MyTexture");

// Set properties
prop.Scale = new Vector2(2f, 2f);
prop.Position = new Vector2(200, 100);
```

### Specialized Prop Types

```csharp
// Create an interactive prop
var interactiveProp = LevelProp.CreateInteractive("Button", Vector2.zero);

// Create a background prop (optimized)
var backgroundProp = LevelProp.CreateBackground("Background", Vector2.zero);

// Create prop with texture in one call
var texturedProp = LevelProp.CreateWithTexture("Sign", Vector2.zero, "Textures/Sign");

// Create solid color prop
var colorProp = LevelProp.CreateSolidColor("Block", Vector2.zero, new Vector2(50, 50), Color.red);
```

### Custom Configuration

```csharp
// Create custom configuration
var config = new LevelProp.PropConfig
{
    enableInteraction = true,
    enableAnimations = true,
    enableEffects = true,
    autoReviveOnEnable = true
};

var customProp = LevelProp.Create("CustomProp", Vector2.zero, config);
```

## Configuration Options

### PropConfig Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `propName` | string | Name of the prop | "" |
| `autoInitialize` | bool | Auto-initialize on start | true |
| `enableInteraction` | bool | Enable interaction system | false |
| `enableAnimations` | bool | Enable animation support | true |
| `enableEffects` | bool | Enable visual effects | true |
| `enableFading` | bool | Enable fade effects | true |
| `enableStateTracking` | bool | Track prop state | true |
| `autoReviveOnEnable` | bool | Auto-revive when enabled | false |
| `defaultAlpha` | float | Default alpha value | 1.0f |
| `useObjectPooling` | bool | Use object pooling | false |

### Preset Configurations

```csharp
// Default configuration
var defaultConfig = PropConfig.CreateDefault();

// Interactive prop configuration
var interactiveConfig = PropConfig.CreateInteractiveConfig();

// Background prop configuration (optimized)
var backgroundConfig = PropConfig.CreateBackgroundConfig();
```

## Animation System

### Playing Animations

```csharp
// Play animation
prop.PlayAnimation("idle");

// Play animation with callback
prop.PlayAnimation("attack", () => {
    Debug.Log("Attack animation completed!");
});

// Check animation state
bool isAnimating = prop.IsAnimating;
string currentAnim = prop.CurrentAnimation;
```

### Animation Events

```csharp
// Subscribe to animation completion
prop.OnAnimationComplete += (prop, animName) => {
    Debug.Log($"Animation {animName} completed on {prop.propName}");
};
```

## Visual Effects

### Fade Effects

```csharp
// Fade out over 2 seconds
prop.FadeOut(2f);

// Fade in over 1 second
prop.FadeIn(1f);

// Fade with callback
prop.FadeOut(1.5f, () => {
    Debug.Log("Fade out completed!");
});

// Set alpha directly
prop.SetAlpha(0.5f);
```

### Color Management

```csharp
// Set color
prop.SetColor(Color.red);

// Reset to original color
prop.sprite.ResetColor();
```

## State Management

### Basic State Operations

```csharp
// Check state
bool isReady = prop.isReady;
bool isDying = prop.isDying;
bool isVisible = prop.IsVisible;

// Revive prop
prop.Revive();

// Kill prop
prop.Kill();
```

### State Events

```csharp
// Subscribe to state events
prop.OnPropReady += (prop) => Debug.Log($"{prop.propName} is ready!");
prop.OnPropDestroyed += (prop) => Debug.Log($"{prop.propName} was destroyed!");
```

## Interaction System

### Basic Interaction

```csharp
// Enable interaction in config
var config = PropConfig.CreateInteractiveConfig();
var interactiveProp = LevelProp.Create("Button", Vector2.zero, config);

// Subscribe to interaction events
interactiveProp.OnPropInteracted += (prop) => {
    Debug.Log($"Player interacted with {prop.propName}!");
};

// Trigger interaction
interactiveProp.Interact();
```

### Advanced Interaction

```csharp
public class InteractiveButton : MonoBehaviour
{
    private LevelProp prop;
    
    void Start()
    {
        prop = GetComponent<LevelProp>();
        prop.OnPropInteracted += OnButtonPressed;
    }
    
    void OnButtonPressed(LevelProp button)
    {
        button.PlayAnimation("press", () => {
            // Button press completed
            TriggerButtonAction();
        });
    }
}
```

## Transform Operations

### Position and Scale

```csharp
// Set position
prop.Position = new Vector2(100, 200);

// Set scale
prop.Scale = new Vector2(1.5f, 1.5f);

// Set rotation
prop.Rotation = 45f; // degrees

// Get screen position
Vector2 screenPos = prop.GetScreenPosition();
```

## Integration with Level System

### Adding Props to Levels

```csharp
public class MyLevel : LevelBase
{
    public override void Create()
    {
        // Create background
        var background = LevelProp.CreateBackground("Background", Vector2.zero);
        background.MakeSolidColor(new Vector2(1920, 1080), Color.blue);
        AddProp(background, "background");
        
        // Create interactive object
        var chest = LevelProp.CreateInteractive("Chest", new Vector2(500, 300));
        chest.LoadSparrowAtlas("Atlases/ChestAtlas", "chest_closed");
        chest.OnPropInteracted += OnChestOpened;
        AddProp(chest, "treasure_chest");
    }
    
    void OnChestOpened(LevelProp chest)
    {
        chest.PlayAnimation("open", () => {
            // Give player treasure
            GiveTreasure();
        });
    }
}
```

### Prop Management

```csharp
// Get prop from level
var chest = GetProp("treasure_chest");

// Update prop configuration
var newConfig = chest.Config;
newConfig.enableEffects = false;
chest.UpdateConfig(newConfig);

// Clone prop
var chestCopy = chest.Clone();
```

## Performance Optimization

### Object Pooling

```csharp
// Enable object pooling for frequently created/destroyed props
var config = new PropConfig
{
    useObjectPooling = true,
    enableCaching = true
};

var pooledProp = LevelProp.Create("PooledProp", Vector2.zero, config);
```

### Background Props

```csharp
// Use background config for static decorative elements
var decorations = new List<LevelProp>();
for (int i = 0; i < 100; i++)
{
    var decoration = LevelProp.CreateBackground($"Decoration_{i}", RandomPosition());
    decoration.LoadTexture("Textures/Decoration");
    decorations.Add(decoration);
}
```

## Best Practices

1. **Use Appropriate Configs**: Choose the right configuration preset for your prop type
2. **Enable Pooling**: Use object pooling for frequently spawned props
3. **Disable Unused Features**: Turn off animations/effects for static props
4. **Event Cleanup**: Unsubscribe from events when props are destroyed
5. **State Management**: Use the built-in state system for complex prop behaviors

## Troubleshooting

### Common Issues

**Prop not appearing:**

- Check that the texture path is correct
- Verify the prop is added to the level properly
- Ensure the prop is not killed or has zero alpha

**Animations not playing:**

- Check that animations are enabled in config
- Verify the animation name is correct
- Ensure the sprite has an animator controller

**Interaction not working:**

- Enable interaction in the prop config
- Subscribe to the OnPropInteracted event
- Check that the prop is ready and not dying

### Debug Information

```csharp
// Log prop state
Debug.Log($"Prop: {prop.propName}");
Debug.Log($"Ready: {prop.isReady}, Dying: {prop.isDying}");
Debug.Log($"Visible: {prop.IsVisible}, Animating: {prop.IsAnimating}");
Debug.Log($"Position: {prop.Position}, Scale: {prop.Scale}");
```

The LevelProp system provides a robust foundation for level object management with extensive customization and optimization features.
