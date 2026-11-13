using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine<T>
{
    private readonly Dictionary<T, State<T>> _allStates = new();
    private State<T> _currentState;

    public T CurrentState { get; private set; }
    public T PreviousState { get; private set; }
    public bool HasCurrentState => _currentState != null;
    public string CurrentStateName => _currentState != null ? _currentState.Name : "Sin estado";

    public void AddState(T id, State<T> state)
    {
        _allStates[id] = state;
    }

    public void ChangeState(T id)
    {
        if (!_allStates.TryGetValue(id, out State<T> nextState))
        {
            Debug.LogError($"{id} - el estado no existe en la máquina de estados.");
            return;
        }

        if (nextState == _currentState)
        {
            _currentState.Exit();
            _currentState.Enter();
            return;
        }

        _currentState?.Exit();
        PreviousState = CurrentState;
        CurrentState = id;
        _currentState = nextState;
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Update();
    }

    public State<T> GetState(T id)
    {
        return _allStates.TryGetValue(id, out State<T> state) ? state : null;
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
