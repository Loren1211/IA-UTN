using UnityEngine;

[DefaultExecutionOrder(-300)]
public class LevelContext : MonoBehaviour
{
    [SerializeField] private ZoneManager _zoneManager;
    [SerializeField] private WolfPack _wolfPack;
    [SerializeField] private BoidManager _boidManager;
    [SerializeField] private InterestPointSpawner _interestSpawner;
    [SerializeField] private global::Bounds _bounds;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _exitPoint;

    public static LevelContext Instance { get; private set; }

    public ZoneManager ZoneManager => _zoneManager;
    public WolfPack WolfPack => _wolfPack;
    public BoidManager BoidManager => _boidManager;
    public InterestPointSpawner InterestPointSpawner => _interestSpawner;
    public global::Bounds Bounds => _bounds;
    public Transform StartPoint => _startPoint;
    public Transform ExitPoint => _exitPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
