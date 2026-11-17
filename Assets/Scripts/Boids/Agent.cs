using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    private enum SteeringModes { Seek, Flee, Arrive, Pursuit, Evade, Flocking }

    [Header("Stats")]
    [SerializeField] private float _maxSpeed = 4f;
    [SerializeField] private float _maxForce = 8f;
    [SerializeField] private float _viewRadius = 5f;
    [SerializeField] private SteeringModes _currentMode = SteeringModes.Flocking;
    [SerializeField] private float _arriveRadius = 3f;
    [SerializeField] private float _arriveStopDistance = 0.4f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Flocking values")]
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private float _alignmentRadius = 4f;
    [SerializeField] private float _visionRadius = 6f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1f;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Agent targetAgent;

    private const float MinPredictionSpeed = 0.01f;
    private const float MinRandomDirectionSqr = 0.01f;

    private Vector3 _velocity;
    private Vector3 _externalTargetVelocity;
    private bool _wrapAtBounds;
    private BoidManager _boidManager;

    public Vector3 Velocity => _velocity;

    private IReadOnlyList<Agent> FlockAgents =>
        _boidManager != null ? _boidManager.Agents : System.Array.Empty<Agent>();

    private void Awake()
    {
        _boidManager = GetComponentInParent<BoidManager>();
    }

    private void Start()
    {
        Vector3 randomVector;
        do
        {
            randomVector = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        }
        while (randomVector.sqrMagnitude < MinRandomDirectionSqr);

        _velocity = randomVector.normalized * _maxSpeed;
    }

    private void Update()
    {
        float fixedHeight = transform.position.y;
        Vector3 steering = Vector3.zero;

        switch (_currentMode)
        {
            case SteeringModes.Flocking:
                steering = CalculateFlocking();
                break;
            case SteeringModes.Pursuit when targetAgent != null:
                steering = CalculatePursuit(targetAgent);
                break;
            case SteeringModes.Evade when target != null:
                steering = CalculateEvade(target.position, _externalTargetVelocity);
                break;
            case SteeringModes.Seek:
            case SteeringModes.Flee:
            case SteeringModes.Arrive:
                if (target != null) steering = GetCurrentSteeringMode();
                break;
        }

        if (_currentMode != SteeringModes.Flocking)
            steering += CalculateSeparation(FlockAgents, _separationRadius) * _separationWeight;

        steering.y = 0f;
        _velocity.y = 0f;
        _velocity += steering;
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        Vector3 nextPosition = transform.position + _velocity * Time.deltaTime;
        nextPosition.y = fixedHeight;

        bool wrap = _wrapAtBounds && Bounds.Instance != null;
        Vector3 boundedPosition = Bounds.Instance != null
            ? (wrap ? Bounds.Instance.CalculateBoundPosition(nextPosition)
                    : Bounds.Instance.ClampPosition(nextPosition))
            : nextPosition;
        boundedPosition.y = fixedHeight;

        if (!wrap)
        {
            if (!Mathf.Approximately(boundedPosition.x, nextPosition.x)) _velocity.x *= -0.5f;
            if (!Mathf.Approximately(boundedPosition.z, nextPosition.z)) _velocity.z *= -0.5f;
        }

        transform.position = boundedPosition;

        if (_velocity.sqrMagnitude > 0.01f)
        {
            transform.forward = Vector3.Slerp(transform.forward, _velocity.normalized,
                Mathf.Clamp01(_rotationSpeed * Time.deltaTime));
        }
    }

    public void ResetAt(Vector3 position)
    {
        transform.position = position;

        Vector3 randomVector;
        do
        {
            randomVector = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        }
        while (randomVector.sqrMagnitude < MinRandomDirectionSqr);

        _velocity = randomVector.normalized * _maxSpeed;
    }

    public void SetWrapAtBounds(bool wrap) => _wrapAtBounds = wrap;

    public void SetFlockingMode()
    {
        _currentMode = SteeringModes.Flocking;
        target = null;
    }

    public void SetArriveTarget(Transform newTarget)
    {
        _currentMode = SteeringModes.Arrive;
        target = newTarget;
    }

    public void SetEvadeTarget(Transform newTarget, Vector3 targetVelocity)
    {
        _currentMode = SteeringModes.Evade;
        target = newTarget;
        _externalTargetVelocity = targetVelocity;
    }

    #region Flocking

    private Vector3 CalculateFlocking()
    {
        return CalculateSeparation(FlockAgents, _separationRadius) * _separationWeight
             + CalculateAlignment(FlockAgents, _alignmentRadius) * _alignmentWeight
             + CalculateCohesion(FlockAgents, _viewRadius) * _cohesionWeight;
    }

    private Vector3 CalculateCohesion(IReadOnlyList<Agent> agents, float radius)
    {
        Vector3 desiredPosition = default;
        int count = 0;

        foreach (Agent item in agents)
        {
            if (item == this || item == null || !item.isActiveAndEnabled) continue;
            if (!InRange(item.transform.position, radius)) continue;

            desiredPosition += item.transform.position;
            count++;
        }

        if (count == 0) return Vector3.zero;
        desiredPosition /= count;

        return CalculateSeek(desiredPosition);
    }

    private Vector3 CalculateSeparation(IReadOnlyList<Agent> agents, float radius)
    {
        Vector3 desired = default;
        int count = 0;

        foreach (Agent item in agents)
        {
            if (item == this || item == null || !item.isActiveAndEnabled) continue;
            if (!InRange(item.transform.position, radius)) continue;

            desired += item.transform.position - transform.position;
            count++;
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(-desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment(IReadOnlyList<Agent> agents, float radius)
    {
        Vector3 desired = default;
        int count = 0;

        foreach (Agent item in agents)
        {
            if (item == this || item == null || !item.isActiveAndEnabled) continue;
            if (!InRange(item.transform.position, radius)) continue;

            desired += item.Velocity;
            count++;
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(desired.normalized * _maxSpeed);
    }

    private bool InRange(Vector3 position, float radius)
    {
        float effectiveRadius = Mathf.Min(radius, _visionRadius);
        return (position - transform.position).sqrMagnitude <= effectiveRadius * effectiveRadius;
    }

    #endregion

    #region Steering

    private Vector3 CalculatePursuit(Agent targetToPursue) => CalculateSeek(GetFuturePosition(targetToPursue));

    private Vector3 CalculateEvade(Vector3 targetPosition, Vector3 targetVelocity)
    {
        float distanceToTarget = (targetPosition - transform.position).magnitude;
        float denominator = _maxSpeed + targetVelocity.magnitude;
        float predictedTime = denominator > MinPredictionSpeed ? distanceToTarget / denominator : 0f;
        return CalculateFlee(targetPosition + targetVelocity * predictedTime);
    }

    private Vector3 GetFuturePosition(Agent targetToPredict)
    {
        float distanceToTarget = (targetToPredict.transform.position - transform.position).magnitude;
        float denominator = _maxSpeed + targetToPredict.Velocity.magnitude;
        float predictedTime = denominator > MinPredictionSpeed ? distanceToTarget / denominator : 0f;
        return targetToPredict.transform.position + targetToPredict.Velocity * predictedTime;
    }

    private Vector3 CalculateSeek(Vector3 targetPosition)
    {
        Vector3 desired = (targetPosition - transform.position).normalized * _maxSpeed;
        return CalculateSteering(desired);
    }

    private Vector3 CalculateFlee(Vector3 targetPosition)
    {
        Vector3 desired = (targetPosition - transform.position).normalized * _maxSpeed;
        return CalculateSteering(-desired);
    }

    private Vector3 CalculateArrive(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;
        if (distance <= _arriveStopDistance) return CalculateSteering(Vector3.zero);

        float speed = _maxSpeed;

        if (distance <= _arriveRadius)
        {
            speed *= distance / Mathf.Max(0.01f, _arriveRadius);
        }

        return CalculateSteering(direction.normalized * speed);
    }

    private Vector3 GetCurrentSteeringMode()
    {
        return _currentMode switch
        {
            SteeringModes.Seek => CalculateSeek(target.position),
            SteeringModes.Flee => CalculateFlee(target.position),
            SteeringModes.Arrive => CalculateArrive(target.position),
            _ => Vector3.zero
        };
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        return Vector3.ClampMagnitude(desired - _velocity, _maxForce * Time.deltaTime);
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _alignmentRadius);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _viewRadius);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _visionRadius);

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);
        }

    }
}
