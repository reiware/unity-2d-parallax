using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UI_Parallax : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private RawImage targetImage;

    [Header("Parallax")] [SerializeField] private float rangeX = 0.0035f;
    [SerializeField] private float rangeY = 0.0020f;
    [SerializeField] private float smoothSpeed = 6f;

    [Header("Behaviour")] [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Gamepad")] [SerializeField] private bool supportGamepad = true;

    private static readonly int OffsetId = Shader.PropertyToID("_Offset");
    private const float KSettleThreshold = 0.000001f;

    private Material _runtimeMaterial;
    private Vector2 _currentOffset;
    private Vector2 _targetOffset;
    private Vector2 _lastMousePosition;

    private float _invScreenW;
    private float _invScreenH;
    private int _cachedScreenW;
    private int _cachedScreenH;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();

        if (targetImage == null)
        {
            Debug.LogError("RawImage not found.", this);
            enabled = false;
            return;
        }

        if (targetImage.material == null)
        {
            Debug.LogError("RawImage needs a material with _Offset property.", this);
            enabled = false;
            return;
        }

        _runtimeMaterial = new Material(targetImage.material);
        targetImage.material = _runtimeMaterial;
        _runtimeMaterial.SetVector(OffsetId, Vector2.zero);

        CacheScreenSize();

        _lastMousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
    }

    private void Update()
    {
        var dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        if (Screen.width != _cachedScreenW || Screen.height != _cachedScreenH)
            CacheScreenSize();

        var mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : _lastMousePosition;

        var mouseMoved = (mousePos - _lastMousePosition).sqrMagnitude > 0.01f;

        var stickInput = Vector2.zero;
        if (supportGamepad && Gamepad.current != null)
            stickInput = Gamepad.current.rightStick.ReadValue();

        if (stickInput.sqrMagnitude > 0.01f)
        {
            var x = stickInput.x * rangeX;
            var y = stickInput.y * rangeY;

            if (invertX) x = -x;
            if (invertY) y = -y;

            _targetOffset = new Vector2(
                Mathf.Clamp(x, -rangeX, rangeX),
                Mathf.Clamp(y, -rangeY, rangeY)
            );
        }
        else if (mouseMoved)
        {
            _lastMousePosition = mousePos;

            var x = (mousePos.x * _invScreenW - 0.5f) * 2f * rangeX;
            var y = (mousePos.y * _invScreenH - 0.5f) * 2f * rangeY;

            if (invertX) x = -x;
            if (invertY) y = -y;

            _targetOffset = new Vector2(
                Mathf.Clamp(x, -rangeX, rangeX),
                Mathf.Clamp(y, -rangeY, rangeY)
            );
        }

        var newOffset = Vector2.Lerp(_currentOffset, _targetOffset, 1f - Mathf.Exp(-smoothSpeed * dt));

        if (!((newOffset - _currentOffset).sqrMagnitude > KSettleThreshold)) return;
        _currentOffset = newOffset;
        _runtimeMaterial.SetVector(OffsetId, _currentOffset);
    }

    private void CacheScreenSize()
    {
        _cachedScreenW = Screen.width;
        _cachedScreenH = Screen.height;
        _invScreenW = _cachedScreenW > 0 ? 1f / _cachedScreenW : 0f;
        _invScreenH = _cachedScreenH > 0 ? 1f / _cachedScreenH : 0f;
    }

    private void OnDisable()
    {
        if (_runtimeMaterial != null)
            _runtimeMaterial.SetVector(OffsetId, Vector2.zero);
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial == null) return;

        if (Application.isPlaying)
            Destroy(_runtimeMaterial);
        else
            DestroyImmediate(_runtimeMaterial);
    }
}