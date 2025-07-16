using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    #region Singleton
    public static PlayerController Instance { get; private set; }
    #endregion

    #region Ball Settings
    [Header("Ball Prefabs")]
    [SerializeField] private GameObject redBallPrefab;
    [SerializeField] private GameObject blueBallPrefab;
    [SerializeField] private Material redBallMaterial;
    [SerializeField] private Material blueBallMaterial;
    #endregion

    #region Movement Settings
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxVelocity = 10f;
    #endregion

    #region Touch Settings
    [Header("Touch Settings")]
    [SerializeField] private float touchDeadzone = 10f;
    [SerializeField] private float maxTouchDistance = 200f;
    private Vector2 _touchOrigin;
    private bool _isTouching;
    #endregion

    #region Components
    private Rigidbody _rb;
    private GameObject _currentBall;
    #endregion

    #region State
    private bool _controlsEnabled;
    private int _selectedBallIndex;
    private Vector3 _moveDirection;
    public float GetSpeed() => _rb.linearVelocity.magnitude;
    #endregion

    #region Initialization
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EnhancedTouchSupport.Enable();
        DisableControls();
        InitializeBall();
    }

    private void OnDestroy()
    {
        EnhancedTouchSupport.Disable();
    }
    #endregion

    #region Input Handling
    private void Update()
    {
        if (!_controlsEnabled || _rb == null) return;

        _moveDirection = Vector3.zero;

        // Handle touch input
        if (Touchscreen.current != null)
        {
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (touches.Count > 0)
            {
                var touch = touches[0];

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        _touchOrigin = touch.screenPosition;
                        _isTouching = true;
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        if (_isTouching)
                        {
                            Vector2 touchDelta = touch.screenPosition - _touchOrigin;
                            if (touchDelta.magnitude > touchDeadzone)
                            {
                                Vector2 direction = touchDelta.normalized;
                                float strength = Mathf.Clamp01(touchDelta.magnitude / maxTouchDistance);
                                _moveDirection = new Vector3(direction.x, 0, direction.y) * strength;
                            }
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        _isTouching = false;
                        break;
                }
            }
        }

        // Handle keyboard input
        if (Keyboard.current != null)
        {
            Vector2 input = new Vector2(
                Keyboard.current.dKey.isPressed ? 1 : Keyboard.current.aKey.isPressed ? -1 : 0,
                Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0
            );

            if (input != Vector2.zero)
            {
                _moveDirection = new Vector3(input.x, 0, input.y);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_controlsEnabled || _rb == null || _moveDirection == Vector3.zero)
            return;

        ApplyMovementForce(_moveDirection);
        LimitVelocity();
    }
    #endregion

    #region Ball Management
    public void SelectBall(int ballIndex)
    {
        _selectedBallIndex = ballIndex;
        SaveLoadManager.Instance?.SetBallIndex(ballIndex);
        InitializeBall();
    }

    public void InitializeBall()
    {
        if (_currentBall != null)
            Destroy(_currentBall);

        GameObject prefab = _selectedBallIndex == 0 ? redBallPrefab : blueBallPrefab;
        _currentBall = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _rb = _currentBall.GetComponent<Rigidbody>();

        ApplyBallMaterial();
    }

    private void ApplyBallMaterial()
    {
        MeshRenderer renderer = _currentBall.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = _selectedBallIndex == 0 ? redBallMaterial : blueBallMaterial;
        }
        else
        {
            Debug.LogError("Ball prefab missing MeshRenderer!");
        }
    }
    #endregion

    #region Movement
    private void ApplyMovementForce(Vector3 direction)
    {
        _rb.AddForce(direction * moveSpeed, ForceMode.Acceleration);
    }

    private void LimitVelocity()
    {
        if (_rb.linearVelocity.magnitude > maxVelocity)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxVelocity;
        }
    }
    #endregion

    #region Control Toggles
    public void EnableControls() => _controlsEnabled = true;
    public void DisableControls() => _controlsEnabled = false;
    #endregion

    #region Collectible Handling
  private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Capsules")) return;
    
    // Play effects IMMEDIATELY
    PlayCollectionEffects(other.transform.position);
    
    // Notify systems
    CollectibleCounter.Instance?.IncrementCount();
    
    // Return to pool
    CollectibleSpawner.Instance?.ReturnToPool(other.gameObject);
}

private void PlayCollectionEffects(Vector3 position)
{
    
    // Play particles
    ParticleEffectPool.Instance?.PlayRandomParticleEffect(position);
}
}
    #endregion