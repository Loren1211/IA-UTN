using UnityEngine;

public class SensorAgent : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _viewRadius = 8f;
    [SerializeField, Range(1f, 360f)] private float _viewAngle = 100f;
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private bool _drawGizmos = true;

    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;
    public LayerMask WallMask => _wallMask;

    public void Configure(float viewRadius, float viewAngle, LayerMask wallMask)
    {
        _viewRadius = Mathf.Max(0.1f, viewRadius);
        _viewAngle = Mathf.Clamp(viewAngle, 1f, 360f);
        _wallMask = wallMask;
    }

    public bool CanSee(Transform target)
    {
        return target != null && target.gameObject.activeInHierarchy && InFieldOfView(target.position);
    }

    public bool CanSee(Vector3 targetPosition)
    {
        return InFieldOfView(targetPosition);
    }

    public bool HasLineOfSight(Vector3 targetPosition)
    {
        return InLineOfSight(targetPosition);
    }

    private bool InRange(Vector3 targetPosition)
    {
        return Vector3.Distance(targetPosition, transform.position) <= _viewRadius;
    }

    private bool InViewAngle(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        return Vector3.Angle(transform.forward, direction.normalized) <= _viewAngle * 0.5f;
    }

    private bool InLineOfSight(Vector3 targetPosition)
    {
        return !Physics.Linecast(transform.position, targetPosition, _wallMask, QueryTriggerInteraction.Ignore);
    }

    private bool InFieldOfView(Vector3 targetPosition)
    {
        return InViewAngle(targetPosition) && InRange(targetPosition) && InLineOfSight(targetPosition);
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _viewRadius);

        if (_viewAngle >= 359f)
        {
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * _viewRadius);
            return;
        }

        Vector3 origin = transform.position;
        float halfAngle = _viewAngle * 0.5f;
        Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, transform.up) * transform.forward;
        Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, transform.up) * transform.forward;

        Gizmos.DrawLine(origin, origin + leftDirection * _viewRadius);
        Gizmos.DrawLine(origin, origin + rightDirection * _viewRadius);
    }
}
