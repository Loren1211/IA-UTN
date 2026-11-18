using UnityEngine;

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
