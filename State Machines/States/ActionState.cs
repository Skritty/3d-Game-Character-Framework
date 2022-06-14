using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Actions/Generic Action")]
public class ActionState : State
{
	[SerializeField]
	protected ActionState exitState;
	[Tooltip("Leave the state null to use the controlled object's default locomotion states. Empty will force Idle.")]
	public SerializedDictionary<Locomotion, LocomotionState> allowedLocomotion = new SerializedDictionary<Locomotion, LocomotionState>();

	public override void OnEnter(ControlledObject controlledObject)
	{
		if (allowedLocomotion.Count == 0)
		{
			controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
		}
		else
		{
            if (allowedLocomotion.ContainsKey(controlledObject.stateMachine.CurrentLocomotion))
            {
				controlledObject.stateMachine.SetLocomotionState(controlledObject.stateMachine.CurrentLocomotion);
			}
            else
            {
				foreach (KeyValuePair<Locomotion, LocomotionState> keyValue in allowedLocomotion)
				{
					controlledObject.stateMachine.SetLocomotionState(keyValue.Key);
					break;
				}
			}
        }

		if (!skipAnim)
        {
			controlledObject.CrossFadeInFixedTime(animationStateName, transitionTime, 0, transitionTimeOffset, transitionTimeNormalized);
			controlledObject.MeshRoot.localPosition = controlledObject.MeshRootOffset;
		}
	}

	public sealed override void HandleState(ControlledObject controlledObject)
	{
		Inturruptions(controlledObject);
		if (controlledObject.stateMachine.actionFrame >= maxFrame)
		{
			if (looping)
			{
				controlledObject.stateMachine.actionFrame = 0;
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
			if (controlledObject.stateMachine.actionFrame >= interrupt.frames.x
				&& controlledObject.stateMachine.actionFrame <= interrupt.frames.y
				&& controlledObject.controller.GetBuffer(interrupt.action) > 0)
			{
				valid.Add(interrupt.action);
			}
		}
		return valid;
	}

	protected override void Exit(ControlledObject controlledObject)
	{
		controlledObject.stateMachine.SetActionState(exitState);
	}
}
