using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : SingletonPersistent<GameManager>
{
    public void NewGame()
    {
        GemManager.Instance.GenerateGemProgress();

        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            StageManager.Instance.UpdateNewStage(1);
            GameplayUI.Instance.SetupGems();
        };
    }
}