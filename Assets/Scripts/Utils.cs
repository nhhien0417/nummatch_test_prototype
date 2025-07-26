using UnityEngine;

public static class Utils
{
    public static Color GetHexColor(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
}
