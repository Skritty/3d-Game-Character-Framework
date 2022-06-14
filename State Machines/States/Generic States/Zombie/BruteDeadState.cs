using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
[CreateAssetMenu(menuName = "State/Actions/BruteDeadState")]
public class BruteDeadState : ActionState
{
    [SerializeField] private SoundEffect deathRattle;

    public override void OnEnter(ControlledObject controlledObject)
    {
        base.OnEnter(controlledObject);
        controlledObject.stateMachine.actionFrame = 0;
        controlledObject.CrossFadeInFixedTime(animationStateName, transitionTime, 0, transitionTime);
        controlledObject.MeshRoot.DOLocalMoveY(0.15f, 0.25f);
        controlledObject.MeshRoot.transform.GetChild(0).localEulerAngles *= 0;
        controlledObject.iFrames = -1;
        //controlledObject.gameObject.layer = 11; // Ignore Object Layer

        controlledObject.GetComponent<CapsuleCollider>().enabled = false;

        if(deathRattle)
        {
            AudioManager.Instance.PlaySFX(deathRattle, controlledObject.transform.position);
        }
    }

    public override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);
        if (controlledObject.velocity.sqrMagnitude != 0)
        {
            controlledObject.velocity *= 0.95f;
            if (controlledObject.velocity.sqrMagnitude < 0.01f)
                controlledObject.velocity *= 0;
        }
        if(controlledObject.stateMachine.actionFrame >= maxFrame)
        {
            Destroy(controlledObject.gameObject);
        }

        //if (controlledObject.currentFrame >= maxFrame)
        //    Destroy(controlledObject.gameObject);
    }

	public override void OnExit(ControlledObject controlledObject)
    {
        base.OnExit(controlledObject);
        controlledObject.iFrames = 40;
        controlledObject.GetComponent<CapsuleCollider>().enabled = true;

    }
}
