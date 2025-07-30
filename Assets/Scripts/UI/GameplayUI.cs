using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI _stageText;
    [SerializeField] private Transform _gemContainer;
    [SerializeField] private GameObject _gemPrefab;

    private List<Gem> _gems = new();

    public void BackToHome()
    {
        SceneManager.LoadScene("Home");
    }

    public void UpdateStageText()
    {
        _stageText.text = $"Stage: {GameManager.Instance.CurrentStage}";
    }

    public void SetupGems()
    {
        _gems.Clear();

        foreach (var gemProgress in GemManager.Instance.GemProgresses)
        {
            var gemObject = Instantiate(_gemPrefab, _gemContainer).GetComponent<Gem>();
            gemObject.UpdateGemInfo(gemProgress.RequiredAmount, gemProgress.Type, GemManager.Instance.GetGemEntries()[gemProgress.Type]);

            _gems.Add(gemObject);
        }
    }

    public void UpdateGemProgresses(GemProgress gemProgress)
    {
        var gem = _gems.FirstOrDefault(g => g.GemType == gemProgress.Type);
        gem.UpdateProgress(gemProgress.RequiredAmount - gemProgress.Collected);
    }
}
