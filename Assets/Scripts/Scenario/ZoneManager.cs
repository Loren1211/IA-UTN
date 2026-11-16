using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class ZoneManager : MonoBehaviour
{
    [SerializeField] private ZoneMarker[] _zones;

    private readonly List<SafeZone> _safeZones = new();

    public IReadOnlyList<SafeZone> SafeZones => _safeZones;
    public IReadOnlyList<ZoneMarker> Zones => _zones;

    private void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        _safeZones.Clear();
        if (_zones == null) return;

        foreach (ZoneMarker zone in _zones)
        {
            if (zone is SafeZone safeZone) _safeZones.Add(safeZone);
        }
    }

    public void SetZones(ZoneMarker[] zones)
    {
        _zones = zones;
        Refresh();
    }

    public ZoneType GetZoneType(Vector3 worldPosition)
    {
        ZoneMarker zone = GetZoneAt(worldPosition);
        return zone != null ? zone.Type : ZoneType.None;
    }

    public ZoneMarker GetZoneAt(Vector3 worldPosition)
    {
        if (_zones == null) return null;

        foreach (ZoneMarker zone in _zones)
        {
            if (zone != null && zone.Contains(worldPosition)) return zone;
        }

        return null;
    }

    public bool IsInsideZone(Vector3 worldPosition, ZoneType type)
    {
        return GetZoneType(worldPosition) == type;
    }

    public bool IsInsideSafeZone(Vector3 worldPosition)
    {
        return IsInsideZone(worldPosition, ZoneType.Safe);
    }
}
