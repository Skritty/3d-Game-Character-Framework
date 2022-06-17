using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

[CreateAssetMenu(menuName = "State/Locomotion/JumpState")]
public class JumpState : LocomotionState
{
    [SerializeField]
    private float jumpSpeed;

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);
        controlledObject.velocity = Vector3.zero;
        controlledObject.Motor.ForceUnground(0.1f);
        controlledObject.Motor.BaseVelocity += (controlledObject.Motor.CharacterUp * jumpSpeed) - Vector3.Project(controlledObject.Motor.BaseVelocity, controlledObject.Motor.CharacterUp);
    }

    protected override void Inturruptions(ControlledObject controlledObject)
    {
        controlledObject.stateMachine.SetLocomotionState(Locomotion.Fall);
    }
}
