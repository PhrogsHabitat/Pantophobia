using Phobia.Audio;
using Phobia.Graphics;
using Phobia.Input;
using UnityEngine;

namespace Phobia.Gameplay.Components.Level.Levels
{
    public class TestLevel : LevelBase
    {
        private LevelProp testProp;
        private LevelProp background;
        private PhobiaModel sillyGirl;
        private PhobiaModel spinner;
        private PhobiaSound _mainMusic;
        private const float SOUND_RADIUS = 15f;
        private bool _musicTied = false;

        public override void Initialize(PlayState playStateRef)
        {
            base.Initialize(playStateRef);
            Debug.Log("[TEST] TestLevel initialized via SceneRegistry");
        }

        public override void Create()
        {
            base.Create();
            Debug.Log($"[TestLevel] Create called on GameObject: {gameObject.name}");
            Debug.Log($"[TestLevel] GameObject active: {gameObject.activeSelf}");
            Debug.Log($"[TestLevel] Component enabled: {this.enabled}");

            // Create background
            background = LevelProp.Create("Background", Vector2.zero);
            background.MakeSolidColor(new Vector2(20, 20), new Color(0.1f, 0.1f, 0.2f));
            AddProp(background, "background");

            // Create test prop
            testProp = LevelProp.Create("TestProp", Vector2.zero);
            testProp.LoadTexture(Paths.levelImage("testLevel", "TestTexture"));
            testProp.Scale = new Vector2(2, 2);
            AddProp(testProp, "test_prop");

            // Create SillyGirl model
            sillyGirl = PhobiaModel.Create(Vector3.zero, "Models/testLevel/SillyGirl/SillyGirlPrefab");
            spinner = PhobiaModel.Create(Vector3.zero, "Models/testLevel/Spinner/SpinnerPrefab");

            // Get reference to the main music from PlayState
            _mainMusic = PlayState.Instance.heartBeatMusic;

            // Play animations here
            // sillyGirl.PlayAnimation("rig_|rig_Action");
        }

        public override void Update()
        {
			Debug.Log("This state is gay");
            // Example update logic
			if (!_musicTied && sillyGirl != null && _mainMusic != null)
			{
				TieMusicToSillyGirl();
				_musicTied = true;
			}

            if (Controls.ACCEPT || Controls.isPressed("back"))
            {
                Debug.Log("[TestLevel] Accept or Back pressed");
            }
            base.Update();
        }

        private void TieMusicToSillyGirl()
        {
            if (_mainMusic == null || sillyGirl == null)
            {
                return;
            }

            _mainMusic.TieTo(sillyGirl.transform);
            _mainMusic.SetDistanceParams(0.1f, SOUND_RADIUS);
            _mainMusic.spatialEnabled = true;
            Debug.Log($"Main music tied to SillyGirl with radius {SOUND_RADIUS}");
        }

        public override void TriggerEvent(string eventType, object parameters)
        {
            Debug.Log($"[TEST] Event triggered: {eventType}");
        }

        public override void Reset()
        {
            foreach (var prop in props.Values)
            {
                if (prop != background && prop != testProp)
                {
                    prop.Kill();
                }
            }
        }

        public override void ToggleWatcher()
        {
            Debug.Log("[TEST] ToggleWatcher called - no watcher in TestLevel");
        }
    }
}
