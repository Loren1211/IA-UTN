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

    [Header("Flocking values")]
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private float _visionRadius = 6f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1f;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Agent targetAgent;

    private static readonly List<Agent> _allAgents = new();
    private Vector3 _velocity;

    public Vector3 Velocity => _velocity;

    private void Awake()
    {
        _allAgents.Add(this);
    }

    private void Start()
    {
        Vector3 randomVector = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = randomVector.normalized * _maxSpeed;
    }

    private void Update()
    {
        Vector3 acceleration = Vector3.zero;

        switch (_currentMode)
        {
            case SteeringModes.Flocking:
                acceleration = CalculateFlocking();
                break;
            case SteeringModes.Pursuit when targetAgent != null:
                acceleration = CalculatePursuit(targetAgent);
                break;
            case SteeringModes.Evade when targetAgent != null:
                acceleration = CalculateEvade(targetAgent);
                break;
            case SteeringModes.Seek:
            case SteeringModes.Flee:
            case SteeringModes.Arrive:
                if (target != null) acceleration = GetCurrentSteeringMode();
                break;
        }

        _velocity += acceleration * Time.deltaTime;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        Vector3 nextPosition = transform.position + _velocity * Time.deltaTime;
        Vector3 clampedPosition = Bounds.Instance != null
            ? Bounds.Instance.ClampPosition(nextPosition)
            : nextPosition;

        if (!Mathf.Approximately(clampedPosition.x, nextPosition.x)) _velocity.x *= -0.5f;
        if (!Mathf.Approximately(clampedPosition.z, nextPosition.z)) _velocity.z *= -0.5f;

        transform.position = clampedPosition;

        if (_velocity.sqrMagnitude > 0.0001f) transform.forward = _velocity;
    }

    public void ResetAt(Vector3 position)
    {
        transform.position = position;

        Vector3 randomVector = new(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = randomVector.normalized * _maxSpeed;
    }

    #region Flocking

    private Vector3 CalculateFlocking()
    {
        return CalculateSeparation(_allAgents, _separationRadius) * _separationWeight
             + CalculateAlignment(_allAgents, _viewRadius) * _alignmentWeight
             + CalculateCohesion(_allAgents, _viewRadius) * _cohesionWeight;
    }

    private Vector3 CalculateCohesion(List<Agent> agents, float radius)
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

    private Vector3 CalculateSeparation(List<Agent> agents, float radius)
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

    private Vector3 CalculateAlignment(List<Agent> agents, float radius)
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

    private Vector3 CalculateEvade(Agent targetToEvade) => CalculateFlee(GetFuturePosition(targetToEvade));

    private Vector3 GetFuturePosition(Agent targetToPredict)
    {
        float distanceToTarget = (targetToPredict.transform.position - transform.position).magnitude;
        float denominator = _maxSpeed + targetToPredict.Velocity.magnitude;
        float predictedTime = denominator > 0.01f ? distanceToTarget / denominator : 0f;
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
        float speed = _maxSpeed;
        float distance = direction.magnitude;

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
        return Vector3.ClampMagnitude(desired - _velocity, _maxForce);
    }

    #endregion

    private void OnDestroy()
    {
        _allAgents.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

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
