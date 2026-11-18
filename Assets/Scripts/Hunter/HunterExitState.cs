using UnityEngine;

public class ExitState : HunterState
{
    private bool _arrived;
    private float _arrivedTimer;

    public ExitState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Exit") { }

    public override void Enter()
    {
        Hunter.ClearAttackTarget();
        Hunter.ClearGatherTarget();
        Hunter.ClearSafeZone();
        _arrived = false;
        _arrivedTimer = 0f;

        if (Hunter.ExitPoint == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        MoveToExit();
        Hunter.SetActionProgress(0f, "Moving to exit");
    }

    public override void Update()
    {
        if (_arrived)
        {
            _arrivedTimer += Time.deltaTime;
            if (_arrivedTimer < ExitArrivalDelay) return;

            Hunter.RespawnAtStart();
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        if (CheckGlobalTransitions()) return;

        if (Hunter.ExitPoint == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        float distance = Vector3.Distance(Hunter.transform.position, Hunter.ExitPoint.position);

        if (distance > ExitArrivalRadius && !Hunter.NavAgent.IsMoving) MoveToExit();

        Hunter.SetActionProgress(Mathf.Clamp01(1f - distance / ExitProgressDistance), "Moving to exit");

        if (distance > ExitArrivalRadius) return;

        _arrived = true;
        _arrivedTimer = 0f;
        Hunter.Stop();
        Hunter.SetVisible(false);
        Hunter.SetActionProgress(1f, "At the exit");
        Hunter.RegisterEscape();
    }

    private void MoveToExit()
    {
        Vector3 exit = Hunter.ExitPoint.position;
        if (Hunter.NavigateTo(exit, Hunter.MoveSpeed)) return;
        if (Hunter.Sensor.HasLineOfSight(exit))
            Hunter.MoveDirect(exit, Hunter.MoveSpeed, FallbackStopDistance);
    }
}
