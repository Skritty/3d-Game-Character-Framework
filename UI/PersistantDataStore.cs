using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Persistant Data Store", fileName = "Data Store")]
public class PersistantDataStore : ScriptableObject
{
    #region Static
    private static SerializedDictionary<string, PersistantDataStore> stores = new SerializedDictionary<string, PersistantDataStore>();
    private static void Add(string name, PersistantDataStore value)
    {
        if (!stores.TryAdd(name, value))
            stores[name] = value;
        else
            Debug.Log($"Added {name} to data store catalogue");
    }

    public static PersistantDataStore GetDataStore(string name)
    {
        PersistantDataStore value;
        if (stores.TryGetValue(name, out value))
            return value;
        value = CreateInstance<PersistantDataStore>();
        value.name = name;
        Add(name, value);
        return value;
    }
    #endregion

    #region Scriptable Object
    [SerializeField]
    private SerializedDictionary<string, dynamic> data = new SerializedDictionary<string, dynamic>();

    public T Get<T>(string key)
    {
        dynamic value;
        if (data.TryGetValue(key, out value))
            return (T)value;
        return default(T);
    }

    public void Add(string key, dynamic value)
    {
        if (!data.TryAdd(key, value))
            data[key] = value;
    }
    #endregion

    private void OnValidate()
    {
        Add(name, this);
    }


}
