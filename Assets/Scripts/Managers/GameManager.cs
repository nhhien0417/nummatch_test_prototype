using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{
    private int _currentStage, _addCount;
    public int CurrentStage => _currentStage;
    public int AddCount => _addCount;

    public void NewGame()
    {
        GemManager.Instance.GenerateGemProgresses();

        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            UpdateNewStage(1);
            GameplayUI.Instance.SetupGems();
        };
    }

    public void UpdateNewStage(int stage)
    {
        _currentStage = stage;
        _addCount = 6;

        CellGenerator.Instance.GenerateBoard();
        GameplayUI.Instance.UpdateStageText();
    }

    public void UpdateAddCount()
    {
        _addCount--;
        GameplayUI.Instance.UpdateAddText();
    }
}