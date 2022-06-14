using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// An abstract controller for controlledObjects, it handles all input vectors and button buffers.
/// </summary>
[RequireComponent(typeof(ControlledObject))]
public abstract class BaseObjectController : MonoBehaviour
{
	//#if UNITY_EDITOR
	//	public ControlledObject controlledObject => GetComponent<ControlledObject>();
	//#else
	//	[NonSerialized]
	//	public ControlledObject controlledObject;

	//	private void Awake()
	//    {
	//		controlledObject = GetComponent<ControlledObject>();
	//	}
	//#endif
	public ControlledObject controlledObject;
	[SerializeField]
	private int bufferTime = 6;

	[Sirenix.OdinInspector.ShowInInspector, Sirenix.OdinInspector.ReadOnly]
	public Dictionary<MultiKey, object> _inputs = new Dictionary<MultiKey, object>();
	public void SetInput(InputActions name, object value)
    {
		Type type = value?.GetType();
		if (type == null) return;
		_inputs[new MultiKey(type, name)] = value;
	}
	public T GetInput<T>(InputActions name) where T : struct
    {
		if (!_inputs.ContainsKey(new MultiKey(typeof(T), name)))
			SetInput(name, Activator.CreateInstance<T>());

		return (T)Convert.ChangeType(_inputs[new MultiKey(typeof(T), name)], typeof(T));
    }
	public void BufferButton(InputActions name)
    {
		_inputs[new MultiKey(name, typeof(int))] = bufferTime;
	}
	public int GetBuffer(InputActions name)
	{
		if (!_inputs.ContainsKey(new MultiKey(typeof(int), name)))
			SetInput(name, 0);

		return GetInput<int>(name);
	}
	
	public void DecrementBuffer()
	{
		foreach(InputActions buffer in Enum.GetValues(typeof(InputActions)))
        {
			int current = GetInput<int>(buffer);
			if (current > 0)
				SetInput(buffer, --current);
		}
	}

	protected void FixedUpdate()
	{
		DecrementBuffer();
		CheckInputs();
	}

	protected void CheckInputs()
	{
		List<InputActions> valid1 = controlledObject.stateMachine.CurrentLocomotionState.ValidInputs(controlledObject);
		if(controlledObject.stateMachine.CurrentActionState != null)
        {
			List<InputActions> valid2 = controlledObject.stateMachine.CurrentActionState.ValidInputs(controlledObject);
			valid1 = valid1.Where(x => valid2.Contains(x)).ToList();
		}

		foreach (InputActions action in valid1)
		{
			controlledObject.UseEquipment(action);
			SetInput(action, 0);
		}
	}

	/// <summary>
	/// Contains logic on what to target
	/// </summary>
	/// <returns>The target</returns>
	public abstract TangibleObject ChooseTarget();
}
