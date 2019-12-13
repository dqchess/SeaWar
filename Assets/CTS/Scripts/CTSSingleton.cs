using UnityEngine;

namespace CTS
{
    /// <summary>
    /// Be aware this will not prevent a non singleton constructor such as `T myT = new T();`
    /// To prevent that, add `protected T () {}` to your singleton class.
    /// 
    /// As a note, this is made as MonoBehaviour because we need Coroutines.
    /// Adapted from http://wiki.unity3d.com/index.php/Singleton
    /// 
    /// public class Manager : Singleton<Manager>
    /// {
    ///     protected Manager() { } // guarantee this will be always a singleton only - can't use the constructor!
    ///     public string myGlobalVar = "whatever";
    /// }
    /// public class MyClass : MonoBehaviour {
    ///	    void Awake () {
    ///		    Debug.Log(Manager.Instance.myGlobalVar);
    ///	    }
    /// }
    /// </summary>
    public class CTSSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("Something went really wrong " +
                                           " - there should never be more than 1 singleton!" +
                                           " Reopenning the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            if(_applicationIsQuitting == false)
                            {
                                GameObject singleton = new GameObject();
                                _instance = singleton.AddComponent<T>();
                                singleton.name = typeof(T).ToString();

                                if (Application.isPlaying)
                                {
                                    DontDestroyOnLoad(singleton);
                                }
                            }
                        }
                    }

                    return _instance;
                }
            }
        }

        void Awake()
        {
            _applicationIsQuitting = false;
        }

        void Start()
        {
            _applicationIsQuitting = false;
        }

        void OnEnable()
        {
            _applicationIsQuitting = false;
        }

        void OnDestroy()
        {
            _applicationIsQuitting = true;
        }

        void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}