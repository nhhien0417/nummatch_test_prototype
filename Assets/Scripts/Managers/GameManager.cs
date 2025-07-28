
public class GameManager : SingletonPersistent<GameManager>
{
    private int _currentStage;
    public int CurrentStage => _currentStage;

    public void UpdateNewStage(int stage)
    {
        _currentStage = stage;

        GameplayUI.Instance.UpdateStageText();
        StageManager.Instance.GenerateBoard();
    }
}
