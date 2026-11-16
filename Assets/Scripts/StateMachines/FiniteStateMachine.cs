using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine<T>
{
    private readonly Dictionary<T, State<T>> _allStates = new();
    private State<T> _currentState;

    public T CurrentState { get; private set; }
    public string CurrentStateName => _currentState != null ? _currentState.Name : "No state";

    public void AddState(T id, State<T> state)
    {
        _allStates[id] = state;
    }

    public void ChangeState(T id)
    {
        if (!_allStates.TryGetValue(id, out State<T> nextState))
        {
            Debug.LogError($"{id} - state is not registered in the state machine.");
            return;
        }

        if (nextState == _currentState) return;

        _currentState?.Exit();
        CurrentState = id;
        _currentState = nextState;
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Update();
    }
}

public abstract class State<T>
{
    public virtual string Name { get; protected set; }
    protected FiniteStateMachine<T> _fsm;

    protected State(FiniteStateMachine<T> fsm, string name = "")
    {
        _fsm = fsm;
        Name = name;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
