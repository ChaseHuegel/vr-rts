using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish
{

    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        //  Keep this object alive
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prefer existing scene instance
                    _instance = FindFirstObjectByType<T>();

                    //if (_instance == null)
                    //{
                    //    GameObject obj = new GameObject();
                    //    _instance = obj.AddComponent<T>();
                    //    obj.name = typeof(T).ToString();

                    //    DontDestroyOnLoad(obj);
                    //}
                }

                return _instance;
            }
        }

        // Ensure duplicates created by scene setup are handled and instance is assigned early
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                // Prefer existing scene instance
                    _instance = FindFirstObjectByType<T>();

                //if (_instance == null)
                //{
                //    GameObject obj = new GameObject();
                //    _instance = obj.AddComponent<T>();
                //    obj.name = typeof(T).ToString();

                //    DontDestroyOnLoad(obj);
                //}
            }
            else if (_instance != this)
            {
                // Destroy duplicate
                //if (Application.isPlaying)
                //    Destroy(this.gameObject);
                //else
                //    DestroyImmediate(this.gameObject);
            }
        }

    }

}