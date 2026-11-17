using System.Collections.Generic;
using UnityEngine;

public enum WolfStateId
{
    Patrol,
    Alert,
    Chase,
    Search
}

[RequireComponent(typeof(NavAgent), typeof(SensorAgent))]
public class WolfAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 3.2f;
    [SerializeField] private float _chaseSpeed = 4.2f;
    [SerializeField] private float _searchSpeed = 3.6f;
    [SerializeField] private float _turnSpeed = 120f;

    [Header("Patrol")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointReachDistance = 1f;

    [Header("Perception")]
    [SerializeField] private ThreatSensor _threatSensor;

    [Header("Combat")]
    [SerializeField] private float _captureRadius = 1.3f;
    [SerializeField] private float _interceptLeadMax = 1f;

    [Header("States")]
    [SerializeField] private float _alertDuration = 0.8f;
    [SerializeField] private float _searchDuration = 4f;

    [Header("References")]
    [SerializeField] private WolfPack _pack;
    [SerializeField] private ZoneManager _zoneManager;
    [SerializeField] private NavAgent _navAgent;
    [SerializeField] private SensorAgent _sensor;
    [SerializeField] private WolfVisual _visual;
    [SerializeField] private EntityStatus _status;
    [SerializeField] private Collider _collider;

    private readonly FiniteStateMachine<WolfStateId> _fsm = new();
    private Transform _hunter;
    private Vector3 _lastKnownPosition;
    private bool _hasLastKnown;
    private bool _started;
    private bool _raisingAlert;
    private int _waypointIndex;
    private float _nextAlertTime;
    private WolfStateId _lastVisualState = (WolfStateId)(-1);
    private bool _lastVisualSees;

    public float PatrolSpeed => _patrolSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float SearchSpeed => _searchSpeed;
    public float TurnSpeed => _turnSpeed;
    public float CaptureRadius => _captureRadius;
    public float InterceptLeadMax => _interceptLeadMax;
    public float AlertDuration => _alertDuration;
    public float SearchDuration => _searchDuration;
    public float WaypointReachDistance => _waypointReachDistance;

    public WolfStateId CurrentState => _fsm.CurrentState;
    public string CurrentStateName => _fsm.CurrentStateName;
    public NavAgent NavAgent => _navAgent;
    public SensorAgent Sensor => _sensor;
    public WolfPack Pack => _pack;
    public ZoneManager ZoneManager => _zoneManager;
    public Transform CurrentTarget => _hunter;
    public bool HasLastKnown => _hasLastKnown;
    public Vector3 LastKnownPosition => _lastKnownPosition;
    public bool IsHunting => CurrentState != WolfStateId.Patrol;
    public bool IsSeeingTarget => _hunter != null;

    private void Awake()
    {
        if (_navAgent == null) _navAgent = GetComponent<NavAgent>();
        if (_sensor == null) _sensor = GetComponent<SensorAgent>();
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_visual == null) _visual = GetComponent<WolfVisual>();
        if (_status == null) _status = GetComponent<EntityStatus>();
        if (_threatSensor == null) _threatSensor = GetComponent<ThreatSensor>();

        if (_patrolSpeed <= 0f) _patrolSpeed = 3.2f;
        if (_chaseSpeed <= 0f) _chaseSpeed = 4.2f;
        if (_searchSpeed <= 0f) _searchSpeed = 3.6f;
        if (_waypointReachDistance <= 0f) _waypointReachDistance = 1f;
        if (_captureRadius <= 0f) _captureRadius = 1.3f;
        if (_interceptLeadMax <= 0f) _interceptLeadMax = 1f;
        if (_alertDuration <= 0f) _alertDuration = 0.8f;
        if (_searchDuration <= 0f) _searchDuration = 4f;

        _fsm.AddState(WolfStateId.Patrol, new WolfPatrolState(_fsm, this));
        _fsm.AddState(WolfStateId.Alert, new WolfAlertState(_fsm, this));
        _fsm.AddState(WolfStateId.Chase, new WolfChaseState(_fsm, this));
        _fsm.AddState(WolfStateId.Search, new WolfSearchState(_fsm, this));

        _fsm.StateChanged += OnStateChanged;
    }

    private void OnEnable()
    {
        if (_pack == null) return;

        _pack.AlertRaised += OnPackAlert;
        _pack.AlertCleared += OnPackCleared;
    }

    private void OnDisable()
    {
        if (_pack == null) return;

        _pack.AlertRaised -= OnPackAlert;
        _pack.AlertCleared -= OnPackCleared;
    }

    private void Start()
    {
        _started = true;
        _fsm.ChangeState(WolfStateId.Patrol);
    }

    private void Update()
    {
        _fsm.Update();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_visual == null) return;
        if (_lastVisualState == CurrentState && _lastVisualSees == IsSeeingTarget) return;

        _lastVisualState = CurrentState;
        _lastVisualSees = IsSeeingTarget;
        _visual.SetState(CurrentState, IsSeeingTarget);
    }

    private void OnStateChanged(WolfStateId state)
    {
        if (_status != null) _status.SetState(state.ToString());
    }

    private void OnPackAlert(Vector3 position)
    {
        if (!_started) return;
        if (_zoneManager != null && _zoneManager.IsInsideSafeZone(position)) return;

        _lastKnownPosition = position;
        _hasLastKnown = true;

        if (_raisingAlert) return;

        if (_fsm.CurrentState == WolfStateId.Patrol || _fsm.CurrentState == WolfStateId.Search)
            _fsm.ChangeState(WolfStateId.Alert);
    }

    private void OnPackCleared()
    {
        _hunter = null;
        _hasLastKnown = false;

        if (_fsm.CurrentState != WolfStateId.Patrol)
            _fsm.ChangeState(WolfStateId.Patrol);
    }

    public bool UpdateDetection()
    {
        Transform candidate = _threatSensor != null ? _threatSensor.Threat : null;

        if (candidate == null || _sensor == null || !_sensor.CanSee(candidate))
        {
            _hunter = null;
            return false;
        }

        if (_zoneManager != null && _zoneManager.IsInsideSafeZone(candidate.position))
        {
            _hunter = null;
            return false;
        }

        _hunter = candidate;
        return true;
    }

    public bool TargetInSafeZone()
    {
        if (_threatSensor == null || _threatSensor.Threat == null || _zoneManager == null) return false;

        return _zoneManager.IsInsideSafeZone(_threatSensor.Threat.position);
    }

    public void ReportDetection(Vector3 position)
    {
        _lastKnownPosition = position;
        _hasLastKnown = true;

        if (_pack == null || Time.time < _nextAlertTime) return;

        _nextAlertTime = Time.time + 1f;

        _raisingAlert = true;
        _pack.RaiseAlert(position);
        _raisingAlert = false;
    }

    public void RememberPosition(Vector3 position)
    {
        if (_zoneManager != null && _zoneManager.IsInsideSafeZone(position)) return;

        _lastKnownPosition = position;
        _hasLastKnown = true;
    }

    public void AbandonChase()
    {
        _hunter = null;
        _hasLastKnown = false;

        if (_pack != null) _pack.ClearAlert();
        if (_fsm.CurrentState != WolfStateId.Patrol) _fsm.ChangeState(WolfStateId.Patrol);
    }

    public void CaptureHunter()
    {
        HunterAgent hunter = _hunter != null ? _hunter.GetComponentInParent<HunterAgent>() : null;
        if (hunter == null || hunter.IsDead) return;
        if (_zoneManager != null && _zoneManager.IsInsideSafeZone(hunter.transform.position)) return;

        hunter.Capture();
        AbandonChase();
    }

    public bool NavigateTo(Vector3 worldPosition, float speed)
    {
        Graph graph = Graph.Current;
        if (graph == null || _navAgent == null) return false;

        Node start = graph.GetClosestNode(transform.position, true);
        Node end = graph.GetClosestNode(worldPosition, true);
        if (start == null || end == null) return false;

        List<Node> path = Pathfinding.AStar(start, end, true);
        if (path.Count == 0) return false;

        _navAgent.SetPath(path, speed);
        return true;
    }

    public void MoveDirect(Vector3 worldPosition, float speed, float stopDistance = 0.1f)
    {
        if (_navAgent != null) _navAgent.MoveDirect(worldPosition, speed, stopDistance);
    }

    public void Stop()
    {
        if (_navAgent != null) _navAgent.Stop();
    }

    public void SetAction(string action, float progress = 0f)
    {
        if (_status != null) _status.SetAction(action, progress);
    }

    public void ForgetTarget()
    {
        _hunter = null;
        _hasLastKnown = false;
    }

    public void RotateInPlace(float degreesPerSecond)
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }

    public bool HasLineOfSight(Vector3 worldPosition)
    {
        return _sensor != null && _sensor.HasLineOfSight(worldPosition);
    }

    public Transform CurrentWaypoint
    {
        get
        {
            if (_waypoints == null || _waypoints.Length == 0) return null;
            return _waypoints[_waypointIndex];
        }
    }

    public void AdvanceWaypoint()
    {
        if (_waypoints == null || _waypoints.Length == 0) return;
        _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
    }

    public void SetWaypoints(Transform[] waypoints)
    {
        _waypoints = waypoints;
        _waypointIndex = 0;
    }
}
