using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class WolfPack : MonoBehaviour
{
    private readonly List<WolfAgent> _agents = new();

    public IReadOnlyList<WolfAgent> Agents => _agents;

    public event Action<Vector3, Vector3> AlertRaised;
    public event Action AlertCleared;

    public Vector3? LastKnownHunterPosition { get; private set; }

    private void Awake()
    {
        _agents.Clear();
        GetComponentsInChildren(true, _agents);
    }

    public void RaiseAlert(Vector3 hunterPosition, Vector3 howlerPosition)
    {
        LastKnownHunterPosition = hunterPosition;
        AlertRaised?.Invoke(hunterPosition, howlerPosition);
    }

    public void ClearAlert()
    {
        LastKnownHunterPosition = null;
        AlertCleared?.Invoke();
    }
}
