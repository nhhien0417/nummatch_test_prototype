using UnityEngine.SceneManagement;

public class HomeUI : Singleton<HomeUI>
{
    public void NewGame()
    {
        AudioManager.Instance.PlaySFX("pop_button");
        SceneManager.LoadSceneAsync("Gameplay").completed += op =>
        {
            GameManager.Instance.NewGame();
        };
    }
}
