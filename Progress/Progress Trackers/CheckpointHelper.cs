using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CheckpointHelper : MonoBehaviour
{
    public System.Action OnTransformChanged;

    private void Update()
    {
        if (transform.hasChanged)
            OnTransformChanged?.Invoke();
    }
}
