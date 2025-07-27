using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        StageManager.Instance.GenerateBoard();
    }
}
