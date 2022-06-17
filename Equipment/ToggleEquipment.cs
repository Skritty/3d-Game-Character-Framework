using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ToggleEquipment : Equipment
{
    public System.Action OnOffActivate { protected get; set; }
    public System.Func<bool> OffActivationCondition { protected get; set; }

    [SerializeField, Tooltip("LocomotionStates to swap out. A null state will keep the existing state instead of changing it.")]
    protected SerializedDictionary<Locomotion, LocomotionState> offLocomotionStates = new SerializedDictionary<Locomotion, LocomotionState>();
    [SerializeField]
    protected State offAction;
    [SerializeField]
    protected LocomotionState offLocomotion;

    protected bool on;

    public static T CreateEquipment<T>(ActionState onState, ActionState offState, LocomotionState onLocomotion = null, LocomotionState offLocomotion = null, Action OnActivate = null, Action OnOffActivate = null, Func<bool> ActivationCondition = null, Func<bool> OffActivationCondition = null, SerializedDictionary<Locomotion, LocomotionState> onlocomotionStates = null, SerializedDictionary<Locomotion, LocomotionState> offLocomotionStates = null) where T : ToggleEquipment
    {
        T equip = new GameObject($"{(onState ? onState.name : onLocomotion)} Equipment").AddComponent<T>();
        equip.locomotionStates = onlocomotionStates;
        equip.offLocomotionStates = offLocomotionStates;
        equip.action = onState;
        equip.offAction = offState;
        equip.locomotion = onLocomotion;
        equip.locomotion = offLocomotion;
        equip.OnActivate = OnActivate;
        equip.OnOffActivate = OnOffActivate;
        equip.ActivationCondition = ActivationCondition;
        equip.OffActivationCondition = OffActivationCondition;
        return equip;
    }

    public override void Activate(ControlledObject controlledObject, InputActions ia)
    {
        if (on)
        {
            if (OffActivationCondition != null && !OffActivationCondition.Invoke()) return;
            on = false;
            ToggleOff(controlledObject, ia);
        }
        else
        {
            if (ActivationCondition != null && !ActivationCondition.Invoke()) return;
            ToggleOn(controlledObject, ia);
            on = true;
        }
    }

    protected virtual void ToggleOn(ControlledObject controlledObject, InputActions ia)
    {
        OnActivate?.Invoke();

        if (locomotionStates != null)
            foreach (KeyValuePair<Locomotion, LocomotionState> locomotionState in locomotionStates)
            {
                controlledObject.locomotionStates[locomotionState.Key] = locomotionState.Value;
            }

        if (locomotion != null)
            controlledObject.stateMachine.SetLocomotionState(offLocomotion);
        if (action != null)
            controlledObject.stateMachine.SetActionState(offAction);
    }

    protected virtual void ToggleOff(ControlledObject controlledObject, InputActions ia)
    {
        OnOffActivate?.Invoke();

        if (offLocomotionStates != null)
            foreach (KeyValuePair<Locomotion, LocomotionState> locomotionState in offLocomotionStates)
            {
                controlledObject.locomotionStates[locomotionState.Key] = locomotionState.Value;
            }

        if (locomotion != null)
            controlledObject.stateMachine.SetLocomotionState(locomotion);
        if (action != null)
            controlledObject.stateMachine.SetActionState(action);
    }
}
