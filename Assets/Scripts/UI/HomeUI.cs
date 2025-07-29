public class HomeUI : Singleton<HomeUI>
{
    public void NewGame()
    {
        GameManager.Instance.NewGame();
    }
}
