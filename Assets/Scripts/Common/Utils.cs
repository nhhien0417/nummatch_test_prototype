using UnityEngine;

public static class Utils
{
    // Converts a hex color string to a Unity Color
    public static Color GetHexColor(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
}
