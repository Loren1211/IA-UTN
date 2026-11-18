using UnityEngine;

public abstract class HunterState : State<HunterStateId>
{
    protected const float DestinationRetrySeconds = 1f;
    protected const float SafeZoneCenterStopDistance = 0.6f;
    protected const float FallbackStopDistance = 0.5f;
    protected const float ExitArrivalRadius = 1.5f;
    protected const float ExitArrivalDelay = 0.3f;
    protected const float ExitProgressDistance = 30f;

    protected readonly HunterAgent Hunter;

    protected HunterState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter, string name)
        : base(fsm, name)
    {
        Hunter = hunter;
    }

    // Priority: Dead > Flee > Exit > Gather > Attack > Search.
    protected bool CheckGlobalTransitions()
    {
        if (Hunter.IsDead)
        {
            if (_fsm.CurrentState != HunterStateId.Dead)
                _fsm.ChangeState(HunterStateId.Dead);
            return true;
        }

        if (Hunter.CanFlee && !Hunter.IsInSafeZone)
        {
            if (_fsm.CurrentState != HunterStateId.Flee)
            {
                _fsm.ChangeState(HunterStateId.Flee);
                return true;
            }

            return false;
        }

        if (_fsm.CurrentState == HunterStateId.Flee)
            return false;

        if (Hunter.Gathers >= Hunter.RequiredGathers && _fsm.CurrentState != HunterStateId.Exit)
        {
            _fsm.ChangeState(HunterStateId.Exit);
            return true;
        }

        if (_fsm.CurrentState == HunterStateId.Attack && Hunter.HasVisibleDeadBoid)
        {
            _fsm.ChangeState(HunterStateId.Gather);
            return true;
        }

        return false;
    }
}
