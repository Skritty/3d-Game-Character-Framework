using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Locomotion/MovementState2")]
public class MoveState2 : LocomotionState
{
    [SerializeField]
    protected AnimationCurve timeVelocityMultiplier = new AnimationCurve();
    [SerializeField]
    protected AnimationCurve distToTargetVelocityMultiplier = new AnimationCurve();
    [SerializeField]
    private SoundEffect footstepSFX;
    [SerializeField]
    private int footstepFrameInterval = 24;

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);
        controlledObject.velocity = GetInputVector(controlledObject) * controlledObject.MoveSpeed * timeVelocityMultiplier.Evaluate(controlledObject.stateMachine.locomotionFrame);

        if(controlledObject.target != null)
        {
            float dist = Vector3.Distance(controlledObject.transform.position, controlledObject.target.transform.position);
            controlledObject.velocity *= distToTargetVelocityMultiplier.Evaluate(dist);
        }

        DoFootsteps(controlledObject);
    }

    private void DoFootsteps(ControlledObject controlledObject)
    {
        if (footstepSFX != null && controlledObject.velocity.sqrMagnitude != 0 && controlledObject.stateMachine.locomotionFrame % (footstepFrameInterval) < 0.001f)
        {
            AudioManager.Instance?.PlaySFX(footstepSFX, controlledObject.transform.position);
        }
    }

    protected override void Inturruptions(ControlledObject controlledObject)
    {
        base.Inturruptions(controlledObject);

        if (!controlledObject.Motor.GroundingStatus.IsStableOnGround)
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Fall);
        }

        if (looping && controlledObject.stateMachine.CurrentLocomotion != Locomotion.Fall && controlledObject.controller.GetInput<Vector2>(InputActions.Move) == Vector2.zero)
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
        }
    }

    public override void OnExit(ControlledObject controlledObject)
    {
        base.OnExit(controlledObject);
        controlledObject.velocity = Vector3.zero;
    }
}
