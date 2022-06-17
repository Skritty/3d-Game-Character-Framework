using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class StateProgressDictionary<T> : ProgressDictionary
{
    [SerializeField]
    private T defaultValue;

    [ShowInInspector, SerializeField]
    private SerializedDictionary<GenericProgressTracker, T> _stateDictionary = new SerializedDictionary<GenericProgressTracker, T>();
    public T CurrentState
    {
        get
        {
            if (currentProgressTracker == null)
                return defaultValue;
            return _stateDictionary[currentProgressTracker];
        }
    }

    public override void EnableAutoUpdate()
    {
        foreach (KeyValuePair<GenericProgressTracker, T> pair in _stateDictionary)
            pair.Key.TrackerUpdated += UpdateCurrent;
    }

    public override void DisableAutoUpdate()
    {
        foreach (KeyValuePair<GenericProgressTracker, T> pair in _stateDictionary)
            pair.Key.TrackerUpdated -= UpdateCurrent;
    }

    private void UpdateCurrent(GenericProgressTracker tracker) => currentProgressTracker = tracker;
}
