using System.Linq;

public class GameManager : SingletonPersistent<GameManager>
{
    private int _currentStage, _addCount;
    public int CurrentStage => _currentStage;
    public int AddCount => _addCount;

    public void NewGame()
    {
        GemManager.Instance.GenerateGemProgresses();
        GameplayUI.Instance.SetupGems();
        UpdateNewStage(1);
    }

    #region Update State
    public void UpdateNewStage(int stage)
    {
        _currentStage = stage;
        _addCount = 6;

        CellGenerator.Instance.GenerateBoard();
        GameplayUI.Instance.UpdateStageText();
        GameplayUI.Instance.UpdateAddText();
    }

    public void UpdateAddCount()
    {
        _addCount--;
        GameplayUI.Instance.UpdateAddText();
    }
    #endregion

    #region Handle Game Over
    public void CheckWinGame()
    {
        var isWin = GemManager.Instance.GemProgresses.All(g => g.Collected == g.RequiredAmount);
        if (!isWin) return;

        PopupController.Instance.ShowPopup(PopupController.Popup.WinPopup);
    }

    public void CheckLoseGame()
    {
        var isLose = !CellGenerator.Instance.AnyPair() && _addCount <= 0;
        if (!isLose) return;

        PopupController.Instance.ShowPopup(PopupController.Popup.LosePopup);
    }
    #endregion
}