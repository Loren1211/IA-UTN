using UnityEngine;

public class ThreatSensor : MonoBehaviour
{
    [SerializeField] private LayerMask _threatMask;
    [SerializeField] private float _releaseRadius = 8f;
    [SerializeField] private float _checkInterval = 0.2f;

    private readonly Collider[] _buffer = new Collider[16];
    private IThreatTarget _threatTarget;
    private float _nextCheckTime;

    public IThreatTarget ThreatTarget => _threatTarget;
    public Transform Threat => _threatTarget != null ? _threatTarget.ThreatTransform : null;
    public bool HasThreat => _threatTarget != null;
    public float ReleaseRadius => _releaseRadius;

    private void Awake()
    {
        if (_releaseRadius <= 0f) _releaseRadius = 8f;
        if (_checkInterval <= 0f) _checkInterval = 0.2f;
    }

    public void Configure(LayerMask threatMask, float releaseRadius)
    {
        _threatMask = threatMask;
        _releaseRadius = Mathf.Max(0.1f, releaseRadius);
    }

    private void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + _checkInterval;

        IThreatTarget closest = null;
        float bestDistance = _releaseRadius * _releaseRadius;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _releaseRadius, _buffer,
                                                  _threatMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider candidate = _buffer[i];
            if (candidate == null) continue;

            IThreatTarget target = candidate.GetComponentInParent<IThreatTarget>();
            if (target == null || !target.IsThreatAlive) continue;

            Transform targetTransform = target.ThreatTransform;
            Vector3 difference = targetTransform.position - transform.position;
            difference.y = 0f;

            float distance = difference.sqrMagnitude;
            if (distance > bestDistance) continue;

            bestDistance = distance;
            closest = target;
        }

        _threatTarget = closest;
    }
}
