using System.Collections.Generic;
using UnityEngine;

public class InterestPointSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private global::Bounds _bounds;
    [SerializeField] private Transform _baitsRoot;

    [Header("Spawning")]
    [SerializeField] private int _maxActive = 5;
    [SerializeField] private float _size = 0.4f;
    [SerializeField] private float _initialDelay = 5f;
    [SerializeField] private float _spawnInterval = 6f;
    [SerializeField] private float _baitLife = 10f;

    private readonly List<InterestPoint> _activePoints = new();
    private Material _material;
    private float _nextSpawnTime;

    private const float SpawnHeightOffset = 0.05f;

    private void Awake()
    {
        if (_bounds == null) _bounds = global::Bounds.Instance;
        if (_maxActive <= 0) _maxActive = 5;
        if (_size <= 0f) _size = 0.4f;
        if (_initialDelay <= 0f) _initialDelay = 5f;
        if (_spawnInterval <= 0f) _spawnInterval = 6f;
        if (_baitLife <= 0f) _baitLife = 10f;

        if (_baitsRoot == null)
        {
            GameObject root = new("Traps");
            _baitsRoot = root.transform;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _material = new Material(shader);

        Color color = new(0.55f, 0.16f, 0.10f);
        if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", color);
        if (_material.HasProperty("_Color")) _material.SetColor("_Color", color);

        _nextSpawnTime = Time.time + _initialDelay;
    }

    public bool TrySpawn()
    {
        CleanupActive();

        if (_bounds == null) return false;
        if (_activePoints.Count >= _maxActive) return false;
        if (Time.time < _nextSpawnTime) return false;

        Vector3 position = _bounds.RandomPointInside();
        position.y = SpawnHeightOffset + _size * 0.5f;

        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
        point.name = $"Bait {_activePoints.Count + 1}";
        point.transform.SetParent(_baitsRoot, false);
        point.transform.position = position;
        point.transform.localScale = Vector3.one * _size;

        Collider collider = point.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        MeshRenderer renderer = point.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;

        InterestPoint interestPoint = point.AddComponent<InterestPoint>();
        interestPoint.Configure(_baitLife);
        _activePoints.Add(interestPoint);
        _nextSpawnTime = Time.time + _spawnInterval;

        return true;
    }

    public InterestPoint FindClosest(Vector3 position, float radius)
    {
        InterestPoint closest = null;
        float bestDistance = radius * radius;

        foreach (InterestPoint point in _activePoints)
        {
            if (point == null || !point.IsActive) continue;

            Vector3 difference = point.transform.position - position;
            difference.y = 0f;

            float distance = difference.sqrMagnitude;
            if (distance > bestDistance) continue;

            bestDistance = distance;
            closest = point;
        }

        return closest;
    }

    private void CleanupActive()
    {
        _activePoints.RemoveAll(point => point == null);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
