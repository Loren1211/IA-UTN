using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class Graph : MonoBehaviour
{
    [Header("Grid setup")]
    [SerializeField] private int _gridWidth = 34;
    [SerializeField] private int _gridHeight = 22;
    [SerializeField] private float _nodeOffset = 2f;
    [SerializeField] private Vector3 _gridOrigin = new(-34f, 0f, -22f);

    [Header("Checks")]
    [SerializeField] private float _wallCheckRadius = 0.45f;
    [SerializeField] private float _floorRayHeight = 3f;

    [Header("Layers")]
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private LayerMask _floorMask;

    [Header("References")]
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private ZoneManager _zoneManager;

    private Node[,] _grid;
    private readonly List<Node> _allNodes = new();
    private readonly Dictionary<Node, List<Node>> _neighbors = new();
    private readonly Dictionary<NodeZone, List<Node>> _nodesByZone = new();
    private static readonly List<Node> EmptyNeighbors = new();

    public static Graph Current { get; private set; }

    public IReadOnlyList<Node> AllNodes => _allNodes;
    public float NodeOffset => _nodeOffset;
    public LayerMask WallMask => _wallMask;

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
    }

    private void Start()
    {
        GenerateGrid();
    }

    public void Configure(int width, int height, float nodeOffset, Vector3 origin,
                          LayerMask wallMask, LayerMask floorMask)
    {
        _gridWidth = Mathf.Max(1, width);
        _gridHeight = Mathf.Max(1, height);
        _nodeOffset = Mathf.Max(0.1f, nodeOffset);
        _gridOrigin = origin;
        _wallMask = wallMask;
        _floorMask = floorMask;
    }

    public void SetZoneManager(ZoneManager zoneManager)
    {
        _zoneManager = zoneManager;
    }

    public void GenerateGrid()
    {
        ClearGraph();

        _grid = new Node[_gridWidth, _gridHeight];

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                Vector3 cellCenter = GetCellCenter(x, y);
                if (!IsFloor(cellCenter)) continue;
                if (IsWall(cellCenter)) continue;

                Node node = _nodePrefab != null
                    ? Instantiate(_nodePrefab, transform)
                    : new GameObject($"Nodo {x},{y}").AddComponent<Node>();

                node.transform.SetParent(transform);
                node.transform.position = cellCenter + Vector3.up * 0.1f;
                node.name = $"Nodo {x},{y}";
                node.Setup(new Coordinates(x, y));
                node.Zone = GetZone(node.transform.position);

                _grid[x, y] = node;
                _allNodes.Add(node);

                if (!_nodesByZone.TryGetValue(node.Zone, out List<Node> zoneNodes))
                {
                    zoneNodes = new List<Node>();
                    _nodesByZone[node.Zone] = zoneNodes;
                }

                zoneNodes.Add(node);
            }
        }

        BuildConnections();
    }

    private void ClearGraph()
    {
        foreach (Node node in _allNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }

        _allNodes.Clear();
        _neighbors.Clear();
        _nodesByZone.Clear();
        _grid = null;
    }

    private void BuildConnections()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                Node node = _grid[x, y];
                if (node == null || node.Blocked) continue;

                if (x + 1 < _gridWidth) TryConnect(node, _grid[x + 1, y]);
                if (y + 1 < _gridHeight) TryConnect(node, _grid[x, y + 1]);
            }
        }
    }

    private void TryConnect(Node a, Node b)
    {
        if (b == null || b.Blocked) return;

        Vector3 aPosition = a.transform.position + Vector3.up * 0.5f;
        Vector3 bPosition = b.transform.position + Vector3.up * 0.5f;
        if (Physics.Linecast(aPosition, bPosition, _wallMask, QueryTriggerInteraction.Ignore)) return;

        if (!_neighbors.TryGetValue(a, out List<Node> aNeighbors))
        {
            aNeighbors = new List<Node>();
            _neighbors[a] = aNeighbors;
        }

        if (!_neighbors.TryGetValue(b, out List<Node> bNeighbors))
        {
            bNeighbors = new List<Node>();
            _neighbors[b] = bNeighbors;
        }

        aNeighbors.Add(b);
        bNeighbors.Add(a);
    }

    public List<Node> GetNeighbors(Node current)
    {
        return _neighbors.TryGetValue(current, out List<Node> neighbors)
            ? neighbors
            : EmptyNeighbors;
    }

    private static readonly System.Func<Node, bool> NotSafeNode = node => !node.IsSafeZone;

    public Node GetClosestNode(Vector3 position, bool avoidSafeZones = false)
    {
        return GetClosestNode(position, avoidSafeZones ? NotSafeNode : null);
    }

    public Node GetClosestNode(Vector3 position, System.Func<Node, bool> filter)
    {
        Node closest = null;
        float bestDistance = float.MaxValue;

        foreach (Node node in _allNodes)
        {
            if (node == null || node.Blocked) continue;
            if (filter != null && !filter(node)) continue;

            Vector3 difference = node.transform.position - position;
            difference.y = 0f;
            float distance = difference.sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = node;
        }

        return closest;
    }

    public Node GetRandomNode(NodeZone zone)
    {
        if (!_nodesByZone.TryGetValue(zone, out List<Node> nodes) || nodes.Count == 0) return null;
        return nodes[Random.Range(0, nodes.Count)];
    }

    private NodeZone GetZone(Vector3 position)
    {
        if (_zoneManager == null) return NodeZone.Corridor;

        return _zoneManager.GetZoneType(position) switch
        {
            ZoneType.HuntingGround => NodeZone.HuntingGround,
            ZoneType.Safe => NodeZone.SafeZone,
            ZoneType.Start => NodeZone.Start,
            ZoneType.Exit => NodeZone.Exit,
            _ => NodeZone.Corridor
        };
    }

    private Vector3 GetCellCenter(int x, int y)
    {
        return _gridOrigin + new Vector3((x + 0.5f) * _nodeOffset, 0f, (y + 0.5f) * _nodeOffset);
    }

    private bool IsFloor(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * _floorRayHeight;
        return Physics.Raycast(origin, Vector3.down, out _, _floorRayHeight + 1f, _floorMask,
                               QueryTriggerInteraction.Ignore);
    }

    private bool IsWall(Vector3 position)
    {
        Vector3 center = position + Vector3.up * 0.5f;
        return Physics.CheckSphere(center, _wallCheckRadius, _wallMask, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmos()
    {
        if (_allNodes.Count == 0) return;

        foreach (Node node in _allNodes)
        {
            if (node == null) continue;

            if (_neighbors.TryGetValue(node, out List<Node> neighbors))
            {
                foreach (Node neighbor in neighbors)
                {
                    if (neighbor == null) continue;
                    Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.35f);
                    Gizmos.DrawLine(node.transform.position, neighbor.transform.position);
                }
            }

            Gizmos.color = node.Zone switch
            {
                NodeZone.HuntingGround => new Color(1f, 0.7f, 0.2f),
                NodeZone.SafeZone => new Color(0.7f, 0.4f, 1f),
                NodeZone.Start => new Color(0.3f, 0.9f, 0.5f),
                NodeZone.Exit => new Color(0.3f, 0.6f, 1f),
                _ => Color.white
            };

            Gizmos.DrawSphere(node.transform.position, 0.12f);
        }
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }
}
