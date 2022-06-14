using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LocomotionState : State
{
    public Locomotion type;
	[SerializeField]
	protected LocomotionState exitState;
	[SerializeField]
	protected List<LocomotionState> simultaniousLocomotion;

	public override void OnEnter(ControlledObject controlledObject)
	{
		foreach(LocomotionState state in simultaniousLocomotion)
        {
			state.OnEnter(controlledObject);
        }

		if (!skipAnim)
        {
			controlledObject.CrossFadeInFixedTime(animationStateName, transitionTime, 0, transitionTimeOffset, transitionTimeNormalized);
			controlledObject.MeshRoot.localPosition = controlledObject.MeshRootOffset;
		}
	}

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
		foreach (LocomotionState state in simultaniousLocomotion)
		{
			state.OnFixedUpdate(controlledObject);
		}
		base.OnFixedUpdate(controlledObject);
    }

    public override void OnUpdate(ControlledObject controlledObject)
    {
		foreach (LocomotionState state in simultaniousLocomotion)
		{
			state.OnUpdate(controlledObject);
		}
		base.OnUpdate(controlledObject);
    }

	public override void OnLateUpdate(ControlledObject controlledObject)
	{
		foreach (LocomotionState state in simultaniousLocomotion)
		{
			state.OnLateUpdate(controlledObject);
		}
		base.OnLateUpdate(controlledObject);
	}

	public override void OnExit(ControlledObject controlledObject)
    {
		foreach (LocomotionState state in simultaniousLocomotion)
		{
			state.OnExit(controlledObject);
		}
		base.OnExit(controlledObject);
    }

    public sealed override void HandleState(ControlledObject controlledObject)
	{
		Inturruptions(controlledObject);
		if (controlledObject.stateMachine.locomotionFrame >= maxFrame)
		{
			if (looping)
			{
				controlledObject.stateMachine.locomotionFrame = 0;
			}
			else
			{
				Exit(controlledObject);
			}
		}
	}

	public override List<InputActions> ValidInputs(ControlledObject controlledObject)
	{
		List<InputActions> valid = new List<InputActions>();
		foreach (InputInterruption interrupt in interruptableByInput)
		{
			if (controlledObject.stateMachine.locomotionFrame >= interrupt.frames.x
				&& controlledObject.stateMachine.locomotionFrame <= interrupt.frames.y
				&& controlledObject.controller.GetBuffer(interrupt.action) > 0)
			{
				valid.Add(interrupt.action);
			}
		}
		return valid;
	}

	protected override void Exit(ControlledObject controlledObject)
	{
		if (exitState)
		{
			controlledObject.stateMachine.SetLocomotionState(exitState);
		}
		else
		{
			controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
		}
	}
}
