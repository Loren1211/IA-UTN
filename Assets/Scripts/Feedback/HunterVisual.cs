using UnityEngine;

public class HunterVisual : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _defaultColor = new(0.25f, 0.8f, 0.35f);
    [SerializeField] private Color _attackColor = new(1f, 0.85f, 0.2f);
    [SerializeField] private Color _fleeColor = new(0.20f, 0.78f, 0.95f);

    private MaterialPropertyBlock _materialBlock;
    private bool _attacking;
    private bool _fleeing;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();

        _materialBlock = new MaterialPropertyBlock();
        SetColor(_defaultColor);
    }

    public void SetAttacking(bool attacking)
    {
        if (_attacking == attacking) return;

        _attacking = attacking;
        RefreshColor();
    }

    public void SetFleeing(bool fleeing)
    {
        if (_fleeing == fleeing) return;

        _fleeing = fleeing;
        RefreshColor();
    }

    private void RefreshColor()
    {
        SetColor(_fleeing ? _fleeColor : _attacking ? _attackColor : _defaultColor);
    }

    public void SetVisible(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;
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
