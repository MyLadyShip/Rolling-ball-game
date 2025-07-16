using UnityEngine;

[RequireComponent(typeof(Renderer))] // Ensures a Renderer component exists
public class RotateStat : MonoBehaviour
{
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    [Header("Rotation Settings")]
    [SerializeField, Range(10f, 100f)] private float rotateSpeed = 50f;
    
    [Header("Float Settings")]
    [SerializeField, Range(0.1f, 2f)] private float floatFrequency = 1f;
    [SerializeField, Range(0.1f, 0.5f)] private float floatAmplitude = 0.2f;
    
    [Header("Glow Settings")]
    [SerializeField, Range(0.5f, 3f)] private float minGlowBrightness = 1f;
    [SerializeField, Range(1f, 5f)] private float maxGlowBrightness = 2f;
    [SerializeField, Range(0.5f, 3f)] private float glowPulseSpeed = 1f;

    private Vector3 _startPosition;
    private Material _materialInstance; // Use material instance to avoid memory leaks
    private Color _baseEmissionColor;
    private float _randomPhaseOffset; // For variation between instances

    private void Awake()
    {
        _startPosition = transform.position;
        _materialInstance = GetComponent<Renderer>().material;
        _baseEmissionColor = _materialInstance.GetColor("_EmissionColor");
        _randomPhaseOffset = Random.Range(0f, 2f * Mathf.PI);
        _materialInstance.enableInstancing = true; // Enable instancing for better performance
        _materialInstance.SetColor(EmissionColorID, _baseEmissionColor);
        _materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        // URP Shader Graph uses different emission property
        int emissionPropertyID = EmissionColorID;
        if (!_materialInstance.HasProperty(emissionPropertyID))
        {
            emissionPropertyID = Shader.PropertyToID("_Emission");
        }

        _baseEmissionColor = _materialInstance.GetColor(emissionPropertyID);
    }

    private void Update()
    {
        HandleRotation();
        HandleFloating();
        HandleGlow();
    }

    private void HandleRotation()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void HandleFloating()
    {
        float newY = _startPosition.y + 
                    Mathf.Sin((Time.time + _randomPhaseOffset) * floatFrequency) * 
                    floatAmplitude;
        
        transform.position = new Vector3(
            _startPosition.x,
            newY,
            _startPosition.z
        );
    }

    private void HandleGlow()
    {
        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
        Color currentEmission = _baseEmissionColor *
                              Mathf.Lerp(minGlowBrightness, maxGlowBrightness, pulse);

        // URP-compatible emission update
        _materialInstance.SetColor(EmissionColorID, currentEmission);

        // Required in URP to force emission update
        if (_materialInstance.IsKeywordEnabled("_EMISSION") == false)
        {
            _materialInstance.EnableKeyword("_EMISSION");
        }
    // Add after emission update:
        DynamicGI.SetEmissive(GetComponent<Renderer>(), currentEmission);
        DynamicGI.UpdateEnvironment();
    }

    private void OnDestroy()
    {
        // Clean up instantiated material
        if (_materialInstance != null && Application.isPlaying)
        {
            Destroy(_materialInstance);
        }
    }

    public void ResetVisuals()
    {
        transform.position = _startPosition;
        transform.rotation = Quaternion.identity;
        _materialInstance.SetColor("_EmissionColor", _baseEmissionColor);
    }
}