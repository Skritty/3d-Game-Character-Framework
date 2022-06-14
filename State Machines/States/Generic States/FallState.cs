using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Locomotion/FallState")]
public class FallState : LocomotionState
{
    protected override void Inturruptions(ControlledObject controlledObject)
    {
        if (controlledObject.Motor.GroundingStatus.IsStableOnGround)
        {
            controlledObject.velocity = Vector3.zero;
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
        }
    }
}