# PhobiaModel - Enhanced 3D Model Management System

## Overview
PhobiaModel is a comprehensive 3D model management system that handles mesh loading, animations, material management, and rendering optimization with intelligent caching and runtime configuration.

## Key Features
- **Runtime Configuration**: Fully configurable model behavior without pre-setup
- **Smart Caching**: Automatic model and material caching with performance tracking
- **Animation Support**: Integrated animator support with state management
- **LOD Support**: Level-of-detail optimization for performance
- **Material Management**: Dynamic material swapping and property modification
- **Physics Integration**: Easy physics component management
- **Performance Optimized**: Object pooling and culling support

## Basic Usage

### Simple Model Creation
```csharp
// Create a basic 3D model
var model = PhobiaModel.Create(Vector3.zero, "Models/MyModel");

// Create with parent transform
var model = PhobiaModel.Create(Vector3.zero, "Models/Character", parentTransform);
```

### Specialized Model Types
```csharp
// Create a character model with animation support
var character = PhobiaModel.CreateCharacter(Vector3.zero, "Models/Player");

// Create a static prop (no animations, optimized)
var prop = PhobiaModel.CreateStatic(Vector3.zero, "Models/Rock");
```

### Custom Configuration
```csharp
// Create custom configuration
var config = PhobiaModel.ModelConfig.CreateDefault();
config.enableAnimations = true;
config.enableLOD = true;
config.cullingDistance = 200f;

var model = PhobiaModel.Create(Vector3.zero, "Models/Enemy", null, config);
```

## Configuration Options

### ModelConfig Properties
| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `enableCaching` | bool | Enable model caching | true |
| `autoDestroy` | bool | Auto-destroy when done | false |
| `enableLOD` | bool | Enable level-of-detail | false |
| `enableAnimations` | bool | Enable animation support | true |
| `animationSpeed` | float | Animation playback speed | 1.0f |
| `loopAnimations` | bool | Loop animations by default | true |
| `enableRootMotion` | bool | Enable root motion | false |
| `enableShadows` | bool | Cast shadows | true |
| `receiveShadows` | bool | Receive shadows | true |
| `cullingDistance` | float | Maximum render distance | 100f |

### Preset Configurations
```csharp
// Default configuration
var defaultConfig = ModelConfig.CreateDefault();

// Character-optimized configuration
var characterConfig = ModelConfig.CreateCharacterConfig();

// Static prop configuration
var staticConfig = ModelConfig.CreateStaticConfig();
```

## Animation System

### Playing Animations
```csharp
// Play animation by name
model.PlayAnimation("idle");

// Play with custom speed
model.PlayAnimation("run", 1.5f);

// Play with callback
model.PlayAnimation("attack", onComplete: () => {
    Debug.Log("Attack finished!");
});
```

### Animation Properties
```csharp
// Check animation state
bool isPlaying = model.IsAnimationPlaying;
string currentAnim = model.CurrentAnimation;

// Control animation
model.SetAnimationSpeed(2f);
model.StopAnimation();
model.PauseAnimation();
model.ResumeAnimation();
```

## Material Management

### Material Swapping
```csharp
// Swap material by index
model.SwapMaterial(0, newMaterial);

// Swap all materials
model.SwapAllMaterials(materialArray);

// Get current materials
Material[] materials = model.GetMaterials();
```

### Material Properties
```csharp
// Set material property
model.SetMaterialProperty("_Color", Color.red);
model.SetMaterialProperty("_MainTex", newTexture);

// Set property on specific material
model.SetMaterialProperty(0, "_Metallic", 0.8f);
```

### Visual Effects
```csharp
// Fade model in/out
model.FadeOut(2f);
model.FadeIn(1.5f);
model.FadeTo(0.5f, 1f);

// Highlight effect
model.SetHighlight(true, Color.yellow);
model.SetHighlight(false);
```

## Performance Features

### Caching System
```csharp
// Clear all cached models
PhobiaModel.ClearCache();

// Get cache performance stats
var (hits, misses, hitRate) = PhobiaModel.GetCacheStats();
Debug.Log($"Cache hit rate: {hitRate:P}");
```

### LOD Management
```csharp
// Enable LOD with custom distances
model.SetupLOD(new float[] { 10f, 50f, 100f });

// Update LOD settings
model.UpdateLODSettings(0, 15f); // Update LOD 0 distance
```

### Culling and Optimization
```csharp
// Set culling distance
model.SetCullingDistance(150f);

// Enable/disable rendering
model.SetVisible(false);
model.SetVisible(true);
```

## Physics Integration

### Basic Physics
```csharp
// Enable physics
model.EnablePhysics(true);

// Enable kinematic physics
model.EnablePhysics(true, isKinematic: true);

// Disable physics
model.EnablePhysics(false);
```

### Collision Detection
```csharp
// Add collision detection
model.EnableCollision(true);

// Set collision layer
model.SetCollisionLayer("Characters");
```

## Utility Methods

### Transform Operations
```csharp
// Get world bounds
Bounds bounds = model.GetWorldBounds();

// Get screen position
Vector2 screenPos = model.GetScreenPosition();

// Clone model
PhobiaModel clone = model.Clone();
```

### State Management
```csharp
// Check if model is loaded
bool isLoaded = model.IsLoaded;

// Check visibility
bool isVisible = model.IsVisible;

// Get mesh information
Mesh mesh = model.Mesh;
bool isSkinned = model.IsSkinned;
```

## Integration Examples

### Character Controller Integration
```csharp
public class Character : MonoBehaviour
{
    private PhobiaModel model;
    
    void Start()
    {
        // Create character model
        var config = PhobiaModel.ModelConfig.CreateCharacterConfig();
        config.enableRootMotion = true;
        
        model = PhobiaModel.Create(transform.position, "Models/Player", transform, config);
        
        // Set up animations
        model.PlayAnimation("idle");
    }
    
    public void Move(Vector3 direction)
    {
        if (direction.magnitude > 0.1f)
        {
            model.PlayAnimation("run");
        }
        else
        {
            model.PlayAnimation("idle");
        }
    }
}
```

### Dynamic Material System
```csharp
public class MaterialChanger : MonoBehaviour
{
    private PhobiaModel model;
    
    void Start()
    {
        model = GetComponent<PhobiaModel>();
    }
    
    public void ChangeMaterial(Material newMaterial)
    {
        // Fade out, change material, fade in
        model.FadeOut(0.5f);
        
        StartCoroutine(DelayedMaterialChange(newMaterial));
    }
    
    IEnumerator DelayedMaterialChange(Material newMaterial)
    {
        yield return new WaitForSeconds(0.5f);
        model.SwapMaterial(0, newMaterial);
        model.FadeIn(0.5f);
    }
}
```

## Best Practices

1. **Use Appropriate Configs**: Choose the right configuration preset for your model type
2. **Enable Caching**: Keep caching enabled for frequently used models
3. **LOD for Performance**: Use LOD for models that appear at various distances
4. **Material Pooling**: Reuse materials when possible to reduce memory usage
5. **Culling Distance**: Set appropriate culling distances based on model importance
6. **Animation Optimization**: Disable animations for static objects

## Troubleshooting

### Common Issues

**Model not appearing:**
- Check that the model path is correct
- Verify the model is in the Resources folder
- Ensure materials are assigned

**Animation not playing:**
- Check that animations are enabled in config
- Verify animator controller is assigned
- Ensure animation names are correct

**Performance issues:**
- Enable LOD for distant objects
- Use static config for non-animated models
- Adjust culling distances
- Monitor cache hit rates

### Debug Information
```csharp
// Log model state
Debug.Log($"Model loaded: {model.IsLoaded}");
Debug.Log($"Animation playing: {model.IsAnimationPlaying}");
Debug.Log($"Current animation: {model.CurrentAnimation}");

// Cache statistics
var stats = PhobiaModel.GetCacheStats();
Debug.Log($"Cache performance: {stats.hitRate:P}");
```

The PhobiaModel system provides a robust foundation for 3D model management with extensive customization and optimization features.
