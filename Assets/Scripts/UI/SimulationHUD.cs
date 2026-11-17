using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SimulationHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private HunterAgent _hunter;
    [SerializeField] private WolfPack _wolfPack;
    [SerializeField] private ZoneManager _zoneManager;

    [Header("Layout")]
    [SerializeField] private int _fontSize = 12;
    [SerializeField] private int _panelWidth = 280;
    [SerializeField] private float _hunterLabelHeight = 2.2f;
    [SerializeField] private float _wolfLabelHeight = 1.8f;
    [SerializeField] private float _pathWidth = 2.5f;

    [Header("Toggles")]
    [SerializeField] private bool _drawPaths = true;
    [SerializeField] private bool _drawZoneLabels = true;
    [SerializeField] private bool _drawPanel = true;

    private readonly List<EntityStatus> _statuses = new();
    private readonly List<string> _events = new();

    private GUIStyle _entityStyle;
    private GUIStyle _panelTextStyle;
    private GUIStyle _zoneStyle;
    private GUIStyle _alertStyle;

    private Vector3? _alertPosition;
    private float _alertUntil;
    private float _nextHowlLog;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void Start()
    {
        CollectStatuses();

        if (_wolfPack != null) _wolfPack.AlertRaised += OnAlertRaised;
    }

    private void OnDestroy()
    {
        if (_wolfPack != null) _wolfPack.AlertRaised -= OnAlertRaised;

        foreach (EntityStatus status in _statuses)
        {
            if (status != null) status.StateChanged -= OnStateChanged;
        }

        _statuses.Clear();
    }

    private void CollectStatuses()
    {
        if (_hunter != null) AddStatus(_hunter.GetComponent<EntityStatus>());

        if (_wolfPack != null)
        {
            foreach (WolfAgent wolf in _wolfPack.Agents)
            {
                if (wolf != null) AddStatus(wolf.GetComponent<EntityStatus>());
            }
        }
    }

    private void AddStatus(EntityStatus status)
    {
        if (status == null || _statuses.Contains(status)) return;

        _statuses.Add(status);
        status.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityStatus status)
    {
        if (status.GetComponent<Boid>() != null) return;
        AddEvent($"{status.DisplayName} -> {status.State}");
    }

    private void OnAlertRaised(Vector3 position)
    {
        _alertPosition = position;
        _alertUntil = Time.time + 1.2f;

        if (Time.time < _nextHowlLog) return;

        _nextHowlLog = Time.time + 2f;
        AddEvent("Howl: pack alerted");
    }

    private void AddEvent(string text)
    {
        _events.Add(text);
        if (_events.Count > 6) _events.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (_camera == null) return;

        EnsureStyles();

        if (_drawPaths) DrawPaths();
        if (_drawZoneLabels) DrawZones();

        DrawEntities();
        DrawAlert();
        DrawLastKnown();

        if (_drawPanel) DrawPanel();
    }

    private void EnsureStyles()
    {
        if (_entityStyle != null) return;

        _entityStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 3, 3)
        };
        _entityStyle.normal.textColor = Color.white;

        _panelTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            alignment = TextAnchor.UpperLeft,
            wordWrap = false
        };
        _panelTextStyle.normal.textColor = Color.white;

        _zoneStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(9, _fontSize - 2),
            alignment = TextAnchor.MiddleCenter
        };
        _zoneStyle.normal.textColor = Color.white;

        _alertStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = _fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _alertStyle.normal.textColor = new Color(1f, 0.55f, 0.2f);
    }

    private bool Project(Vector3 worldPosition, out Vector2 screen)
    {
        Vector3 point = _camera.WorldToScreenPoint(worldPosition);

        if (point.z <= 0f)
        {
            screen = default;
            return false;
        }

        screen = new Vector2(point.x, Screen.height - point.y);
        return true;
    }

    private void DrawLine(Vector2 from, Vector2 to, Color color, float width)
    {
        float length = Vector2.Distance(from, to);
        if (length < 1f) return;

        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;

        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.color = color;
        GUI.DrawTexture(new Rect(from.x, from.y, length, width), Texture2D.whiteTexture);

        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }

    private void DrawPaths()
    {
        if (_hunter != null)
        {
            DrawAgentPath(_hunter.transform.position, _hunter.NavAgent,
                _hunter.GetComponent<EntityStatus>());
        }

        if (_wolfPack == null) return;

        foreach (WolfAgent wolf in _wolfPack.Agents)
        {
            if (wolf == null) continue;

            DrawAgentPath(wolf.transform.position, wolf.NavAgent,
                wolf.GetComponent<EntityStatus>());
        }
    }

    private void DrawAgentPath(Vector3 origin, NavAgent agent, EntityStatus status)
    {
        if (agent == null) return;

        Color color = status != null ? status.Color : Color.white;
        color.a = 0.85f;

        if (!Project(origin, out Vector2 previous)) return;
        bool hasPrevious = true;

        foreach (Node node in agent.CurrentPath)
        {
            if (node == null) continue;

            if (!Project(node.transform.position, out Vector2 point))
            {
                hasPrevious = false;
                continue;
            }

            if (hasPrevious) DrawLine(previous, point, color, _pathWidth);

            previous = point;
            hasPrevious = true;
        }

        if (!agent.TryGetDestination(out Vector3 destination)) return;
        if (!Project(destination, out Vector2 destinationScreen)) return;

        if (!agent.IsFollowingPath && hasPrevious)
            DrawLine(previous, destinationScreen, color, _pathWidth);

        Color savedColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(destinationScreen.x - 3f, destinationScreen.y - 3f, 6f, 6f),
            Texture2D.whiteTexture);
        GUI.color = savedColor;
    }

    private void DrawZones()
    {
        if (_zoneManager == null) return;

        foreach (ZoneMarker zone in _zoneManager.Zones)
        {
            if (zone == null) continue;
            if (!Project(zone.transform.position, out Vector2 screen)) continue;

            string text = zone.DisplayName;
            Vector2 size = _zoneStyle.CalcSize(new GUIContent(text));
            Rect rect = new(screen.x - size.x * 0.5f - 4f, screen.y - size.y * 0.5f - 4f,
                size.x + 8f, size.y + 8f);

            Color savedColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = zone.ZoneColor;
            GUI.Label(rect, text, _zoneStyle);
            GUI.color = savedColor;
        }
    }

    private void DrawEntities()
    {
        foreach (EntityStatus status in _statuses)
        {
            if (status == null || !status.gameObject.activeInHierarchy) continue;

            HunterAgent hunter = status.GetComponent<HunterAgent>();
            if (hunter != null)
            {
                bool showBar = status.Progress > 0.001f;
                DrawEntityLabel(status, hunter.transform.position, _hunterLabelHeight,
                    showBar, status.Progress);
                continue;
            }

            WolfAgent wolf = status.GetComponent<WolfAgent>();
            if (wolf != null)
            {
                DrawEntityLabel(status, wolf.transform.position, _wolfLabelHeight, false, 0f);
            }
        }
    }

    private void DrawEntityLabel(EntityStatus status, Vector3 worldPosition, float offset,
                                 bool showBar, float barValue)
    {
        if (!Project(worldPosition + Vector3.up * offset, out Vector2 screen)) return;

        string text = string.IsNullOrEmpty(status.Action)
            ? $"{status.DisplayName} [{status.State}]"
            : $"{status.DisplayName} [{status.State}]\n{status.Action}";

        Vector2 size = _entityStyle.CalcSize(new GUIContent(text));
        Rect rect = new(screen.x - size.x * 0.5f, screen.y - size.y, size.x, size.y);

        Color savedColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = status.Color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
        GUI.color = savedColor;

        GUI.Label(rect, text, _entityStyle);

        if (!showBar) return;

        Rect barRect = new(rect.x, rect.yMax + 2f, rect.width, 4f);
        DrawBar(barRect, barValue, status.Color);
    }

    private void DrawBar(Rect rect, float value, Color color)
    {
        if (rect.width <= 1f) return;

        Color savedColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        Rect fill = rect;
        fill.width = rect.width * Mathf.Clamp01(value);
        GUI.color = color;
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = savedColor;
    }

    private void DrawAlert()
    {
        if (_alertPosition == null || Time.time >= _alertUntil) return;
        if (!Project(_alertPosition.Value + Vector3.up * 2f, out Vector2 screen)) return;

        GUI.Label(new Rect(screen.x - 60f, screen.y - 12f, 120f, 24f), "HOWL!", _alertStyle);
    }

    private void DrawLastKnown()
    {
        if (_wolfPack == null || !_wolfPack.LastKnownHunterPosition.HasValue) return;

        Vector3 position = _wolfPack.LastKnownHunterPosition.Value;
        if (!Project(position, out Vector2 screen)) return;

        Color savedColor = GUI.color;
        GUI.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(screen.x - 8f, screen.y - 1f, 16f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screen.x - 1f, screen.y - 8f, 2f, 16f), Texture2D.whiteTexture);
        GUI.color = savedColor;

        GUI.Label(new Rect(screen.x + 10f, screen.y - 10f, 110f, 20f), "Last known", _zoneStyle);
    }

    private void DrawPanel()
    {
        StringBuilder builder = new();
        builder.AppendLine("HUNTER");

        if (_hunter != null)
        {
            builder.AppendLine($"State: {_hunter.CurrentStateName}");

            if (!string.IsNullOrEmpty(_hunter.CurrentActionLabel))
                builder.AppendLine($"Action: {_hunter.CurrentActionLabel}");

            builder.AppendLine($"Prey: {_hunter.Gathers}/{_hunter.RequiredGathers}");
            builder.AppendLine($"Melee: {FormatCooldown(_hunter.MeleeReady, _hunter.TimeUntilMelee)}   " +
                               $"Arrow: {FormatCooldown(_hunter.RangedReady, _hunter.TimeUntilRanged)}");
            builder.AppendLine($"Safe zone: {(_hunter.SelectedSafeZone != null ? _hunter.SelectedSafeZone.DisplayName : "-")}");
        }

        builder.AppendLine();
        builder.AppendLine("WOLVES");

        if (_wolfPack != null)
        {
            foreach (WolfAgent wolf in _wolfPack.Agents)
            {
                if (wolf != null) builder.AppendLine($"{wolf.name}: {wolf.CurrentStateName}");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"Last known: {FormatLastKnown()}");

        if (_events.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("EVENTS");

            foreach (string entry in _events) builder.AppendLine(entry);
        }

        string text = builder.ToString();
        Vector2 size = _panelTextStyle.CalcSize(new GUIContent(text));
        Rect rect = new(10f, 10f, _panelWidth, size.y + 20f);

        Color savedColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = savedColor;

        GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f),
            text, _panelTextStyle);
    }

    private static string FormatCooldown(bool ready, float remaining)
    {
        return ready ? "ready" : $"{remaining:0.0}s";
    }

    private string FormatLastKnown()
    {
        if (_wolfPack == null || !_wolfPack.LastKnownHunterPosition.HasValue) return "-";

        Vector3 position = _wolfPack.LastKnownHunterPosition.Value;
        return $"({position.x:0.0}, {position.z:0.0})";
    }
}
