using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI _stageText;
    [SerializeField] private Transform _gemContainer;
    [SerializeField] private GameObject _gemPrefab;

    public void BackToHome()
    {
        SceneManager.LoadScene("Home");
    }

    public void UpdateStageText()
    {
        _stageText.text = $"Stage: {CellGenerator.Instance.CurrentStage}";
    }

    public void SetupGems()
    {
        foreach (var gemProgress in GemManager.Instance.GemProgresses)
        {
            var gemObject = Instantiate(_gemPrefab, _gemContainer).GetComponent<Gem>();
            gemObject.UpdateGemInfo(gemProgress.RequiredAmount, GemManager.Instance.GetGemEntries().TryGetValue(gemProgress.Type, out var sprite) ? sprite : null);
        }
    }
}
