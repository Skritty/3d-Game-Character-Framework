using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Equipment is something that changes a controlled object's states and animations
/// </summary>
public class Equipment : MonoBehaviour
{
    public Action OnActivate { protected get;  set; }
    public Func<bool> ActivationCondition { protected get; set; }

    [Header("Equipment Data")]
    [SerializeField, Tooltip("LocomotionStates to swap out. A null state will keep the existing state instead of changing it.")]
    protected SerializedDictionary<Locomotion, LocomotionState> locomotionStates = new SerializedDictionary<Locomotion, LocomotionState>();

    [SerializeField]
    protected Vector3 positionOffset;
    [SerializeField, Tooltip("The ActionState to switch to if this equipment is activated")]
    protected State action;
    [SerializeField, Tooltip("The type of Locomotion to switch to if this equipment is activated")]
    protected LocomotionState locomotion;
    [SerializeField]
    protected bool useHoldInput;


    [Header("Equipment Refs")]
    [SerializeField, Tooltip("New equipment animator. A null value will clear the old one")]
    public Animator animator;
    public Cinemachine.CinemachineImpulseSource ImpulseSource;
    public Cinemachine.CinemachineImpulseSource RecoilSource;

    public static T CreateEquipment<T>(ActionState state, LocomotionState locomotion = null, Action OnActivate = null, Func<bool> ActivationCondition = null, SerializedDictionary<Locomotion, LocomotionState> locomotionStates = null) where T : Equipment
    {
        T equip = new GameObject($"{(state ? state.name : locomotion)} Equipment").AddComponent<T>();
        equip.locomotionStates = locomotionStates;
        equip.action = state;
        equip.locomotion = locomotion;
        equip.OnActivate = OnActivate;
        equip.ActivationCondition = ActivationCondition;
        return equip;
    }

    public virtual void OnEquip(ControlledObject controlledObject, InputActions ia)
    {
        transform.localPosition += positionOffset;
        controlledObject.controller.SetInput(ia, useHoldInput);
    }

    public virtual void Activate(ControlledObject controlledObject, InputActions ia)
    {
        if (ActivationCondition != null && !ActivationCondition.Invoke()) return;
        OnActivate?.Invoke();

        if(locomotionStates != null)
            foreach (KeyValuePair<Locomotion, LocomotionState> locomotionState in locomotionStates)
            {
                controlledObject.locomotionStates[locomotionState.Key] = locomotionState.Value;
            }

        if(locomotion != null)
            controlledObject.stateMachine.SetLocomotionState(locomotion);
        if (action != null)
            controlledObject.stateMachine.SetActionState(action);
    }
}
