using UnityEngine;

public class WolfSearchState : WolfState
{
    private const float SearchTurnScale = 0.5f;

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

        Wolf.RotateInPlace(Wolf.TurnSpeed * SearchTurnScale);

        if (_timer < Wolf.SearchDuration) return;

        Wolf.ForgetTarget();
        _fsm.ChangeState(WolfStateId.Patrol);
    }
}
