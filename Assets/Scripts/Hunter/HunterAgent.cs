using System.Collections.Generic;
using UnityEngine;

public enum HunterStateId
{
    Search,
    Attack,
    Gather,
    Flee,
    Exit,
    Dead
}

[RequireComponent(typeof(NavAgent), typeof(SensorAgent))]
public class HunterAgent : MonoBehaviour, IThreatTarget
{
    [Header("FSM")]
    [SerializeField] private int _requiredGathers = 3;
    [SerializeField] private float _respawnTime = 5f;
    [SerializeField] private float _safeZoneWaitTime = 4f;

    [Header("Combat")]
    [SerializeField] private float _meleeAttackRadius = 2f;
    [SerializeField] private float _meleeStopDistance = 1.3f;
    [SerializeField] private float _rangeAttackRadius = 7f;
    [SerializeField] private float _meleeDamage = 1f;
    [SerializeField] private float _rangedDamage = 1.5f;
    [SerializeField] private float _meleeCooldown = 0.3f;
    [SerializeField] private float _rangedCooldown = 1f;
    [SerializeField] private float _gatherDuration = 2f;
    [SerializeField] private float _gatherRange = 1.5f;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _fleeSpeed = 6f;

    [Header("References")]
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _exitPoint;
    [SerializeField] private NavAgent _navAgent;
    [SerializeField] private SensorAgent _sensor;
    [SerializeField] private Collider _collider;
    [SerializeField] private HunterVisual _visual;
    [SerializeField] private EntityStatus _status;
    [SerializeField] private InterestPointSpawner _interestSpawner;
    [SerializeField] private BoidManager _boidManager;
    [SerializeField] private ZoneManager _zoneManager;
    [SerializeField] private WolfPack _wolfPack;

    private readonly FiniteStateMachine<HunterStateId> _fsm = new();

    private int _gathers;
    private float _nextMeleeTime;
    private float _nextRangedTime;
    private Vector3 _lastPosition;
    private bool _isDead;
    private Boid _attackTarget;
    private Boid _gatherTarget;
    private SafeZone _selectedSafeZone;

    public int RequiredGathers => _requiredGathers;
    public float RespawnTime => _respawnTime;
    public float SafeZoneWaitTime => _safeZoneWaitTime;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float MeleeStopDistance => _meleeStopDistance;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeDamage => _meleeDamage;
    public float RangedDamage => _rangedDamage;
    public float GatherDuration => _gatherDuration;
    public float GatherRange => _gatherRange;
    public float MoveSpeed => _moveSpeed;
    public float FleeSpeed => _fleeSpeed;

    public int Gathers => _gathers;
    public bool IsDead => _isDead;
    public bool MeleeReady => Time.time >= _nextMeleeTime;
    public bool RangedReady => Time.time >= _nextRangedTime;
    public float TimeUntilMelee => Mathf.Max(0f, _nextMeleeTime - Time.time);
    public float TimeUntilRanged => Mathf.Max(0f, _nextRangedTime - Time.time);

    public HunterStateId CurrentState => _fsm.CurrentState;
    public string CurrentStateName => _fsm.CurrentStateName;
    public Vector3 Velocity { get; private set; }
    public Boid AttackTarget => _attackTarget;
    public Boid GatherTarget => _gatherTarget;
    public SafeZone SelectedSafeZone => _selectedSafeZone;
    public NavAgent NavAgent => _navAgent;
    public SensorAgent Sensor => _sensor;
    public Transform StartPoint => _startPoint;
    public Transform ExitPoint => _exitPoint;
    public Transform ThreatTransform => transform;
    public BoidManager BoidManager => _boidManager;
    public ZoneManager ZoneManager => _zoneManager;
    public WolfPack WolfPack => _wolfPack;
    public Vector3 CotoCenter => global::Bounds.Instance != null
        ? global::Bounds.Instance.transform.position
        : Vector3.zero;

    private IReadOnlyList<Boid> Boids =>
        _boidManager != null ? _boidManager.Boids : System.Array.Empty<Boid>();

    public float CurrentActionProgress { get; private set; }
    public string CurrentActionLabel { get; private set; } = "";

    public bool IsInsideCoto => global::Bounds.Instance != null &&
                                global::Bounds.Instance.Contains(transform.position);

    public bool IsInSafeZone => _zoneManager != null && _zoneManager.IsInsideSafeZone(transform.position);

    public bool HasVisibleDeadBoid => GetClosestVisibleDeadBoid() != null;

    public bool WolfVisible
    {
        get
        {
            if (_isDead || _sensor == null || _wolfPack == null) return false;

            foreach (Transform wolf in _wolfPack.Wolves)
            {
                if (wolf == null || !wolf.gameObject.activeInHierarchy) continue;
                if (_sensor.CanSee(wolf)) return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        if (_navAgent == null) _navAgent = GetComponent<NavAgent>();
        if (_sensor == null) _sensor = GetComponent<SensorAgent>();
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_visual == null) _visual = GetComponent<HunterVisual>();
        if (_status == null) _status = GetComponent<EntityStatus>();
        if (_meleeDamage <= 0f) _meleeDamage = 1f;
        if (_rangedDamage <= 0f) _rangedDamage = 1.5f;
        if (_meleeCooldown <= 0f) _meleeCooldown = 0.3f;
        if (_rangedCooldown <= 0f) _rangedCooldown = 1f;
        if (_meleeStopDistance <= 0f) _meleeStopDistance = 1.3f;

        _fsm.AddState(HunterStateId.Search, new SearchState(_fsm, this));
        _fsm.AddState(HunterStateId.Attack, new AttackState(_fsm, this));
        _fsm.AddState(HunterStateId.Gather, new GatherState(_fsm, this));
        _fsm.AddState(HunterStateId.Flee, new FleeState(_fsm, this));
        _fsm.AddState(HunterStateId.Exit, new ExitState(_fsm, this));
        _fsm.AddState(HunterStateId.Dead, new DeadState(_fsm, this));

        _fsm.StateChanged += OnStateChanged;

        _lastPosition = transform.position;
    }

    private void Start()
    {
        if (_navAgent != null)
        {
            _navAgent.SetObstacleMask(Graph.Current != null
                ? Graph.Current.WallMask
                : LayerMask.GetMask("Wall"));
        }

        _fsm.ChangeState(HunterStateId.Search);
        if (_visual != null) _visual.SetAttacking(false);
    }

    private void Update()
    {
        _fsm.Update();
        UpdateHuntColor();
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        Vector3 delta = transform.position - _lastPosition;
        float maxStep = _fleeSpeed * deltaTime * 2f;

        Velocity = deltaTime <= 0f || delta.magnitude > maxStep
            ? Vector3.zero
            : delta / deltaTime;

        _lastPosition = transform.position;
    }

    private void UpdateHuntColor()
    {
        if (_visual == null) return;

        bool hunting = CurrentState == HunterStateId.Attack ||
                       (CurrentState == HunterStateId.Search &&
                        GetClosestVisibleLiveBoid() != null);

        _visual.SetFleeing(CurrentState == HunterStateId.Flee);
        _visual.SetAttacking(hunting);
    }

    private void OnStateChanged(HunterStateId state)
    {
        if (_status != null) _status.SetState(state.ToString());
    }

    public void SetActionProgress(float progress, string label)
    {
        CurrentActionProgress = Mathf.Clamp01(progress);
        CurrentActionLabel = label;

        if (_status != null) _status.SetAction(label, CurrentActionProgress);
    }

    public Boid GetClosestVisibleLiveBoid()
    {
        if (_sensor == null) return null;

        Boid closest = null;
        float bestDistance = float.MaxValue;

        foreach (Boid boid in Boids)
        {
            if (boid == null || !boid.IsAlive || boid.IsGathered) continue;
            if (!_sensor.CanSee(boid.transform)) continue;

            float distance = (boid.transform.position - transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = boid;
        }

        return closest;
    }

    public Boid GetClosestVisibleDeadBoid()
    {
        if (_sensor == null) return null;

        Boid closest = null;
        float bestDistance = float.MaxValue;

        foreach (Boid boid in Boids)
        {
            if (boid == null || boid.IsAlive || boid.IsGathered) continue;
            if (!_sensor.CanSee(boid.transform)) continue;

            float distance = (boid.transform.position - transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = boid;
        }

        return closest;
    }

    public SafeZone FindClosestSafeZone()
    {
        Graph graph = Graph.Current;
        if (graph == null || _zoneManager == null) return null;

        Node start = graph.GetClosestNode(transform.position);
        if (start == null) return null;

        SafeZone closest = null;
        float bestLength = float.MaxValue;

        foreach (SafeZone zone in _zoneManager.SafeZones)
        {
            if (zone == null) continue;

            Node target = graph.GetClosestNode(zone.Center, node => node.IsSafeZone);
            if (target == null) continue;

            List<Node> path = Pathfinding.AStar(start, target);
            if (path.Count == 0) continue;

            float length = Pathfinding.GetPathLength(path);
            if (length >= bestLength) continue;

            bestLength = length;
            closest = zone;
        }

        return closest;
    }

    public bool NavigateToNode(Node target, float speed)
    {
        if (_navAgent == null || target == null || Graph.Current == null) return false;

        Node start = Graph.Current.GetClosestNode(transform.position);
        if (start == null) return false;

        List<Node> path = Pathfinding.AStar(start, target);
        if (path.Count == 0) return false;

        _navAgent.SetPath(path, speed);
        return true;
    }

    public bool NavigateTo(Vector3 worldPosition, float speed)
    {
        if (Graph.Current == null) return false;
        return NavigateToNode(Graph.Current.GetClosestNode(worldPosition), speed);
    }

    public bool NavigateToRandomCotoNode(float speed)
    {
        if (Graph.Current == null) return false;
        return NavigateToNode(Graph.Current.GetRandomNode(NodeZone.HuntingGround), speed);
    }

    public Vector3 GetPredictedPosition(Boid target, float speed)
    {
        Vector3 targetPosition = target.transform.position;
        float distance = Vector3.Distance(transform.position, targetPosition);
        float denominator = speed + target.Velocity.magnitude;
        float predictedTime = denominator > 0.01f ? distance / denominator : 0f;
        return targetPosition + target.Velocity * predictedTime;
    }

    public void MoveDirect(Vector3 target, float speed, float stopDistance = 0.1f)
    {
        if (_navAgent != null) _navAgent.MoveDirect(target, speed, stopDistance);
    }

    public void Stop()
    {
        if (_navAgent != null) _navAgent.Stop();
    }

    public void SetAttackTarget(Boid target) => _attackTarget = target;
    public void ClearAttackTarget() => _attackTarget = null;
    public void SetGatherTarget(Boid target) => _gatherTarget = target;
    public void ClearGatherTarget() => _gatherTarget = null;
    public void SetSelectedSafeZone(SafeZone zone) => _selectedSafeZone = zone;
    public void ClearSafeZone() => _selectedSafeZone = null;

    public void ClearTargets()
    {
        _attackTarget = null;
        _gatherTarget = null;
    }

    public void ResetMeleeCooldown() => _nextMeleeTime = Time.time + _meleeCooldown;
    public void ResetRangedCooldown() => _nextRangedTime = Time.time + _rangedCooldown;

    public bool CanAttack(Boid target)
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= _meleeAttackRadius) return MeleeReady;
        if (distance <= _rangeAttackRadius) return RangedReady;

        return false;
    }

    public void RegisterGather()
    {
        _gathers++;
    }

    public void TrySpawnInterestPoint()
    {
        if (_interestSpawner != null) _interestSpawner.TrySpawn();
    }

    public void Capture()
    {
        if (_isDead) return;
        _fsm.ChangeState(HunterStateId.Dead);
    }

    public void OnDeath()
    {
        _isDead = true;
        _attackTarget = null;
        _gatherTarget = null;
        _selectedSafeZone = null;

        if (_navAgent != null) _navAgent.Stop();
        if (_visual != null) _visual.SetAttacking(false);
        if (_visual != null) _visual.SetFleeing(false);
        SetVisible(false);
        SetActionProgress(0f, "Dead");
    }

    public void RespawnAtStart()
    {
        if (_navAgent != null) _navAgent.Stop();

        if (_startPoint != null) transform.position = _startPoint.position;

        _lastPosition = transform.position;
        Velocity = Vector3.zero;

        _isDead = false;
        _gathers = 0;
        _nextMeleeTime = 0f;
        _nextRangedTime = 0f;
        _attackTarget = null;
        _gatherTarget = null;
        _selectedSafeZone = null;

        SetVisible(true);
        if (_visual != null) _visual.SetAttacking(false);
        if (_visual != null) _visual.SetFleeing(false);
        SetActionProgress(0f, "");
    }

    public void SetVisible(bool visible)
    {
        if (_visual != null) _visual.SetVisible(visible);
        if (_collider != null) _collider.enabled = visible;
        if (_sensor != null) _sensor.enabled = visible;
        if (_navAgent != null) _navAgent.enabled = visible;
    }
}
