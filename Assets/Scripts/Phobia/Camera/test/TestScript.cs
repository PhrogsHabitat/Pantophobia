using Phobia.Audio;
using UnityEngine;

public class SpatialAudioDemo : MonoBehaviour
{
    public AudioClip testSound;
    public GameObject movingObject;
    public float moveSpeed = 5f;
    public float moveRange = 10f;

    private PhobiaSound _spatialSound;
    private Vector3 _startPosition;

	private void start()
    {
        _startPosition = movingObject.transform.position;

        // Create spatial sound tied to moving object
        _spatialSound = PhobiaSound.Create(testSound, 0.8f, true);
        _spatialSound.TieTo(movingObject.transform);
        _spatialSound.SetDistanceParams(0.1f, 20f); // Min volume, max distance
        _spatialSound.Play();

        Debug.Log("Spatial audio test started. Move with arrow keys.");
        Debug.Log("Controls:\n" +
                  "- Left/Right: Move sound source\n" +
                  "- Space: Toggle spatial audio\n" +
                  "- Up/Down: Adjust volume\n" +
                  "- F: Fade out/in");
    }

	private void Update()
    {
        // Move object with keyboard
        float newX = _startPosition.x + Mathf.PingPong(Time.time * moveSpeed, moveRange) - moveRange / 2;
        movingObject.transform.position = new Vector3(newX, _startPosition.y, _startPosition.z);

        // Toggle spatial audio with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _spatialSound.spatialEnabled = !_spatialSound.spatialEnabled;
            _spatialSound.SetupSpatialAudio();
            Debug.Log("Spatial audio: " + _spatialSound.spatialEnabled);
        }

        // Adjust volume with keys
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            float newVolume = Mathf.Clamp01(_spatialSound.GetVolume() + 0.1f);
            _spatialSound.SetVolume(newVolume);
            Debug.Log("Volume: " + newVolume);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            float newVolume = Mathf.Clamp01(_spatialSound.GetVolume() - 0.1f);
            _spatialSound.SetVolume(newVolume);
            Debug.Log("Volume: " + newVolume);
        }

        // Fade with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_spatialSound.GetVolume() > 0.5f)
            {
                _spatialSound.FadeTo(0, 2f, () => Debug.Log("Fade out complete"));
            }
            else
            {
                _spatialSound.FadeTo(1, 2f, () => Debug.Log("Fade in complete"));
            }
        }
    }

	private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_spatialSound == null)
        {
            return;
        }

        // Draw audio range visualization
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(movingObject.transform.position, _spatialSound.maxDistance);

        // Draw min volume range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(movingObject.transform.position, 1f); // Min distance
    }
}
