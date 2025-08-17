using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class WatcherDem : MonoBehaviour
{
	// Animation States
	public bool IsIdle { get; private set; }
	public bool IsSitting { get; private set; }
	public bool IsStanding { get; private set; }
	public bool IsDying { get; private set; }
	public bool IsReady { get; private set; }

	// Shit to render the watcher dem with
	private SpriteRenderer _renderer;
	private Material _swapMaterial;
	private Vector3 _basePosition;
	private Coroutine _sitRoutine;

	[Header("Animation Settings")]
	[SerializeField] private float _frameRate = 24f;

	[Header("Animation Offsets")]
	[SerializeField] private Vector2 _standOffset = new Vector2(0, 0);
	[SerializeField] private Vector2 _idleOffset = new Vector2(0, 0);
	[SerializeField] private Vector2 _sitOffset = new Vector2(0, 0);

	[Header("Texture Swap Settings")]
	[SerializeField] private Texture2D _darkTexture;
	[Range(0, 1)][SerializeField] private float _swapAmount;

	private void Awake()
	{
		_renderer = GetComponent<SpriteRenderer>();
		_basePosition = transform.position;

		// Set default darkTexture if not assigned in inspector
		if (_darkTexture == null)
		{
			// Assumes a texture named "DefaultDarkTexture" exists in a Resources folder
			_darkTexture = Resources.Load<Texture2D>("DefaultDarkTexture");
			if (_darkTexture == null)
			{
				Debug.LogWarning("WatcherDem: No darkTexture assigned and DefaultDarkTexture not found in Resources.");
			}
		}

		// Setup texture swap material
		if (_swapMaterial != null)
		{
			_swapMaterial.SetTexture("_SwapTex", _darkTexture);
		}

		// Start in idle state
		StartIdleAnim();
	}

	public void ShowOutline()
	{
		SetSwapAmount(1f);
	}

	public void ShowNormal()
	{
		SetSwapAmount(0f);
	}

	private void SetSwapAmount(float amount)
	{
		_swapAmount = amount;
		if (_swapMaterial != null)
		{
			_swapMaterial.SetFloat("_Amount", _swapAmount);
		}
	}

	public void StartStandAnim()
	{
		ResetState();
		IsStanding = true;
		StartCoroutine(PlayAnimationRoutine(_standOffset));
		Debug.Log("WatcherDem: Playing standAnim - Let's get tall!");
	}

	public void StartIdleAnim()
	{
		ResetState();
		IsIdle = true;
		IsReady = true;
		StartCoroutine(PlayAnimationRoutine(_idleOffset, true));
		Debug.Log("WatcherDem: Playing idleAnim - Chilling like a villain");
	}

	public void StartSitAnim()
	{
		ResetState();
		IsSitting = true;

		if (_sitRoutine != null)
		{
			StopCoroutine(_sitRoutine);
		}

		_sitRoutine = StartCoroutine(PlaySitAnimation());
		Debug.Log("WatcherDem: Playing sitAnim - Taking a load off");
	}

	private IEnumerator PlaySitAnimation()
	{
		yield return StartCoroutine(PlayAnimationRoutine(_sitOffset));

		// Return to idle after sitting
		StartIdleAnim();
	}

	private IEnumerator PlayAnimationRoutine(Vector2 offset, bool loop = false)
	{
		ApplyOffset(offset);

		int index = 0;
		float frameDelay = 1f / _frameRate;

		do
		{
			_renderer.sprite = _renderer.sprite;
			index = (index + 1) % 1;
			yield return new WaitForSeconds(frameDelay);

		} while (loop || index != 0);
	}

	private void ApplyOffset(Vector2 offset)
	{
		transform.position = _basePosition + new Vector3(offset.x, offset.y, 0);
	}

	private void ResetState()
	{
		IsIdle = false;
		IsSitting = false;
		IsStanding = false;
		IsDying = false;
		IsReady = false;

		if (_sitRoutine != null)
		{
			StopCoroutine(_sitRoutine);
			_sitRoutine = null;
		}

		StopAllCoroutines();
	}

	private void Update()
	{
		HandleDebugInput();
		UpdateBasePosition();
	}

	private void HandleDebugInput()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			StartStandAnim();
		}

		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			StartIdleAnim();
		}

		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			StartSitAnim();
		}

		if (Input.GetKeyDown(KeyCode.O))
		{
			ShowOutline();
		}

		if (Input.GetKeyDown(KeyCode.P))
		{
			ShowNormal();
		}
	}

	private void UpdateBasePosition()
	{
		// Update base position when moved
		float moveSpeed = 5f;
		Vector3 moveInput = new Vector3(
			Input.GetAxis("Horizontal"),
			Input.GetAxis("Vertical"),
			0
		);

		if (moveInput.magnitude > 0)
		{
			transform.position += moveInput * moveSpeed * Time.deltaTime;
			_basePosition = transform.position;
		}

		if (Input.GetKeyDown(KeyCode.G))
		{
			Debug.Log($"WatcherDem Position: {transform.position}");
		}
	}

	private void OnDestroy()
	{
		if (_swapMaterial != null)
		{
			Destroy(_swapMaterial);
		}
	}

}
