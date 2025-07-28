using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI _stageText;

    public void BackToHome()
    {
        SceneManager.LoadScene("Home");
    }

    public void UpdateStageText()
    {
        _stageText.text = $"Stage: {StageManager.Instance.CurrentStage}";
    }
}
