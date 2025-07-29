using System.Collections.Generic;
using UnityEngine;

public class GemManager : SingletonPersistent<GemManager>
{
    [SerializeField] private List<GemEntry> _gemEntries;

    private Dictionary<GemType, Sprite> _gemDict;

    public Dictionary<GemType, Sprite> GetGemEntries()
    {
        if (_gemDict != null) return _gemDict;
        _gemDict = new();

        foreach (var entry in _gemEntries)
        {
            if (!_gemDict.ContainsKey(entry.Type))
                _gemDict.Add(entry.Type, entry.Sprite);
        }

        return _gemDict;
    }
}

[System.Serializable]
public class GemEntry
{
    public GemType Type;
    public Sprite Sprite;
}

public enum GemType
{
    None,
    Orange,
    Pink,
    Purple
}

public class GemProgress
{
    public GemType Type;
    public int RequiredAmount;
    public int Collected;
}
