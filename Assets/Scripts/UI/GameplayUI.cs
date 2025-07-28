using UnityEngine.SceneManagement;

public class GameplayUI : Singleton<GameplayUI>

{
    public void BackToHome()
    {
        SceneManager.LoadScene("Home");
    }
}
