using UnityEngine;

public class FleeState : HunterState
{
    private const float StuckTimeout = 2.5f;

    private SafeZone _safeZone;
    private float _waitTimer;
    private float _stuckTimer;

    public FleeState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Flee") { }

    public override void Enter()
    {
        Hunter.ClearAttackTarget();
        Hunter.ClearGatherTarget();

        _safeZone = Hunter.FindClosestSafeZone();
        Hunter.SetSelectedSafeZone(_safeZone);
        _waitTimer = 0f;
        _stuckTimer = 0f;

        if (_safeZone == null)
        {
            Hunter.DeferFlee(DestinationRetrySeconds);
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 center = _safeZone.transform.position;
        MoveToSafeZone(center);

        Hunter.SetActionProgress(0f, "Fleeing");
    }

    public override void Update()
    {
        if (CheckGlobalTransitions()) return;

        if (_safeZone == null)
        {
            Hunter.DeferFlee(DestinationRetrySeconds);
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 center = _safeZone.transform.position;
        bool inside = _safeZone.Contains(Hunter.transform.position);

        if (!inside)
        {
            if (!Hunter.NavAgent.IsMoving) MoveToSafeZone(center);

            _stuckTimer = Hunter.NavAgent.IsMoving ? 0f : _stuckTimer + Time.deltaTime;

            if (_stuckTimer >= StuckTimeout)
            {
                Hunter.DeferFlee(DestinationRetrySeconds);
                _fsm.ChangeState(HunterStateId.Search);
                return;
            }

            Hunter.SetActionProgress(0f, "Fleeing");
            return;
        }

        Vector3 toCenter = center - Hunter.transform.position;
        toCenter.y = 0f;

        if (toCenter.magnitude > SafeZoneCenterStopDistance)
        {
            Hunter.MoveDirect(center, Hunter.FleeSpeed, SafeZoneCenterStopDistance);
            Hunter.SetActionProgress(0f, "Fleeing");
            return;
        }

        Hunter.Stop();
        _waitTimer += Time.deltaTime;

        Hunter.SetActionProgress(Mathf.Clamp01(_waitTimer / Hunter.SafeZoneWaitTime),
            "Waiting in safe zone");

        if (_waitTimer < Hunter.SafeZoneWaitTime) return;

        _fsm.ChangeState(Hunter.Gathers >= Hunter.RequiredGathers
            ? HunterStateId.Exit
            : HunterStateId.Search);
    }

    private void MoveToSafeZone(Vector3 center)
    {
        if (Hunter.NavigateTo(center, Hunter.FleeSpeed)) return;
        if (Hunter.Sensor.HasLineOfSight(center))
            Hunter.MoveDirect(center, Hunter.FleeSpeed, FallbackStopDistance);
    }

    public override void Exit()
    {
        Hunter.ClearSafeZone();
        _safeZone = null;
    }
}
