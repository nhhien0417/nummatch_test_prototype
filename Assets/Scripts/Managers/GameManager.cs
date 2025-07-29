using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{

    private List<GemProgress> _gemProgress = new();
    public List<GemProgress> GemProgress => _gemProgress;

    public void GenerateGemProgress()
    {
        _gemProgress.Clear();

        var gemTypes = GemManager.Instance.GetGemEntries().Keys.Where(type => type != GemType.None).ToList();
        var countToPick = Random.Range(1, gemTypes.Count + 1);
        var selectedTypes = gemTypes.OrderBy(_ => Random.value).Take(countToPick);

        foreach (var type in selectedTypes)
        {
            _gemProgress.Add(new GemProgress
            {
                Type = type,
                RequiredAmount = Random.Range(1, 4),
                Collected = 0
            });
        }
    }

    public void NewGame()
    {
        GenerateGemProgress();

        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            StageManager.Instance.UpdateNewStage(1);
            GameplayUI.Instance.SetupGems();
        };
    }
}