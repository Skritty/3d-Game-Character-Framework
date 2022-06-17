using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State/Actions/SwapWeaponsState")]
public class SwapWeaponsState : ActionState
{
    protected override void Exit(ControlledObject controlledObject)
    {
        PlayerController player = controlledObject.controller as PlayerController;
        if (player)
        {
            float direction = player.GetInput<float>(InputActions.Scroll);
            Equipment prev = controlledObject.GetEquipment(InputActions.Attack);

            int index = player.weapons.IndexOf(prev);
            if (direction > 0)
            {
                index = (index + 1) % player.weapons.Count;
            }
            else if (direction < 0)
            {
                index--;
                if (index < 0)
                    index = player.weapons.Count - 1;
            }

            Equipment next = player.weapons[index];
            controlledObject.Equip(next, next.transform.parent, InputActions.Attack);
            prev.gameObject.SetActive(false);
            next.gameObject.SetActive(true);
        }
        base.Exit(controlledObject);
    }
}
