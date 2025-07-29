using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{

    private List<GemProgress> _gemProgress = new();
    public List<GemProgress> GemProgress => _gemProgress;

    public void GenerateGemProgress()
    {
        _gemProgress.Clear();

        foreach (var gem in GemManager.Instance.GetGemEntries())
        {
            if (gem.Key == GemType.None) continue;

            _gemProgress.Add(new GemProgress
            {
                Type = gem.Key,
                RequiredAmount = 10,
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