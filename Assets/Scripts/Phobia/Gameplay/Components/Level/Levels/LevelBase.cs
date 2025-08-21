using UnityEngine;
using System.Collections.Generic;

namespace Phobia.Gameplay.Components.Level.Levels
{
	public abstract class LevelBase : MonoBehaviour
	{
		[HideInInspector] public PlayState playState;
		protected Dictionary<string, LevelProp> props = new Dictionary<string, LevelProp>();
		
		protected virtual void Awake()
		{
			if (playState == null && PlayState.Instance != null)
			{
				playState = PlayState.Instance;
			}
		}

		public virtual void Initialize(PlayState playStateRef)
		{
			playState = playStateRef;
			Debug.Log($"[LevelBase] Initialized with PlayState: {playStateRef.name}");
		}

	public virtual void Create() { }
	public virtual void Update() { }
	public virtual void Reset() { }
	public virtual void TriggerEvent(string eventType, object parameters) { }
	public virtual void ToggleWatcher() { }

	public void AddProp(LevelProp prop, string name)
	{
		if (prop == null)
		{
			Debug.LogError("Tried to add null prop to level");
			return;
		}

		props[name] = prop;
		prop.transform.SetParent(transform);
		Debug.Log($"[LevelBase] Added prop: {name}");
	}

	public LevelProp GetProp(string name)
	{
		if (props.ContainsKey(name))
		{
			return props[name];
		}
		Debug.LogWarning($"[LevelBase] Prop '{name}' not found");
		return null;
	}
	}
}
