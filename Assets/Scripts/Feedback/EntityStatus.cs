using System;
using UnityEngine;

public class EntityStatus : MonoBehaviour
{
    [SerializeField] private string _displayName;

    private string _state = "Idle";
    private string _action = "";
    private float _progress;

    public event Action<EntityStatus> Changed;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public string State => _state;
    public string Action => _action;
    public float Progress => _progress;

    private void Awake()
    {
        if (string.IsNullOrEmpty(_displayName)) _displayName = name;
    }

    public void SetState(string state)
    {
        if (_state == state) return;

        _state = state;
        Changed?.Invoke(this);
    }

    public void SetAction(string action)
    {
        if (_action == action) return;

        _action = action;
        Changed?.Invoke(this);
    }

    public void SetAction(string action, float progress)
    {
        _progress = Mathf.Clamp01(progress);
        SetAction(action);
    }

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
    }
}
