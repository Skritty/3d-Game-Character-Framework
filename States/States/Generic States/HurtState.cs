using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "State/Actions/HurtState")]
public class HurtState : ActionState
{
	[Header("Hurt Data")]
	[SerializeField]
	public int iFrames;
	[SerializeField]
	private int armorFrames;
	public AnimationCurve hurtFriction;

    [SerializeField] private SoundEffect hurtSound;
	
    public override void OnEnter(ControlledObject controlledObject)
	{
		base.OnEnter(controlledObject);
		controlledObject.iFrames += iFrames;
		controlledObject.armorFrames += armorFrames;

        if (hurtSound)
        {
            AudioManager.Instance.PlaySFX(hurtSound, controlledObject.transform.position);
        }
    }

	public override void OnFixedUpdate(ControlledObject controlledObject)
	{
		base.OnFixedUpdate(controlledObject);
		controlledObject.velocity *= hurtFriction.Evaluate(controlledObject.stateMachine.actionFrame);
	}

    public override void OnExit(ControlledObject controlledObject)
    {
        base.OnExit(controlledObject);
	}
}