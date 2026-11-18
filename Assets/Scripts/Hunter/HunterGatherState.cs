using UnityEngine;

public class GatherState : HunterState
{
    private Boid _target;
    private bool _usingPath;
    private float _gatherTimer;

    public GatherState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Gather") { }

    public override void Enter()
    {
        Hunter.ClearAttackTarget();
        Hunter.ClearSafeZone();
        _target = Hunter.GetClosestVisibleDeadBoid();
        Hunter.SetGatherTarget(_target);
        _usingPath = false;
        _gatherTimer = 0f;

        if (_target == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Hunter.MoveDirect(_target.transform.position, Hunter.MoveSpeed, Hunter.GatherRange);
        Hunter.SetActionProgress(0f, "Gathering");
    }

    public override void Update()
    {
        if (CheckGlobalTransitions()) return;

        if (_target == null || _target.IsAlive || _target.IsGathered)
        {
            CancelGather();
            return;
        }

        float distance = Vector3.Distance(Hunter.transform.position, _target.transform.position);

        if (distance <= Hunter.GatherRange)
        {
            Hunter.Stop();
            _gatherTimer += Time.deltaTime;
            Hunter.SetActionProgress(Mathf.Clamp01(_gatherTimer / Hunter.GatherDuration), "Gathering");

            if (_gatherTimer < Hunter.GatherDuration) return;

            _target.Gather();
            Hunter.RegisterGather();
            Hunter.ClearGatherTarget();
            Hunter.SetActionProgress(0f, "");

            _fsm.ChangeState(Hunter.Gathers >= Hunter.RequiredGathers
                ? HunterStateId.Exit
                : HunterStateId.Search);
            return;
        }

        if (Hunter.Sensor.CanSee(_target.transform))
        {
            _usingPath = false;
            Hunter.MoveDirect(_target.transform.position, Hunter.MoveSpeed, Hunter.GatherRange);
        }
        else if (!_usingPath || !Hunter.NavAgent.IsMoving)
        {
            _usingPath = Hunter.NavigateTo(_target.transform.position, Hunter.MoveSpeed);
            if (!_usingPath)
            {
                CancelGather();
                return;
            }
        }

        Hunter.SetActionProgress(0f, "Gathering");
    }

    private void CancelGather()
    {
        Hunter.ClearGatherTarget();
        Hunter.SetActionProgress(0f, "");
        _fsm.ChangeState(HunterStateId.Search);
    }

    public override void Exit()
    {
        Hunter.ClearGatherTarget();
        _target = null;
    }
}
