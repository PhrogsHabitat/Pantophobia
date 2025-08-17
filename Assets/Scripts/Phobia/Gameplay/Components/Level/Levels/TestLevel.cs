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
            Debug.Log("[TEST] TestLevel.Create() called");

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
        }

        public override void InitLevelSpecifics()
        {
            Debug.Log("[TEST] Level specifics initialized");
        }

        public override void UpdateLevel(float elapsed)
        {

            if (!_musicTied && sillyGirl != null && _mainMusic != null)
            {
                TieMusicToSillyGirl();
                _musicTied = true;
            }

            if (!_mainMusic)
            {
                Debug.Log("Failed to tie the music to SillyGirl. Attempting to reset _mainMusic...");
                _mainMusic = PlayState.Instance.heartBeatMusic;
                sillyGirl.PlayAnimation("rig_|rig_Action");
                spinner.PlayAnimation("Spin");
            }
            base.UpdateLevel(elapsed);

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

        public override void HandleLevelUpdate(float elapsed, Vector2 mousePos)
        {

            if (Controls.ACCEPT)
            {
                Debug.Log("[DEBUG] ACCEPT action triggered");
                if (sillyGirl != null)
                {
                    sillyGirl.transform.position += Vector3.forward * 2f;
                    Debug.Log($"Moved SillyGirl closer to {sillyGirl.transform.position}");
                }
            }

            if (Controls.isPressed("accept"))
            {
                Debug.Log("[ACCEPT] - Pressed from UpdateLevel");
            }

            if (Controls.BACK)
            {
                Debug.Log("[DEBUG] BACK action triggered");
                if (sillyGirl != null)
                {
                    sillyGirl.transform.position += Vector3.back * 2f;
                    Debug.Log($"Moved SillyGirl farther to {sillyGirl.transform.position}");
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (_mainMusic != null)
                {
                    _mainMusic.spatialEnabled = !_mainMusic.spatialEnabled;
                    Debug.Log($"Spatial audio: {_mainMusic.spatialEnabled}");

                    if (_mainMusic.spatialEnabled)
                    {
                        Debug.Log("Spatial audio ENABLED - sound tied to SillyGirl");
                    }
                    else
                    {
                        Debug.Log("Spatial audio DISABLED - sound is now 2D");
                    }
                }
            }
            base.HandleLevelUpdate(elapsed, mousePos);
        }

        public override void TriggerEvent(string eventType, object parameters)
        {
            Debug.Log($"[TEST] Event triggered: {eventType}");
        }

        public override void ResetLevel()
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
