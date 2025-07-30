using UnityEngine.SceneManagement;

public class HomeUI : Singleton<HomeUI>
{
    public void NewGame()
    {
        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            GameManager.Instance.NewGame();
        };
    }
}
