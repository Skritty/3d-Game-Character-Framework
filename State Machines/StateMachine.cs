using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class StateMachine : MonoBehaviour
{
    private ControlledObject controlledObject;
    [ShowInInspector]
    public State CurrentActionState { get; private set; }
    [ShowInInspector]
    public LocomotionState CurrentLocomotionState { get; private set; }
    [ShowInInspector]
    public Locomotion CurrentLocomotion { get; private set; }
    public int actionFrame;
    public int locomotionFrame;

    public void Awake()
    {
        controlledObject = GetComponent<ControlledObject>();
        CurrentActionState = null;
        CurrentLocomotionState = controlledObject.locomotionStates[Locomotion.Idle];
    }

    private void FixedUpdate()
    {
        actionFrame++;
        locomotionFrame++;

        CurrentActionState?.OnFixedUpdate(controlledObject);
        CurrentActionState?.HandleState(controlledObject);

        CurrentLocomotionState?.OnFixedUpdate(controlledObject);
        CurrentLocomotionState?.HandleState(controlledObject);
    }

    private void Update()
    {
        CurrentActionState?.OnUpdate(controlledObject);

        CurrentLocomotionState?.OnUpdate(controlledObject);
    }

    private void LateUpdate()
    {
        CurrentActionState?.OnLateUpdate(controlledObject);

        CurrentLocomotionState?.OnLateUpdate(controlledObject);
    }

    public void SetLocomotionState(Locomotion newState)
    {
        if (!CheckLocomotionAllowed(newState)) return;
        CurrentLocomotionState?.OnExit(controlledObject);
        locomotionFrame = 0;
        CurrentLocomotionState = GetLocomotionState(newState);
        CurrentLocomotionState?.OnEnter(controlledObject);
        CurrentLocomotion = newState;
    }

    public void SetLocomotionState(LocomotionState newState)
    {
        if (!CheckLocomotionAllowed(newState.type)) return;
        CurrentLocomotionState?.OnExit(controlledObject);
        locomotionFrame = 0;
        CurrentLocomotionState = newState;
        CurrentLocomotionState?.OnEnter(controlledObject);
        CurrentLocomotion = newState.type;
    }

    public void SetActionState(State newState)
    {
        CurrentActionState?.OnExit(controlledObject);
        actionFrame = 0;
        CurrentActionState = newState;
        CurrentActionState?.OnEnter(controlledObject);
    }

    private bool CheckLocomotionAllowed(Locomotion type)
    {
        if (CurrentActionState == null)
        {
            return true;
        }
        else if (CurrentActionState is ActionState)
        {
            ActionState action = CurrentActionState as ActionState;
            if(action.allowedLocomotion.Count == 0 && type == Locomotion.Idle)
            {
                return true;
            }
            else if (action.allowedLocomotion.ContainsKey(type))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else return true;
    }

    private LocomotionState GetLocomotionState(Locomotion type)
    {
        ActionState action = CurrentActionState as ActionState;
        if (action != null && action.allowedLocomotion.Count > 0 && action.allowedLocomotion[type] != null)
        {
            return action.allowedLocomotion[type];
        }
        else
        {
            return controlledObject.locomotionStates[type];
        }
    }

    public void PlayStateAnim(ControlledObject controlledObject, Locomotion stateEnum)
    {
        State state = controlledObject.locomotionStates[stateEnum];
        controlledObject.CrossFadeInFixedTime(state.animationStateName, state.transitionTime, 0, state.transitionTime);
        controlledObject.MeshRoot.transform.GetChild(0).localPosition *= 0;
        controlledObject.MeshRoot.transform.GetChild(0).localEulerAngles *= 0;
    }

    public void PlayStateAnim(ControlledObject controlledObject, State state)
    {
        controlledObject.CrossFadeInFixedTime(state.animationStateName, state.transitionTime, 0, state.transitionTime);
        controlledObject.MeshRoot.transform.GetChild(0).localPosition *= 0;
        controlledObject.MeshRoot.transform.GetChild(0).localEulerAngles *= 0;
    }

    public void PlayStateAnim(ControlledObject controlledObject, State state, float transitionTime)
    {
        controlledObject.CrossFadeInFixedTime(state.animationStateName, transitionTime, 0, transitionTime);
        controlledObject.MeshRoot.transform.GetChild(0).localPosition *= 0;
        controlledObject.MeshRoot.transform.GetChild(0).localEulerAngles *= 0;
    }
}
