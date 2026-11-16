using UnityEngine;

public class BoidVisual : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _fullHealthColor = new(0.20f, 0.78f, 0.95f);
    [SerializeField] private Color _halfHealthColor = new(1f, 0.85f, 0.2f);
    [SerializeField] private Color _lowHealthColor = new(0.95f, 0.22f, 0.20f);
    [SerializeField] private Color _deadColor = new(0.05f, 0.05f, 0.06f);

    private MaterialPropertyBlock _materialBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();

        _materialBlock = new MaterialPropertyBlock();
        SetHealth(1f);
    }

    public void SetHealth(float normalized)
    {
        Color color;

        if (normalized >= 0.75f) color = _fullHealthColor;
        else if (normalized >= 0.34f) color = _halfHealthColor;
        else color = _lowHealthColor;

        SetColor(color);
    }

    public void SetDead()
    {
        SetColor(_deadColor);
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
