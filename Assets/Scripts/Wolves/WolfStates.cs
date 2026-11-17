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

public class WolfPatrolState : WolfState
{
    public WolfPatrolState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf)
        : base(fsm, wolf, "Patrol") { }

    public override void Enter()
    {
        Wolf.Stop();
        Wolf.SetAction("Patrolling");
    }

    public override void Update()
    {
        if (TryStartChase()) return;

        Transform waypoint = Wolf.CurrentWaypoint;
        if (waypoint == null) return;

        float distance = Vector3.Distance(Wolf.transform.position, waypoint.position);
        if (distance <= Wolf.WaypointReachDistance)
        {
            Wolf.AdvanceWaypoint();
            return;
        }

        if (Wolf.HasLineOfSight(waypoint.position))
        {
            Wolf.MoveDirect(waypoint.position, Wolf.PatrolSpeed, Wolf.WaypointReachDistance);
            return;
        }

        if (!Wolf.NavAgent.IsMoving && !Wolf.NavigateTo(waypoint.position, Wolf.PatrolSpeed))
            Wolf.AdvanceWaypoint();
    }
}

public class WolfAlertState : WolfState
{
    private float _timer;

    public WolfAlertState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf)
        : base(fsm, wolf, "Alert") { }

    public override void Enter()
    {
        _timer = 0f;
        Wolf.SetAction("Responding to howl");
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

public class WolfChaseState : WolfState
{
    public WolfChaseState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf)
        : base(fsm, wolf, "Chase") { }

    public override void Enter()
    {
        Wolf.SetAction("Chasing");
    }

    public override void Update()
    {
        if (Wolf.TargetInSafeZone())
        {
            Wolf.AbandonChase();
            return;
        }

        if (Wolf.UpdateDetection())
        {
            Vector3 position = Wolf.CurrentTarget.position;
            Wolf.ReportDetection(position);

            float distance = Vector3.Distance(Wolf.transform.position, position);
            if (distance <= Wolf.CaptureRadius)
            {
                Wolf.CaptureHunter();
                return;
            }

            if (Wolf.HasLineOfSight(position))
            {
                Wolf.MoveDirect(position, Wolf.ChaseSpeed, Wolf.CaptureRadius * 0.8f);
            }
            else if (!Wolf.NavAgent.IsFollowingPath)
            {
                Wolf.NavigateTo(position, Wolf.ChaseSpeed);
            }

            Wolf.FaceTowards(position);
            return;
        }

        if (!Wolf.HasLastKnown)
        {
            _fsm.ChangeState(WolfStateId.Patrol);
            return;
        }

        float lastDistance = Vector3.Distance(Wolf.transform.position, Wolf.LastKnownPosition);
        if (lastDistance <= Wolf.WaypointReachDistance)
        {
            _fsm.ChangeState(WolfStateId.Search);
            return;
        }

        if (!Wolf.NavAgent.IsFollowingPath && !Wolf.NavigateTo(Wolf.LastKnownPosition, Wolf.ChaseSpeed))
            _fsm.ChangeState(WolfStateId.Search);
    }
}

public class WolfSearchState : WolfState
{
    private float _timer;

    public WolfSearchState(FiniteStateMachine<WolfStateId> fsm, WolfAgent wolf)
        : base(fsm, wolf, "Search") { }

    public override void Enter()
    {
        _timer = 0f;
        Wolf.Stop();
        Wolf.SetAction("Searching");
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

        Wolf.RotateInPlace(Wolf.TurnSpeed * 0.5f);

        if (_timer < Wolf.SearchDuration) return;

        Wolf.ForgetTarget();
        _fsm.ChangeState(WolfStateId.Patrol);
    }
}
