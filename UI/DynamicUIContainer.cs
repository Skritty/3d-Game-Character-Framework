using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DynamicUIContainer : UIBehaviour
{
    [SerializeField, Sirenix.OdinInspector.ShowInInspector]
    private SerializedDictionary<string, Object> references = new SerializedDictionary<string, Object>();
    public Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();

    public T Get<T>(string key) where T : Object
    {
        return (T)references[key];
    }

    public T GetData<T>(string key)
    {
        return (T)data[key];
    }
}
