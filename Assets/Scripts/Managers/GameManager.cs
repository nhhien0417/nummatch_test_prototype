using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{
    private int _currentStage;
    public int CurrentStage => _currentStage;

    public void NewGame()
    {
        GemManager.Instance.GenerateGemProgress();

        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            UpdateNewStage(1);
            GameplayUI.Instance.SetupGems();
        };
    }

    public void UpdateNewStage(int stage)
    {
        _currentStage = stage;

        CellGenerator.Instance.GenerateBoard();
        GameplayUI.Instance.UpdateStageText();
    }

}