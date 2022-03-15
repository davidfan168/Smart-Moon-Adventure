using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitySingleton<T> : MonoBehaviour
    where T : Component
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject();
                // obj.hideFlags = HideFlags.HideAndDontSave;
                instance = obj.AddComponent<T>();
            }
            return instance;
        }
    }
    
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}