using UnityEngine;

public class InterestPoint : MonoBehaviour
{
    [SerializeField] private float _maxLife = 10f;

    private float _life;
    private Renderer _renderer;
    private MaterialPropertyBlock _materialBlock;
    private Vector3 _baseScale;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsActive => _life > 0f;
    public float Life01 => _maxLife > 0f ? Mathf.Clamp01(_life / _maxLife) : 0f;

    private void Awake()
    {
        if (_maxLife <= 0f) _maxLife = 10f;
        _life = _maxLife;
        _baseScale = transform.localScale;
        _renderer = GetComponentInChildren<Renderer>();
        _materialBlock = new MaterialPropertyBlock();
        UpdateVisual();
    }

    public void Configure(float life)
    {
        _maxLife = Mathf.Max(0.1f, life);
        _life = _maxLife;
        UpdateVisual();
    }

    public void Consume(float amount)
    {
        if (_life <= 0f || amount <= 0f) return;

        _life -= amount;

        if (_life <= 0f)
        {
            _life = 0f;
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float lifePercent = _maxLife > 0f ? Mathf.Clamp01(_life / _maxLife) : 0f;
        transform.localScale = _baseScale * Mathf.Lerp(0.55f, 1f, lifePercent);

        if (_renderer == null) return;

        Color color = Color.Lerp(new Color(0.25f, 0.12f, 0.08f),
                                 new Color(0.85f, 0.20f, 0.12f), lifePercent);
        _renderer.GetPropertyBlock(_materialBlock);
        _materialBlock.SetColor(BaseColorId, color);
        _materialBlock.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_materialBlock);
    }
}
