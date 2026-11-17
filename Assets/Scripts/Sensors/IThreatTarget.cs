using UnityEngine;

public interface IThreatTarget
{
    Transform ThreatTransform { get; }
    bool IsThreatAlive { get; }
}
