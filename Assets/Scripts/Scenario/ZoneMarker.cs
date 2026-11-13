using UnityEngine;

public class ZoneMarker : MonoBehaviour
{
    [SerializeField] private string _displayName = "Zona";
    [SerializeField] private Color _zoneColor = Color.white;

    public string DisplayName => _displayName;
    public Color ZoneColor => _zoneColor;

    public void Configure(string displayName, Color color)
    {
        _displayName = displayName;
        _zoneColor = color;
    }
}
