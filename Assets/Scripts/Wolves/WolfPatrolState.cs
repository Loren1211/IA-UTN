using UnityEngine;

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
