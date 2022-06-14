using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Action/PushState")]
public class PushState : AttackState
{
    public AttackState nextState;
    public override void HitConfirm(ControlledObject controlledObject, TangibleObject obj, Hitbox hitbox)
    {
        base.HitConfirm(controlledObject, obj, hitbox);
        if(controlledObject.controller is AIController && nextState != null)
        {
            
            (controlledObject.controller as AIController).chosenAttack.attack = nextState;
        }
    }
}
