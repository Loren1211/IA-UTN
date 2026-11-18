using UnityEngine;

public class WolfAlertState : WolfState
{
    private float _timer;

    public WolfAlertState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf)
        : base(fsm, wolf, "Alert") { }

    public override void Enter()
    {
        _timer = 0f;
        Wolf.Stop();
        Wolf.SetAction("Responding to howl");

        if (Wolf.HasLastKnown)
            Wolf.NavigateTo(Wolf.LastKnownPosition, Wolf.ChaseSpeed);
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        if (Wolf.UpdateDetection())
        {
            Wolf.ReportDetection(Wolf.CurrentTarget.position);
            _fsm.ChangeState(WolfStateId.Chase);
            return;
        }

        if (Wolf.TargetInSafeZone())
        {
            Wolf.AbandonChase();
            return;
        }

        if (!Wolf.HasLastKnown)
        {
            _fsm.ChangeState(WolfStateId.Patrol);
            return;
        }

        if (!Wolf.NavAgent.IsMoving && !Wolf.NavigateTo(Wolf.LastKnownPosition, Wolf.ChaseSpeed))
            _fsm.ChangeState(WolfStateId.Patrol);

        if (_timer >= Wolf.AlertDuration)
            _fsm.ChangeState(WolfStateId.Chase);
    }
}
