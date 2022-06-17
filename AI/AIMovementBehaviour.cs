using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class AIMovementBehaviour : ScriptableObject
{
    [Header("Activation")]
    public int frequency;
    public ObjectType target;
    public ControlledObjectAllegiance allegiance = ControlledObjectAllegiance.Neutral;
    public bool canAttack;
    //public bool isWaiting;

    [Header("Info")]
    [Range(0f, 1f)]
    public float leanIntoMovement = 1f;

    public virtual bool IsValid(AIController controller, AIBehaviour.AIAttack chosenAttack, ObjectType targetType)
    {
        if (target.HasFlag(targetType)
            && chosenAttack != null == canAttack
            && (!(controller.controlledObject.target is ControlledObject) || ((controller.controlledObject.target as ControlledObject).allegiance == allegiance)))
        {
            return true;
        }
        return false;
    }
    public abstract Vector3 PickTargetLocation(AIController controller);
}
