using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Locomotion/RotateState")]
public class RotateState : LocomotionState
{
    [SerializeField]
    private float rotationSmoothness;

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);
        controlledObject.preferredRotation = Vector3.Lerp(controlledObject.preferredRotation, GetInputVector(controlledObject), rotationSmoothness).normalized;
    }
}