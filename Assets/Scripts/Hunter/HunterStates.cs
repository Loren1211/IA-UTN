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

public class SearchState : HunterState
{
    private float _nextDestinationTime;

    public SearchState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Search") { }

    public override void Enter()
    {
        Hunter.ClearTargets();
        Hunter.ClearSafeZone();
        Hunter.SetActionProgress(0f, "Searching");
        _nextDestinationTime = 0f;
    }

    public override void Update()
    {
        if (CheckGlobalTransitions()) return;

        Boid deadBoid = Hunter.GetClosestVisibleDeadBoid();
        if (deadBoid != null)
        {
            Hunter.SetGatherTarget(deadBoid);
            _fsm.ChangeState(HunterStateId.Gather);
            return;
        }

        Boid liveBoid = Hunter.GetClosestVisibleLiveBoid();
        if (liveBoid != null)
        {
            if (Hunter.CanAttack(liveBoid))
            {
                _fsm.ChangeState(HunterStateId.Attack);
                return;
            }

            Vector3 predictedPosition = Hunter.GetPredictedPosition(liveBoid, Hunter.MoveSpeed);
            Hunter.MoveDirect(predictedPosition, Hunter.MoveSpeed, Hunter.MeleeStopDistance);
            Hunter.SetActionProgress(0f, "Waiting for attack cooldown");
            return;
        }

        if (!Hunter.NavAgent.IsMoving && Time.time >= _nextDestinationTime)
        {
            ChooseDestination();
            _nextDestinationTime = Time.time + DestinationRetrySeconds;
        }
    }

    private void ChooseDestination()
    {
        if (Hunter.NavigateToRandomHuntingGroundNode(Hunter.MoveSpeed)) return;
        Hunter.NavigateTo(Hunter.HuntingGroundCenter, Hunter.MoveSpeed);
    }
}

public class AttackState : HunterState
{
    private Boid _target;

    public AttackState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Attack") { }

    public override void Enter()
    {
        Hunter.ClearSafeZone();
        _target = Hunter.GetClosestVisibleLiveBoid();
        Hunter.SetAttackTarget(_target);

        if (_target == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Hunter.SetActionProgress(0f, "Attacking");
    }

    public override void Update()
    {
        if (CheckGlobalTransitions()) return;

        if (_target == null || !_target.IsAlive || _target.IsGathered)
        {
            Hunter.ClearAttackTarget();
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        if (!Hunter.Sensor.CanSee(_target.transform))
        {
            Hunter.ClearAttackTarget();
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        float distance = Vector3.Distance(Hunter.transform.position, _target.transform.position);

        if (distance <= Hunter.MeleeAttackRadius)
        {
            if (Hunter.MeleeReady)
            {
                HitTarget(Hunter.MeleeDamage, "Melee attack");
                Hunter.ResetMeleeCooldown();
                return;
            }

            Hunter.Stop();
            Hunter.SetActionProgress(0f, "Waiting for melee");
            return;
        }

        if (distance <= Hunter.RangeAttackRadius && Hunter.RangedReady)
        {
            HitTarget(Hunter.RangedDamage, "Ranged attack");
            Hunter.ResetRangedCooldown();
            return;
        }

        Vector3 predictedPosition = Hunter.GetPredictedPosition(_target, Hunter.MoveSpeed);
        Hunter.MoveDirect(predictedPosition, Hunter.MoveSpeed, Hunter.MeleeStopDistance);
        Hunter.SetActionProgress(0f, distance <= Hunter.RangeAttackRadius
            ? "Waiting for arrow"
            : "Chasing");
    }

    private void HitTarget(float damage, string label)
    {
        _target.TakeDamage(damage);
        Hunter.ClearAttackTarget();
        Hunter.SetActionProgress(1f, label);
        _fsm.ChangeState(HunterStateId.Search);
    }

    public override void Exit()
    {
        Hunter.ClearAttackTarget();
        _target = null;
    }
}

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

public class DeadState : HunterState
{
    private float _timer;

    public DeadState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Dead") { }

    public override void Enter()
    {
        _timer = 0f;
        Hunter.OnDeath();
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        Hunter.SetActionProgress(Mathf.Clamp01(_timer / Hunter.RespawnTime), "Dead");

        if (_timer < Hunter.RespawnTime) return;

        Hunter.RespawnAtStart();
        _fsm.ChangeState(HunterStateId.Search);
    }
}
