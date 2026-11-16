using System;
using System.Collections.Generic;
using UnityEngine;

public class WolfPack : MonoBehaviour
{
    [SerializeField] private Transform[] _wolves = Array.Empty<Transform>();

    public IReadOnlyList<Transform> Wolves => _wolves;

    public event Action<Vector3> AlertRaised;

    public Vector3? LastKnownHunterPosition { get; private set; }

    public void SetWolves(Transform[] wolves)
    {
        _wolves = wolves ?? Array.Empty<Transform>();
    }

    public void RaiseAlert(Vector3 hunterPosition)
    {
        LastKnownHunterPosition = hunterPosition;
        AlertRaised?.Invoke(hunterPosition);
    }

    public void ClearAlert()
    {
        LastKnownHunterPosition = null;
    }
}
