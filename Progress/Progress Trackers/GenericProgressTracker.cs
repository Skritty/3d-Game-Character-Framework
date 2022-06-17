using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[System.Serializable, CreateAssetMenu(menuName = "Progress/Progress Trackers/Generic Progress Tracker")]
public class GenericProgressTracker : ScriptableObject
{
    public static System.Action ProgressUpdated;
    public System.Action<GenericProgressTracker> TrackerUpdated;

    protected bool _isReached = false;
    [ShowInInspector, DisableInEditorMode]
    public virtual bool isReached
    {
        get => _isReached;
        set
        {
            _isReached = value;
            ProgressUpdated?.Invoke();
            TrackerUpdated?.Invoke(this);
        }
    }

    internal void SetReachedNoUpdates(bool isReached)
    {
        _isReached = isReached;
    }

    public GenericProgressTracker prerequisiteTracker;
    internal void ChainActivate()
    {
        prerequisiteTracker?.ChainActivate();
        isReached = true;
    }
}