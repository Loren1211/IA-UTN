using System.Collections.Generic;
using UnityEngine;

public class NavAgent : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _nodeReachDistance = 0.2f;
    [SerializeField] private float _rotationSpeed = 12f;
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private float _pathClearance = 0.5f;

    private const float MinPathDistance = 0.01f;
    private const float MinStopDistance = 0.05f;
    private const float DirectionEpsilon = 0.0001f;

    private readonly List<Node> _path = new();
    private bool _hasDirectTarget;
    private Vector3 _directTarget;
    private float _directStopDistance = 0.1f;

    private void Awake()
    {
        if (_pathClearance <= 0f) _pathClearance = 0.5f;
    }

    public bool IsMoving => _path.Count > 0 || _hasDirectTarget;
    public bool IsFollowingPath => _path.Count > 0;
    public IReadOnlyList<Node> CurrentPath => _path;
    public float Speed => _speed;

    public bool TryGetDestination(out Vector3 destination)
    {
        if (_path.Count > 0)
        {
            destination = _path[_path.Count - 1].transform.position;
            return true;
        }

        if (_hasDirectTarget)
        {
            destination = _directTarget;
            return true;
        }

        destination = transform.position;
        return false;
    }

    public void SetSpeed(float speed) => _speed = Mathf.Max(0.1f, speed);

    public void SetObstacleMask(LayerMask mask) => _wallMask = mask;

    public void SetPath(List<Node> path, float speed)
    {
        _path.Clear();
        _hasDirectTarget = false;
        SetSpeed(speed);

        if (path == null) return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null) _path.Add(path[i]);
        }

        if (_path.Count > 0 &&
            SqrDistance(transform.position, _path[0].transform.position) <=
            _nodeReachDistance * _nodeReachDistance)
        {
            _path.RemoveAt(0);
        }

        SmoothPath();
    }

    private void SmoothPath()
    {
        if (_wallMask == 0 || _path.Count <= 1) return;

        List<Node> smoothed = new();
        Vector3 current = transform.position;
        int index = 0;

        while (index < _path.Count)
        {
            int selected = index;

            for (int i = _path.Count - 1; i >= index; i--)
            {
                if (!HasClearPath(current, _path[i].transform.position)) continue;
                selected = i;
                break;
            }

            smoothed.Add(_path[selected]);
            current = _path[selected].transform.position;
            index = selected + 1;
        }

        _path.Clear();
        _path.AddRange(smoothed);
    }

    private bool HasClearPath(Vector3 from, Vector3 to)
    {
        from.y = transform.position.y;
        to.y = transform.position.y;

        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance <= MinPathDistance) return true;

        return !Physics.SphereCast(from, _pathClearance, direction / distance, out _,
                                   distance, _wallMask, QueryTriggerInteraction.Ignore);
    }

    public void MoveDirect(Vector3 target, float speed, float stopDistance = 0.1f)
    {
        _path.Clear();
        _directTarget = target;
        _directStopDistance = Mathf.Max(MinStopDistance, stopDistance);
        _hasDirectTarget = true;
        SetSpeed(speed);
    }

    public void Stop()
    {
        _path.Clear();
        _hasDirectTarget = false;
    }

    private void Update()
    {
        if (_path.Count > 0)
        {
            Vector3 target = _path[0].transform.position;
            target.y = transform.position.y;

            if (SqrDistance(transform.position, target) <= _nodeReachDistance * _nodeReachDistance)
            {
                _path.RemoveAt(0);
                return;
            }

            Move(target);
            return;
        }

        if (!_hasDirectTarget) return;

        Vector3 directTarget = _directTarget;
        directTarget.y = transform.position.y;

        if (SqrDistance(transform.position, directTarget) <= _directStopDistance * _directStopDistance)
        {
            _hasDirectTarget = false;
            return;
        }

        Move(directTarget);
    }

    private void Move(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < DirectionEpsilon) return;

        transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        transform.forward = Vector3.Slerp(transform.forward, direction.normalized,
            Mathf.Clamp01(_rotationSpeed * Time.deltaTime));
    }

    private static float SqrDistance(Vector3 a, Vector3 b)
    {
        Vector3 difference = a - b;
        difference.y = 0f;
        return difference.sqrMagnitude;
    }

    private void OnDrawGizmos()
    {
        if (_path.Count == 0) return;

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Vector3 previous = transform.position;

        foreach (Node node in _path)
        {
            if (node == null) continue;
            Vector3 point = node.transform.position;
            point.y = transform.position.y;
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawSphere(point, 0.1f);
            previous = point;
        }
    }
}
