using UnityEngine;

// Singleton for components that exist only in the current scene
public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                var objs = FindObjectsByType<T>(FindObjectsSortMode.None);
                if (objs.Length > 0)
                    _instance = objs[0];

                if (objs.Length > 1)
                    Debug.LogError($"There is more than one {typeof(T).Name} in the scene.");
            }

            return _instance;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}

// Singleton for components that should persist across scenes
public class SingletonPersistent<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
