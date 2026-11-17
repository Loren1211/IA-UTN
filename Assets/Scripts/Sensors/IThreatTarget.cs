using UnityEngine;

public interface IThreatTarget
{
    Transform ThreatTransform { get; }
    Vector3 Velocity { get; }
    bool IsThreatAlive { get; }
}
