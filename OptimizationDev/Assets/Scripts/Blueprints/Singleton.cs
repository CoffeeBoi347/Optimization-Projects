using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T _instance;

    public static T Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if( _instance != null)
                {
                    Debug.LogError($"{typeof(T).Name} not found! Please initialize it before catching reference.");
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if( _instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(this.gameObject);
    }
}