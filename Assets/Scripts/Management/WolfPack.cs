using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class WolfPack : MonoBehaviour
{
    private readonly List<WolfAgent> _agents = new();
    private readonly List<Transform> _wolves = new();

    public IReadOnlyList<WolfAgent> Agents => _agents;
    public IReadOnlyList<Transform> Wolves => _wolves;

    public event Action<Vector3> AlertRaised;
    public event Action AlertCleared;

    public Vector3? LastKnownHunterPosition { get; private set; }

    public bool IsHunting
    {
        get
        {
            foreach (WolfAgent wolf in _agents)
            {
                if (wolf != null && wolf.IsHunting) return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        _agents.Clear();
        GetComponentsInChildren(true, _agents);

        _wolves.Clear();
        foreach (WolfAgent wolf in _agents) _wolves.Add(wolf.transform);
    }

    public void RaiseAlert(Vector3 hunterPosition)
    {
        LastKnownHunterPosition = hunterPosition;
        AlertRaised?.Invoke(hunterPosition);
    }

    public void ClearAlert()
    {
        if (LastKnownHunterPosition == null) return;

        LastKnownHunterPosition = null;
        AlertCleared?.Invoke();
    }
}
