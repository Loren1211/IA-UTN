using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class WolfPack : MonoBehaviour
{
    private readonly List<WolfAgent> _agents = new();

    public IReadOnlyList<WolfAgent> Agents => _agents;

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
    }

    public void RaiseAlert(Vector3 hunterPosition)
    {
        LastKnownHunterPosition = hunterPosition;
        AlertRaised?.Invoke(hunterPosition);
    }

    public void ClearAlert()
    {
        LastKnownHunterPosition = null;
        AlertCleared?.Invoke();
    }
}
