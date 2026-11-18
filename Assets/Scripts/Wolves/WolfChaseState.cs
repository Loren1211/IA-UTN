using UnityEngine;

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
                Vector3 aim = position;
                HunterAgent hunter = Wolf.HunterTarget;

                if (hunter != null)
                {
                    float closingSpeed = Wolf.ChaseSpeed + hunter.Velocity.magnitude;
                    float lead = Mathf.Clamp(distance / Mathf.Max(0.1f, closingSpeed),
                        0.1f, Wolf.InterceptLeadMax);
                    Vector3 predicted = position + hunter.Velocity * lead;

                    bool predictedIsSafe = Wolf.ZoneManager == null ||
                                           !Wolf.ZoneManager.IsInsideSafeZone(predicted);

                    if (predictedIsSafe && Wolf.HasLineOfSight(predicted)) aim = predicted;
                }

                Wolf.MoveDirect(aim, Wolf.ChaseSpeed, Wolf.CaptureRadius * 0.8f);
            }
            else if (!Wolf.NavAgent.IsFollowingPath)
            {
                Wolf.NavigateTo(position, Wolf.ChaseSpeed);
            }

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
