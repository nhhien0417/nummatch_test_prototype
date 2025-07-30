using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GemManager : SingletonPersistent<GemManager>
{
    [SerializeField] private List<GemEntry> _gemEntries;

    private Dictionary<GemType, Sprite> _gemDict;
    private List<GemProgress> _gemProgresses = new();
    public List<GemProgress> GemProgresses => _gemProgresses;

    public Dictionary<GemType, Sprite> GetGemEntries()
    {
        if (_gemDict != null) return _gemDict;

        _gemDict = _gemEntries.Where(e => e.Type != GemType.None)
                              .GroupBy(e => e.Type)
                              .ToDictionary(g => g.Key, g => g.First().Sprite);

        return _gemDict;
    }

    public List<GemType> AvailableGemTypes => _gemProgresses.Where(g => g.Collected < g.RequiredAmount)
                                                            .Select(g => g.Type).ToList();

    public void GenerateGemProgresses()
    {
        _gemProgresses.Clear();

        var gemTypes = GetGemEntries().Keys.ToList();
        int countToPick = Random.Range(1, gemTypes.Count + 1);

        foreach (var type in gemTypes.OrderBy(_ => Random.value).Take(countToPick))
        {
            _gemProgresses.Add(new GemProgress(type, Random.Range(1, 4)));
        }
    }

    public void UpdateGemProgress(GemType gemType)
    {
        var gemProgress = _gemProgresses.FirstOrDefault(g => g.Type == gemType);
        gemProgress.CollectGem();
        
        GameplayUI.Instance.UpdateGemProgresses(gemProgress);
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
    public GemType Type { get; }
    public int RequiredAmount { get; }
    public int Collected { get; private set; }
    public bool IsCompleted => Collected >= RequiredAmount;

    public GemProgress(GemType type, int requiredAmount)
    {
        Type = type;
        RequiredAmount = requiredAmount;
        Collected = 0;
    }

    public void CollectGem()
    {
        if (Collected < RequiredAmount)
        {
            Collected++;
        }
    }
}
