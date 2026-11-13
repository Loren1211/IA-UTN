using System.Collections.Generic;
using UnityEngine;

public class SafeZone : ZoneMarker
{
    public static readonly List<SafeZone> All = new();

    [SerializeField] private Vector3 _size = new(6f, 0f, 6f);
    [SerializeField] private Transform _entryPoint;

    public Node EntryNode { get; set; }
    public Vector3 Size => _size;
    public Vector3 EntryPosition => _entryPoint != null ? _entryPoint.position : transform.position;

    private void Awake()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    public void Configure(string displayName, Color color, Vector3 size, Transform entryPoint)
    {
        base.Configure(displayName, color);
        _size = size;
        _entryPoint = entryPoint;
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 center = transform.position;
        return Mathf.Abs(worldPosition.x - center.x) <= _size.x * 0.5f &&
               Mathf.Abs(worldPosition.z - center.z) <= _size.z * 0.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f,
                            new Vector3(Size.x, 3f, Size.z));

        if (_entryPoint == null) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(_entryPoint.position, 0.2f);
        Gizmos.DrawLine(transform.position, _entryPoint.position);
    }

    private void OnDestroy()
    {
        All.Remove(this);
    }
}
