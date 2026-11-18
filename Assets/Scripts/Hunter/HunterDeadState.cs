using UnityEngine;

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
