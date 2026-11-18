using UnityEngine;

public abstract class WolfState : State<WolfStateId>
{
    protected readonly WolfAgent Wolf;

    protected WolfState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf, string name)
        : base(fsm, name)
    {
        Wolf = wolf;
    }

    protected bool TryStartChase()
    {
        if (!Wolf.UpdateDetection()) return false;

        Wolf.ReportDetection(Wolf.CurrentTarget.position);
        _fsm.ChangeState(WolfStateId.Chase);
        return true;
    }
}
