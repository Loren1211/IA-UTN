using UnityEngine;

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
            Hunter.SpawnRangedProjectile(_target);
            Hunter.ResetRangedCooldown();
            Hunter.SetActionProgress(1f, "Shooting");
            Hunter.ClearAttackTarget();
            _fsm.ChangeState(HunterStateId.Search);
            return;
        }

        Vector3 predictedPosition = Hunter.GetPredictedPosition(_target, Hunter.MoveSpeed);
        Hunter.MoveDirect(predictedPosition, Hunter.MoveSpeed, Hunter.MeleeStopDistance);
        Hunter.SetActionProgress(0f, distance <= Hunter.RangeAttackRadius
            ? "Waiting for shot"
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
