using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class BoidManager : MonoBehaviour
{
    [SerializeField] private InterestPointSpawner _interestSpawner;

    private readonly List<Boid> _boids = new();
    private readonly List<Agent> _agents = new();

    public IReadOnlyList<Boid> Boids => _boids;
    public IReadOnlyList<Agent> Agents => _agents;
    public InterestPointSpawner InterestSpawner => _interestSpawner;

    private void Awake()
    {
        Collect();
    }

    public void Collect()
    {
        _boids.Clear();
        _agents.Clear();

        GetComponentsInChildren(true, _boids);
        GetComponentsInChildren(true, _agents);
    }

    public InterestPoint FindClosestInterestPoint(Vector3 position, float radius)
    {
        return _interestSpawner != null ? _interestSpawner.FindClosest(position, radius) : null;
    }
}
