using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class State : ScriptableObject
{
	[Header("Animation")]
	public bool skipAnim = false;
	public string animationStateName = "default";
	public float transitionTime = 0.1f;
	public float transitionTimeOffset;
	public float transitionTimeNormalized;

	[Header("State Data")]
	public int maxFrame;
	[SerializeField]
	protected bool looping;
	[SerializeField]
	protected List<InputInterruption> interruptableByInput;

	[System.Serializable]
	public class InputInterruption
    {
        [HideInInspector]
        public int dyanmicInt;
		[HorizontalGroup("V2I"), HideLabel]
		public InputActions action;
		[HorizontalGroup("V2I"), HideLabel, MinMaxSlider(0, "@dyanmicInt")]
		public Vector2Int frames;
	}

    private void OnValidate()
    {
		foreach (InputInterruption interrupt in interruptableByInput)
			interrupt.dyanmicInt = maxFrame;
    }

	public abstract void OnEnter(ControlledObject controlledObject);

	public virtual void OnLateUpdate(ControlledObject controlledObject) { }

	public virtual void OnUpdate(ControlledObject controlledObject) { }

	public virtual void OnFixedUpdate(ControlledObject controlledObject) { }

	public virtual void OnExit(ControlledObject controlledObject) { }

	public abstract void HandleState(ControlledObject controlledObject);

	protected virtual void Inturruptions(ControlledObject controlledObject) { }

	public abstract List<InputActions> ValidInputs(ControlledObject controlledObject);

	protected abstract void Exit(ControlledObject controlledObject);

	protected Vector3 GetInputVector(ControlledObject controlledObject)
	{
		Vector2 input = controlledObject.controller.GetInput<Vector2>(InputActions.Move);
		Vector3 moveInputVector = new Vector3(input.x, 0f, input.y).normalized;
		if (controlledObject.reorientToCamera)
		{
			moveInputVector = Reorient(Camera.main.transform.forward, moveInputVector);
		}
		return moveInputVector;
	}

	protected Vector3 Reorient(Vector3 newForward, Vector3 vector)
	{
		if (newForward == Vector3.zero) return vector;
		Vector3 planarDirection = Vector3.ProjectOnPlane(newForward, Vector3.up);
		return Quaternion.LookRotation(planarDirection, Vector3.up) * vector;
	}
}
