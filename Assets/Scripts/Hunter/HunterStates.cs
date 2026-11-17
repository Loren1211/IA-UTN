using UnityEngine;

public abstract class HunterState : State<HunterStateId>
{
    protected readonly HunterAgent Hunter;

    protected HunterState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter, string name)
        : base(fsm, name)
    {
        Hunter = hunter;
    }

    protected bool CheckGlobalTransitions()
    {
        if (Hunter.IsDead)
        {
            if (_fsm.CurrentState != HunterStateId.Dead)
                _fsm.ChangeState(HunterStateId.Dead);
            return true;
        }

        if (_fsm.CurrentState == HunterStateId.Dead || _fsm.CurrentState == HunterStateId.Flee)
            return false;

        if (Hunter.WolfVisible)
        {
            _fsm.ChangeState(HunterStateId.Flee);
            return true;
        }

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
            Hunter.TrySpawnInterestPoint();
            return;
        }

        if (!Hunter.NavAgent.IsMoving && Time.time >= _nextDestinationTime)
        {
            ChooseDestination();
            _nextDestinationTime = Time.time + 1f;
        }

        Hunter.TrySpawnInterestPoint();
    }

    private void ChooseDestination()
    {
        if (Hunter.NavigateToRandomCotoNode(Hunter.MoveSpeed)) return;
        Hunter.NavigateTo(Hunter.CotoCenter, Hunter.MoveSpeed);
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
    private SafeZone _safeZone;
    private float _waitTimer;
    public FleeState(FiniteStateMachine<HunterStateId> fsm, HunterAgent hunter)
        : base(fsm, hunter, "Flee") { }

    public override void Enter()
    {
        Hunter.ClearAttackTarget();
        Hunter.ClearGatherTarget();

        _safeZone = Hunter.FindClosestSafeZone();
        Hunter.SetSelectedSafeZone(_safeZone);
        _waitTimer = 0f;

        if (_safeZone == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 center = _safeZone.transform.position;
        if (!Hunter.NavigateTo(center, Hunter.FleeSpeed))
            Hunter.MoveDirect(center, Hunter.FleeSpeed, 0.4f);

        Hunter.SetActionProgress(0f, "Fleeing");
    }

    public override void Update()
    {
        if (CheckGlobalTransitions()) return;

        if (_safeZone == null)
        {
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 center = _safeZone.transform.position;
        bool inside = _safeZone.Contains(Hunter.transform.position);

        if (!inside)
        {
            if (!Hunter.NavAgent.IsMoving)
            {
                if (Hunter.NavigateTo(center, Hunter.FleeSpeed)) return;
                Hunter.MoveDirect(center, Hunter.FleeSpeed, 0.4f);
            }

            Hunter.SetActionProgress(0f, "Fleeing");
            return;
        }

        if (Vector3.Distance(Hunter.transform.position, center) > 0.6f)
        {
            Hunter.MoveDirect(center, Hunter.FleeSpeed, 0.5f);
            Hunter.SetActionProgress(0f, "Fleeing");
            return;
        }

        Hunter.Stop();
        _waitTimer += Time.deltaTime;

        bool wolvesHunting = Hunter.WolfPack != null && Hunter.WolfPack.IsHunting;
        if (wolvesHunting || Hunter.WolfVisible) _waitTimer = 0f;

        Hunter.SetActionProgress(Mathf.Clamp01(_waitTimer / Hunter.SafeZoneWaitTime),
            "Waiting in safe zone");

        if (_waitTimer < Hunter.SafeZoneWaitTime) return;

        _fsm.ChangeState(Hunter.Gathers >= Hunter.RequiredGathers
            ? HunterStateId.Exit
            : HunterStateId.Search);
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

        if (!Hunter.NavigateTo(Hunter.ExitPoint.position, Hunter.MoveSpeed))
            Hunter.MoveDirect(Hunter.ExitPoint.position, Hunter.MoveSpeed, 0.5f);

        Hunter.SetActionProgress(0f, "Moving to exit");
    }

    public override void Update()
    {
        if (_arrived)
        {
            _arrivedTimer += Time.deltaTime;
            if (_arrivedTimer < 0.3f) return;

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

        if (distance > 1.5f && !Hunter.NavAgent.IsMoving)
            Hunter.MoveDirect(Hunter.ExitPoint.position, Hunter.MoveSpeed, 0.5f);

        Hunter.SetActionProgress(Mathf.Clamp01(1f - distance / 30f), "Moving to exit");

        if (distance > 1.5f) return;

        _arrived = true;
        _arrivedTimer = 0f;
        Hunter.Stop();
        Hunter.SetVisible(false);
        Hunter.SetActionProgress(1f, "At the exit");
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
