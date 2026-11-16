using UnityEngine;

public enum ZoneType
{
    None,
    Start,
    HuntingGround,
    Safe,
    Exit
}

public class ZoneMarker : MonoBehaviour
{
    [SerializeField] private string _displayName = "Zone";
    [SerializeField] private Color _zoneColor = Color.white;
    [SerializeField] private ZoneType _type = ZoneType.None;
    [SerializeField] private Vector3 _size = Vector3.zero;

    public string DisplayName => _displayName;
    public Color ZoneColor => _zoneColor;
    public ZoneType Type => _type;
    public Vector3 Size => _size.sqrMagnitude > 0.0001f ? _size : transform.lossyScale;

    public void Configure(string displayName, Color color)
    {
        Configure(displayName, color, _type, _size);
    }

    public void Configure(string displayName, Color color, ZoneType type, Vector3 size)
    {
        _displayName = displayName;
        _zoneColor = color;
        _type = type;
        _size = size;
    }

    public void SetType(ZoneType type) => _type = type;

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 size = Size;
        Vector3 center = transform.position;

        return Mathf.Abs(worldPosition.x - center.x) <= size.x * 0.5f &&
               Mathf.Abs(worldPosition.z - center.z) <= size.z * 0.5f;
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new Color(_zoneColor.r, _zoneColor.g, _zoneColor.b, 0.35f);
        Vector3 size = Size;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(size.x, 3f, size.z));
    }
}
