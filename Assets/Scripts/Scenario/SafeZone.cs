using UnityEngine;

public class SafeZone : ZoneMarker
{
    public Vector3 Center => transform.position;

    public void Configure(string displayName, Color color, Vector3 size)
    {
        base.Configure(displayName, color, ZoneType.Safe, size);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = new Color(0.30f, 0.90f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
