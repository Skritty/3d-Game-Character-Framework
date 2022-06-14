using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KinematicCharacterController;

[CreateAssetMenu(menuName = "State/Locomotion/IdleState")]
public class IdleState : LocomotionState
{
	protected override void Inturruptions(ControlledObject controlledObject)
    {
        if (!controlledObject.Motor.GroundingStatus.IsStableOnGround)
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Fall);
        }

        if (looping && controlledObject.controller.GetInput<Vector2>(InputActions.Move) != Vector2.zero)
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Move);
        }
    }
}
