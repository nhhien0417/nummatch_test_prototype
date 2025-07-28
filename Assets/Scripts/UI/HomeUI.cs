using UnityEngine.SceneManagement;

public class HomeUI : Singleton<HomeUI>
{
    public void NewGame()
    {
        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            StageManager.Instance.UpdateNewStage(1);
        };
    }
}
