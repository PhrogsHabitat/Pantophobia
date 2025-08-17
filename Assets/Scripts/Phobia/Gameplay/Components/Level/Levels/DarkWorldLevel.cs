namespace Phobia.Gameplay.Components.Level.Levels
{
	using UnityEngine;
	using Phobia.Gameplay.Components.Level;
	using System.Collections.Generic;

	public class DarkWorldLevel : LevelBase
	{

		public LevelProp bench;

		public override void Create()
		{
			// Create background layers
			CreateBackgroundLayers();

			// Create props
			CreateBench();
		}

		private void CreateBackgroundLayers()
		{
			// Implementation to create background sprites
		}

		public override void UpdateLevel(float elapsed)
		{
			// Update demon logic, timers, etc.
		}

		public override void TriggerEvent(string eventType, object parameters)
		{
			switch (eventType)
			{
				case "spawnDemon":
					SpawnDemon((string)parameters);
					break;
				case "toggleWatcher":
					ToggleWatcher();
					break;
			}
		}

		public void CreateBench()
		{
			bench = LevelProp.Create("Bench", new Vector2(-761, -99));
			bench.LoadSparrowAtlas(
				$"Images/darkWorld/bench_atlas", // Atlas path
				"BenchFriends"                   // Sprite name
			);
			bench.Scale = new Vector2(0.15f, 0.15f);
			bench.PlayAnimation("idle");
			AddProp(bench, "bench");
		}

		private void SpawnDemon(string demonId)
		{
			Debug.Log($"Spawning demon: {demonId}");
		}

		public override void ToggleWatcher()
		{
			Debug.Log("Toggling watcher");
		}
	}
}
