using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Core Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _followSpeed = 5f;
    [SerializeField] private Vector3 _baseOffset = new Vector3(0f, 3f, -5f);

    [Header("Wall Avoidance")]
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private float _wallBuffer = 0.5f;
    [SerializeField] private float _minDistance = 2f;

    [Header("Dynamic Zoom")]
    [SerializeField] private float _zoomSpeed = 3f;
    [SerializeField] private float _zoomMultiplier = 0.5f;
    [SerializeField] private float _minZoom = 3f;
    [SerializeField] private float _maxZoom = 10f;
    private float _currentZoom = 5f;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 2f;
    [SerializeField] private bool _allowManualRotation = true;
    private float _currentRotationY;

    private Vector3 _smoothedPosition;
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _currentZoom = -_baseOffset.z;
        _currentRotationY = transform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    private void HandleZoom()
    {
        // Dynamic zoom based on player speed (example)
        float targetZoom = Mathf.Clamp(
            _currentZoom - (PlayerController.Instance.GetSpeed() * _zoomMultiplier),
            _minZoom,
            _maxZoom
        );
        
        _currentZoom = Mathf.Lerp(_currentZoom, targetZoom, _zoomSpeed * Time.deltaTime);
        _baseOffset.z = -_currentZoom;
    }

    private void HandleRotation()
    {
        if (!_allowManualRotation) return;

        if (Input.GetMouseButton(1)) // Right-click hold to rotate
        {
            _currentRotationY += Input.GetAxis("Mouse X") * _rotationSpeed;
        }

        // Auto-level camera when not rotating
        else if (transform.forward.y != 0)
        {
            _currentRotationY = Mathf.LerpAngle(
                _currentRotationY,
                _target.eulerAngles.y,
                2f * Time.deltaTime
            );
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 desiredOffset = Quaternion.Euler(0, _currentRotationY, 0) * _baseOffset;
        Vector3 desiredPosition = _target.position + desiredOffset;

        // Wall collision detection
        if (Physics.Linecast(_target.position, desiredPosition, out RaycastHit hit, _wallMask))
        {
            desiredPosition = hit.point + hit.normal * _wallBuffer;
            
            // Maintain minimum distance
            if (Vector3.Distance(desiredPosition, _target.position) < _minDistance)
            {
                desiredPosition = _target.position + 
                    (desiredPosition - _target.position).normalized * _minDistance;
            }
        }

        // Smooth follow
        _smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            _followSpeed * Time.deltaTime
        );

        transform.position = _smoothedPosition;
        transform.LookAt(_target);
    }

    // Call this when collecting capsules
    public void TriggerScreenShake(float duration, float intensity)
    {
        StartCoroutine(ShakeCamera(duration, intensity));
    }

    private IEnumerator ShakeCamera(float duration, float intensity)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.localPosition = originalPos + 
                Random.insideUnitSphere * intensity;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}