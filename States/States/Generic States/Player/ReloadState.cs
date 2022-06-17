using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Actions/ReloadState")]
public class ReloadState : ActionState
{
    [SerializeField, Header("Reload State")]
    private int reloadFrame = 0;

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);

        if(reloadFrame == controlledObject.stateMachine.actionFrame)
        {
            LimitedUseEquipment equip = controlledObject.GetEquipment(InputActions.Attack) as LimitedUseEquipment;
            if (equip)
            {
                equip.Refresh();
            }
        }
    }
}
