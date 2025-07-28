using UnityEngine;

public class GameManager : SingletonPersistent<GameManager>
{
    [SerializeField] private GemConfigs _gemConfigs;
}
