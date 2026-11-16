using UnityEngine;

public class SafeZone : ZoneMarker
{
    [SerializeField] private Transform _entryPoint;

    public Node EntryNode { get; set; }
    public Vector3 EntryPosition => _entryPoint != null ? _entryPoint.position : transform.position;

    public void Configure(string displayName, Color color, Vector3 size, Transform entryPoint)
    {
        base.Configure(displayName, color, ZoneType.Safe, size);
        _entryPoint = entryPoint;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (_entryPoint == null) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(_entryPoint.position, 0.2f);
        Gizmos.DrawLine(transform.position, _entryPoint.position);
    }
}
