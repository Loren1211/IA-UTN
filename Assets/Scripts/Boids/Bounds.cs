using UnityEngine;

public class Bounds : MonoBehaviour
{
    public static Bounds Instance { get; private set; }

    [SerializeField] private float _width = 20f;
    [SerializeField] private float _height = 8f;
    [SerializeField] private float _margin = 1f;
    [SerializeField] private bool _drawGizmos = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public void Configure(float width, float height, float margin = 1f)
    {
        _width = Mathf.Max(1f, width);
        _height = Mathf.Max(1f, height);
        _margin = Mathf.Max(0f, margin);
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        Vector3 center = transform.position;
        float halfWidth = Mathf.Max(0.1f, _width * 0.5f - _margin);
        float halfHeight = Mathf.Max(0.1f, _height * 0.5f - _margin);

        position.x = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
        position.z = Mathf.Clamp(position.z, center.z - halfHeight, center.z + halfHeight);
        return position;
    }

    public bool Contains(Vector3 position)
    {
        Vector3 center = transform.position;
        return Mathf.Abs(position.x - center.x) <= _width * 0.5f &&
               Mathf.Abs(position.z - center.z) <= _height * 0.5f;
    }

    public Vector3 RandomPointInside()
    {
        Vector3 center = transform.position;
        float halfWidth = Mathf.Max(0.1f, _width * 0.5f - _margin);
        float halfHeight = Mathf.Max(0.1f, _height * 0.5f - _margin);

        return new Vector3(
            center.x + Random.Range(-halfWidth, halfWidth),
            center.y,
            center.z + Random.Range(-halfHeight, halfHeight));
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Gizmos.color = new Color(1f, 0.65f, 0.15f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(_width, 1f, _height));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
