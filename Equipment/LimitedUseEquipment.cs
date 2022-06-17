using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LimitedUseEquipment : Equipment
{
    [field: SerializeField, Header("Limited Use Data"), Tooltip("This is what refreshing pulls from. If less than 0, it will consider it infinite.")]
    public int PoolAmount { get; set; }
    [field: SerializeField, Tooltip("This is how much is taken from the pool. If less than 0, the current amount will be considered infinite.")]
    public int RefreshAmount { get; private set; } = -1;
    [field: SerializeField, Tooltip("This is how much is subtracted from the current amount. The action will only be triggered if there would be 0 or more remaining after use.")]
    public int Cost { get; private set; } = 1;
    [field: SerializeField]
    public int CurrentAmount { get; set; } = 0;
    [SerializeField]
    private bool startAtMax = true;
    [SerializeField]
    private bool emptyCurrent = false;
    [SerializeField]
    private bool reloadIfEmpty = true;
    [SerializeField]
    private ActionState refreshState;
    private Equipment reload;

    protected void Start()
    {
        if (startAtMax)
        {
            CurrentAmount = RefreshAmount;
            PoolAmount -= RefreshAmount;
        }
    }

    public override void OnEquip(ControlledObject controlledObject, InputActions ia)
    {
        base.OnEquip(controlledObject, ia);
        if (reload != null)
        {
            controlledObject.Equip(reload, transform, InputActions.Reload);
        }
        else
        {
            reload = CreateEquipment<Equipment>(refreshState);
            reload.ActivationCondition = () => CurrentAmount != RefreshAmount && PoolAmount > 0;
            controlledObject.Equip(reload, transform, InputActions.Reload);
        }
    }

    public void AddFromPool(int amount, bool capAtRefresh = true)
    {
        if (emptyCurrent)
            CurrentAmount = 0;
        for(int i = 0; i < amount; i++)
        {
            if (PoolAmount == 0 || capAtRefresh && CurrentAmount >= RefreshAmount) break;
            PoolAmount--;
            CurrentAmount++;
        }
    }

    public void Refresh()
    {
        AddFromPool(RefreshAmount);
    }

    public override void Activate(ControlledObject controlledObject, InputActions ia)
    {
        if(CurrentAmount <= -1 || (CurrentAmount - Cost) >= 0)
        {
            CurrentAmount -= Cost;
            controlledObject.stateMachine.SetActionState(action);
        }
        else if(reloadIfEmpty && PoolAmount > 0)
        {
            reload.Activate(controlledObject, ia);
        }
    }
}
