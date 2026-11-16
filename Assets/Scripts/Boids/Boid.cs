using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Agent _agent;
    [SerializeField] private BoidVisual _visual;
    [SerializeField] private EntityStatus _status;
    [SerializeField] private ThreatSensor _threatSensor;

    [Header("Health")]
    [SerializeField] private float _maxHealth = 3f;
    [SerializeField] private float _respawnTime = 4f;

    [Header("Interest")]
    [SerializeField] private float _interestRadius = 10f;
    [SerializeField] private float _consumeRadius = 1.2f;
    [SerializeField] private float _consumeRate = 0.6f;

    [Header("Threat")]
    [SerializeField] private float _threatRadius = 6f;

    private bool _isAlive = true;
    private bool _isGathered;
    private bool _isFleeing;
    private float _health;
    private float _respawnTimer;
    private BoidManager _boidManager;

    public bool IsAlive => _isAlive;
    public bool IsGathered => _isGathered;
    public bool IsFleeing => _isFleeing;
    public bool IsInterested { get; private set; }
    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public Vector3 Velocity => _agent != null ? _agent.Velocity : Vector3.zero;

    private void Awake()
    {
        if (_agent == null) _agent = GetComponent<Agent>();
        if (_visual == null) _visual = GetComponent<BoidVisual>();
        if (_status == null) _status = GetComponent<EntityStatus>();
        if (_threatSensor == null) _threatSensor = GetComponentInChildren<ThreatSensor>();
        _boidManager = GetComponentInParent<BoidManager>();

        if (_maxHealth <= 0f) _maxHealth = 3f;
        if (_interestRadius <= 0f) _interestRadius = 10f;
        if (_consumeRadius <= 0f) _consumeRadius = 1.2f;
        if (_consumeRate <= 0f) _consumeRate = 0.6f;
        if (_threatRadius <= 0f) _threatRadius = 6f;

        _health = _maxHealth;
        UpdateStatus();
    }

    public void TakeDamage(float damage)
    {
        if (!_isAlive || damage <= 0f) return;

        _health -= damage;

        if (_health <= 0f)
        {
            _health = 0f;
            Kill();
            return;
        }

        if (_visual != null) _visual.SetHealth(_health / _maxHealth);
    }

    public void Kill()
    {
        if (!_isAlive) return;

        _isAlive = false;
        _health = 0f;
        _isFleeing = false;
        IsInterested = false;

        if (_agent != null) _agent.enabled = false;
        if (_visual != null) _visual.SetDead();

        UpdateStatus();
    }

    public void Gather()
    {
        if (_isAlive || _isGathered) return;

        _isGathered = true;
        _respawnTimer = _respawnTime;

        if (_visual != null) _visual.SetVisible(false);

        UpdateStatus();
    }

    private void Update()
    {
        if (_isGathered)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) Respawn();
            return;
        }

        if (!_isAlive) return;

        UpdateInterestAndThreat();
        UpdateStatus();
    }

    private void UpdateInterestAndThreat()
    {
        Transform threat = _threatSensor != null ? _threatSensor.Threat : null;

        if (threat != null)
        {
            Vector3 toThreat = threat.position - transform.position;
            toThreat.y = 0f;

            if (!_isFleeing && toThreat.sqrMagnitude <= _threatRadius * _threatRadius)
                _isFleeing = true;
        }
        else
        {
            _isFleeing = false;
        }

        _agent.SetWrapAtBounds(_isFleeing);

        if (_isFleeing && threat != null)
        {
            IsInterested = false;
            _agent.SetFleeTarget(threat);
            return;
        }

        InterestPoint point = _boidManager != null
            ? _boidManager.FindClosestInterestPoint(transform.position, _interestRadius)
            : null;

        if (point == null)
        {
            IsInterested = false;
            _agent.SetFlockingMode();
            return;
        }

        IsInterested = true;
        _agent.SetArriveTarget(point.transform);

        Vector3 toPoint = point.transform.position - transform.position;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude <= _consumeRadius * _consumeRadius)
            point.Consume(Time.deltaTime * _consumeRate);
    }

    private void UpdateStatus()
    {
        if (_status == null) return;

        if (_isGathered) _status.SetState("Gathered");
        else if (!_isAlive) _status.SetState("Dead");
        else if (_isFleeing) _status.SetState("Fleeing");
        else if (IsInterested) _status.SetState("Interested");
        else _status.SetState("Flocking");
    }

    public void Respawn()
    {
        Vector3 position = global::Bounds.Instance != null
            ? global::Bounds.Instance.RandomPointInside()
            : transform.position;
        position.y = transform.position.y;

        _isAlive = true;
        _isGathered = false;
        _isFleeing = false;
        IsInterested = false;
        _health = _maxHealth;
        transform.position = position;

        if (_visual != null)
        {
            _visual.SetVisible(true);
            _visual.SetHealth(1f);
        }

        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.ResetAt(position);
        }

        UpdateStatus();
    }
}
