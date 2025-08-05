using System.Linq;

public class GameManager : SingletonPersistent<GameManager>
{
    private int _currentStage, _addCount, _hintCount;

    private const int AddInit = 6;
    private const int HintInit = 6;

    public int CurrentStage => _currentStage;
    public int AddCount => _addCount;
    public int HintCount => _hintCount;

    public void NewGame()
    {
        GemManager.Instance.GenerateGemProgresses();
        GameplayUI.Instance.SetupGems();
        UpdateNewStage(1);
    }

    #region Update State
    // Starts a new stage and resets resources.
    public void UpdateNewStage(int stage)
    {
        _currentStage = stage;
        _addCount = AddInit;
        _hintCount = HintInit;

        BoardController.Instance.GenerateBoard();
        GameplayUI.Instance.UpdateStageText();
        GameplayUI.Instance.UpdateAddText();
        GameplayUI.Instance.UpdateHintText();
    }

    public void UpdateAddCount()
    {
        _addCount--;
        GameplayUI.Instance.UpdateAddText();
    }

    public void UpdateHintCount()
    {
        _hintCount--;
        GameplayUI.Instance.UpdateHintText();
    }
    #endregion

    #region Game Over
    // Checks if all required gems have been collected.
    public void CheckWinGame()
    {
        var isWin = GemManager.Instance.GemProgresses.All(g => g.Collected == g.RequiredAmount);
        if (!isWin) return;

        PopupController.Instance.ShowPopup(PopupController.Popup.WinPopup);
    }

    // Checks if the player has no more valid pairs and no adds left.
    public void CheckLoseGame()
    {
        var isLose = !BoardController.Instance.AnyPair() && _addCount <= 0;
        if (!isLose) return;

        PopupController.Instance.ShowPopup(PopupController.Popup.LosePopup);
    }
    #endregion
}