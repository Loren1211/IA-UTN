using UnityEngine;

public class WolfVisual : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _patrolColor = new(0.55f, 0.62f, 0.72f);
    [SerializeField] private Color _alertColor = new(1f, 0.60f, 0.20f);
    [SerializeField] private Color _chaseColor = new(0.90f, 0.22f, 0.20f);
    [SerializeField] private Color _searchColor = new(1f, 0.85f, 0.20f);

    private MaterialPropertyBlock _materialBlock;
    private WolfStateId _lastState = (WolfStateId)(-1);
    private bool _howling;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();

        _materialBlock = new MaterialPropertyBlock();
        SetColor(_patrolColor);
        _lastState = WolfStateId.Patrol;
    }

    public void SetState(WolfStateId state)
    {
        if (_lastState == state) return;

        _lastState = state;
        if (!_howling) SetColor(GetStateColor(state));
    }

    public void SetHowling(bool howling)
    {
        if (_howling == howling) return;

        _howling = howling;
        SetColor(howling ? _alertColor : GetStateColor(_lastState));
    }

    private Color GetStateColor(WolfStateId state)
    {
        return state switch
        {
            WolfStateId.Alert => _alertColor,
            WolfStateId.Chase => _chaseColor,
            WolfStateId.Search => _searchColor,
            _ => _patrolColor
        };
    }

    private void SetColor(Color color)
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_materialBlock);
        _materialBlock.SetColor(BaseColorId, color);
        _materialBlock.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_materialBlock);
    }
}
