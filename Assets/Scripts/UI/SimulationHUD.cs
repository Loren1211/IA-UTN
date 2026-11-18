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
    private readonly List<HudEntity> _entities = new();
    private readonly Dictionary<EntityStatus, HudEntity> _entitiesByStatus = new();
    private readonly List<string> _events = new();

    private GUIStyle _entityStyle;
    private GUIStyle _panelTextStyle;
    private GUIStyle _zoneStyle;

    private const float AlertDuration = 1.2f;
    private const float AlertStartRadius = 0.4f;
    private const float AlertEndRadius = 4.5f;
    private const int AlertSegments = 32;
    private const float MinLineLength = 1f;
    private const float LastKnownMarkerRadius = 8f;

    private Vector3 _alertHowler;
    private float _alertUntil;
    private float _nextHowlLog;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void Start()
    {
        CollectStatuses();
    }

    private void OnEnable()
    {
        if (_wolfPack != null) _wolfPack.AlertRaised += OnAlertRaised;
    }

    private void OnDisable()
    {
        if (_wolfPack != null) _wolfPack.AlertRaised -= OnAlertRaised;
    }

    private void OnDestroy()
    {
        foreach (EntityStatus status in _statuses)
        {
            if (status != null) status.StateChanged -= OnStateChanged;
        }

        _statuses.Clear();
        _entities.Clear();
        _entitiesByStatus.Clear();
    }

    private void CollectStatuses()
    {
        _entities.Clear();
        _entitiesByStatus.Clear();

        if (_hunter != null)
            AddEntity(_hunter.GetComponent<EntityStatus>(), true, "Hunter");

        if (_wolfPack != null)
        {
            foreach (WolfAgent wolf in _wolfPack.Agents)
            {
                if (wolf != null)
                    AddEntity(wolf.GetComponent<EntityStatus>(), false, GetWolfLabel(wolf));
            }
        }
    }

    private void AddEntity(EntityStatus status, bool isHunter, string label)
    {
        if (status == null) return;

        HudEntity entity = new()
        {
            Status = status,
            IsHunter = isHunter,
            Transform = status.transform,
            Label = label
        };

        _entities.Add(entity);
        _entitiesByStatus[status] = entity;
        AddStatus(status);
    }

    private void AddStatus(EntityStatus status)
    {
        if (status == null || _statuses.Contains(status)) return;

        _statuses.Add(status);
        status.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityStatus status)
    {
        if (!_entitiesByStatus.TryGetValue(status, out HudEntity entity)) return;

        AddEvent($"{entity.Label} -> {status.State}");
    }

    private void OnAlertRaised(Vector3 hunterPosition, Vector3 howlerPosition)
    {
        _alertHowler = howlerPosition;
        _alertUntil = Time.time + AlertDuration;

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
        DrawWolfThreat();
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
        if (length < MinLineLength) return;

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
                GetStatus(_hunter.transform));
        }

        if (_wolfPack == null) return;

        foreach (WolfAgent wolf in _wolfPack.Agents)
        {
            if (wolf == null) continue;

            DrawAgentPath(wolf.transform.position, wolf.NavAgent,
                GetStatus(wolf.transform));
        }
    }

    private EntityStatus GetStatus(Transform target)
    {
        foreach (HudEntity entity in _entities)
        {
            if (entity.Transform == target) return entity.Status;
        }

        return null;
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
        foreach (HudEntity entity in _entities)
        {
            EntityStatus status = entity.Status;
            if (status == null || !status.gameObject.activeInHierarchy) continue;

            bool showBar = entity.IsHunter && status.Progress > 0.001f;
            float barValue = showBar ? status.Progress : 0f;
            float offset = entity.IsHunter ? _hunterLabelHeight : _wolfLabelHeight;

            DrawEntityLabel(status, entity.Label, entity.Transform.position,
                offset, showBar, barValue);
        }
    }

    private string GetWolfLabel(WolfAgent wolf)
    {
        if (_wolfPack != null)
        {
            for (int i = 0; i < _wolfPack.Agents.Count; i++)
            {
                if (_wolfPack.Agents[i] == wolf) return $"Wolf {i + 1}";
            }
        }

        return "Wolf";
    }

    private void DrawEntityLabel(EntityStatus status, string displayName, Vector3 worldPosition,
                                 float offset, bool showBar, float barValue)
    {
        if (!Project(worldPosition + Vector3.up * offset, out Vector2 screen)) return;

        string text = string.IsNullOrEmpty(status.Action)
            ? $"{displayName} [{status.State}]"
            : $"{displayName} [{status.State}]\n{status.Action}";

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

    private void DrawWolfThreat()
    {
        if (_hunter == null) return;

        WolfAgent wolf = _hunter.PerceivedWolf;
        if (wolf == null) return;

        Color color = wolf.IsSeeingTarget
            ? new Color(1f, 0.25f, 0.2f, 0.9f)
            : new Color(1f, 0.75f, 0.1f, 0.9f);

        if (Project(_hunter.transform.position, out Vector2 from) &&
            Project(wolf.transform.position, out Vector2 to))
        {
            DrawLine(from, to, color, 2f);
        }
    }

    private void DrawAlert()
    {
        if (Time.time >= _alertUntil) return;

        float progress = Mathf.Clamp01(1f - (_alertUntil - Time.time) / AlertDuration);
        float radius = Mathf.Lerp(AlertStartRadius, AlertEndRadius, progress);
        Color color = new(1f, 0.55f, 0.20f, Mathf.Lerp(0.90f, 0.08f, progress));

        Vector3 center = _alertHowler;
        center.y = 0.1f;

        Vector2 previous = default;
        bool hasPrevious = false;

        for (int i = 0; i <= AlertSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / AlertSegments;
            Vector3 worldPoint = center + new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            if (!Project(worldPoint, out Vector2 screen))
            {
                hasPrevious = false;
                continue;
            }

            if (hasPrevious) DrawLine(previous, screen, color, 2f);

            previous = screen;
            hasPrevious = true;
        }
    }

    private void DrawLastKnown()
    {
        if (_wolfPack == null || !_wolfPack.LastKnownHunterPosition.HasValue) return;

        Vector3 position = _wolfPack.LastKnownHunterPosition.Value;
        if (!Project(position, out Vector2 screen)) return;

        Color savedColor = GUI.color;
        GUI.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(screen.x - LastKnownMarkerRadius, screen.y - 1f,
            LastKnownMarkerRadius * 2f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screen.x - 1f, screen.y - LastKnownMarkerRadius,
            2f, LastKnownMarkerRadius * 2f), Texture2D.whiteTexture);
        GUI.color = savedColor;
    }

    private void DrawPanel()
    {
        StringBuilder builder = new();
        builder.AppendLine("HUNTER");

        if (_hunter != null)
        {
            builder.AppendLine($"State: {_hunter.CurrentStateName}");
            builder.AppendLine($"Gathers: {_hunter.Gathers}/{_hunter.RequiredGathers}");
            builder.AppendLine($"Escapes: {_hunter.Escapes}   Deaths: {_hunter.Deaths}");
            builder.AppendLine($"TBA melee: {FormatCooldown(_hunter.MeleeReady, _hunter.TimeUntilMelee)}   " +
                               $"TBA shoot: {FormatCooldown(_hunter.RangedReady, _hunter.TimeUntilRanged)}");
            builder.AppendLine($"Objective: {GetHunterObjective()}");
            builder.AppendLine($"Wolf threat: {(_hunter.PerceivedWolf != null ? GetWolfLabel(_hunter.PerceivedWolf) : "-")}");
            builder.AppendLine($"Selected safe zone: {(_hunter.SelectedSafeZone != null ? _hunter.SelectedSafeZone.DisplayName : "-")}");
            builder.AppendLine($"Action: {GetHunterAction()}");
        }

        builder.AppendLine();
        builder.AppendLine("WOLVES");

        if (_wolfPack != null)
        {
            foreach (WolfAgent wolf in _wolfPack.Agents)
            {
                if (wolf == null) continue;

                string lastPosition = wolf.HasLastKnown ? FormatPosition(wolf.LastKnownPosition) : "-";
                builder.AppendLine($"{GetWolfLabel(wolf)}: {wolf.CurrentStateName}");
                builder.AppendLine($"   last pos: {lastPosition}   A*: {FormatPathCount(wolf.NavAgent)}");
            }
        }

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

    private string GetHunterObjective()
    {
        if (_hunter == null) return "-";

        switch (_hunter.CurrentState)
        {
            case HunterStateId.Search:
                return "Hunting Ground";
            case HunterStateId.Attack:
                return _hunter.AttackTarget != null ? _hunter.AttackTarget.name : "-";
            case HunterStateId.Gather:
                return _hunter.GatherTarget != null ? _hunter.GatherTarget.name : "-";
            case HunterStateId.Flee:
                return _hunter.SelectedSafeZone != null ? _hunter.SelectedSafeZone.DisplayName : "-";
            case HunterStateId.Exit:
                return "Exit";
            default:
                return "-";
        }
    }

    private string GetHunterAction()
    {
        return _hunter != null && !string.IsNullOrEmpty(_hunter.CurrentActionLabel)
            ? _hunter.CurrentActionLabel
            : "-";
    }

    private static string FormatPathCount(NavAgent agent)
    {
        return agent != null && agent.IsFollowingPath ? agent.CurrentPath.Count.ToString() : "-";
    }

    private static string FormatPosition(Vector3 position)
    {
        return $"({position.x:0.0}, {position.z:0.0})";
    }

    private static string FormatCooldown(bool ready, float remaining)
    {
        return ready ? "ready" : $"{remaining:0.0}s";
    }

    private sealed class HudEntity
    {
        public EntityStatus Status;
        public bool IsHunter;
        public Transform Transform;
        public string Label;
    }
}
