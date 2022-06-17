using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class ProgressDictionary : ScriptableObject
{
    [DisableInEditorMode]
    public GenericProgressTracker currentProgressTracker;

    public abstract void EnableAutoUpdate();
    public abstract void DisableAutoUpdate();
}
