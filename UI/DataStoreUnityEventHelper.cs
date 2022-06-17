using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStoreUnityEventHelper : MonoBehaviour
{
    [SerializeField]
    private string valueName;
    [SerializeField]
    private PersistantDataStore store;

    public void Add(Object value)
    {
        store.Add(valueName, value);
    }
}
