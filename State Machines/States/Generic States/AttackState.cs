// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "State/Actions/AttackState")]
public class AttackState : ActionState
{
    [Header("FX")]
    [SerializeField]
    protected AttackSFX[] SFX;
    [SerializeField]
    protected AttackVFX[] VFX;
    [SerializeField]
    protected GameObject hitVFX;
    [SerializeField]
    protected SoundEffect hitSFX;
    [SerializeField]
    protected AnimationCurve trailTimeCurve;
    [SerializeField]
    protected AnimationCurve trailtailWidthCurve;

    //[Header("Movement")]
    //[SerializeField]
    //protected AnimationCurve movementCurve;
    //[SerializeField]
    //protected AnimationCurve strafeCurve;
    //[SerializeField]
    ////[MinMaxSlider(0, 120)]
    //public Vector2Int[] readjustFrames;
    [MinMaxSlider(0, "@maxFrame")]
    public Vector2Int armorFrames;
    //public float readjustSmoothingTime;

    [Header("Data")]
    [SerializeField]
    public Hitbox[] hitboxes;
    [SerializeField]
    public ProjectileAttack[] projectiles;
    public State dodgeState;
    public int iasa;

    public sealed override void OnEnter(ControlledObject controlledObject)
    {
        base.OnEnter(controlledObject);
        controlledObject.velocity *= 0;
        controlledObject.attackID = Random.Range(0, 10000);
    }

	public sealed override void OnExit(ControlledObject controlledObject)
	{
		base.OnExit(controlledObject);
        if (controlledObject.weaponTrail)
            controlledObject.weaponTrail.enabled = false;
    }

	public sealed override void OnFixedUpdate(ControlledObject controlledObject)
    {
        base.OnFixedUpdate(controlledObject);
        //ReadjustMovement(controlledObject);
        //SetVelocity(controlledObject);
        SetWeaponTrail(controlledObject);
        CheckHitboxes(controlledObject);
        FireProjectiles(controlledObject);
        SetObjectTangibility(controlledObject);
        CreateVFX(controlledObject);
        CreateSFX(controlledObject);
    }

    //protected virtual void ReadjustMovement(ControlledObject controlledObject)//set this to read from input and then stop cancelling input on enter that way players can redirect based on input and enemies still have rotate
    //{
    //    Vector2 input = controlledObject.controller.GetInput<Vector2>(InputActions.Move);
    //    foreach (Vector2Int readjust in readjustFrames)
    //        if (controlledObject.currentFrame >= readjust.x && controlledObject.currentFrame <= readjust.y)
    //        {
    //            Vector3 moveInputVector;
    //            if (controlledObject.controller is PlayerController)
    //            {
    //                moveInputVector = (controlledObject.GetPlanarRotation(Camera.main.transform.rotation) * new Vector3(input.x, 0f, input.y)).normalized;
    //            }
    //            else
    //            {
    //                Vector3 dir = controlledObject.target ?  controlledObject.target.transform.position - controlledObject.transform.position  : controlledObject.Motor.CharacterForward;
    //                moveInputVector = (controlledObject.GetPlanarRotation(Quaternion.identity) * dir).normalized;
    //                moveInputVector = new Vector3(moveInputVector.x, 0f, moveInputVector.z);
    //            }

    //            // Movement
    //            controlledObject.velocity = moveInputVector;

    //            // Rotation
                
    //            //controlledObject.preferredRotation = Vector3.Lerp(controlledObject.preferredRotation, moveInputVector, readjustSmoothingTime).normalized;
    //        }

    //    //if (controlledObject.controller.input.GetBuffer(2) > 0 && dodgeState != null && controlledObject.currentFrame > iasa)
    //    //{
    //    //    controlledObject.stateMachine.ChangeState(dodgeState, false, ActionStateEnums.Move);
    //    //}
    //}

 //   public void SetVelocity(ControlledObject controlledObject)
	//{
 //       controlledObject.velocity = controlledObject.preferredRotation * movementCurve.Evaluate(controlledObject.currentFrame);
 //       controlledObject.velocity += Quaternion.AngleAxis(90, Vector3.up) * controlledObject.preferredRotation * strafeCurve.Evaluate(controlledObject.currentFrame);
 //   }

    public void SetWeaponTrail(ControlledObject controlledObject)
    {
        if (controlledObject.weaponTrail)
        {
            controlledObject.weaponTrail.time = trailTimeCurve.Evaluate(controlledObject.stateMachine.actionFrame);
                controlledObject.weaponTrail.gameObject.SetActive(trailTimeCurve.Evaluate(controlledObject.stateMachine.actionFrame) != 0);
            controlledObject.weaponTrail.endWidth = trailtailWidthCurve.Evaluate(controlledObject.stateMachine.actionFrame);
        }
	}

    public void SetObjectTangibility(ControlledObject controlledObject)
	{
        if (controlledObject.stateMachine.actionFrame >= armorFrames.x && controlledObject.stateMachine.actionFrame <= armorFrames.y)
            controlledObject.armorFrames++;
	}
    protected void CheckHitboxes(ControlledObject controlledObject)
    {
        foreach (Hitbox hitbox in hitboxes)
        {
            if (controlledObject.stateMachine.actionFrame < hitbox.activeFrames.x || controlledObject.stateMachine.actionFrame > hitbox.activeFrames.y) continue;

            Vector3 center = controlledObject.transform.rotation * Quaternion.Euler(hitbox.rotation) * hitbox.position + controlledObject.transform.position;
            Collider[] result = Physics.OverlapSphere(center, hitbox.radius);
            foreach (Collider collider in result)
            {
                TangibleObject obj = collider.GetComponent<TangibleObject>();
                if (obj is ControlledObject && (obj as ControlledObject).allegiance == controlledObject.allegiance) continue;
                if (obj == null || obj.gameObject == controlledObject.gameObject || obj.hurtID == controlledObject.attackID || (obj as ControlledObject)?.currentHealth <=  0) continue;

                obj.hurtID = controlledObject.attackID;
                hitbox.effects.allegiance = controlledObject.allegiance;
                hitbox.effects.damageOrigin = controlledObject.transform.position;//center;
                hitbox.effects.impactOrigin = collider.ClosestPoint(center);

                if(hitVFX)
                    Instantiate(hitVFX, collider.ClosestPoint(center), Quaternion.identity);
                if (hitSFX)
                    AudioManager.Instance?.PlaySFX(hitSFX, collider.ClosestPoint(center));

                HitConfirm(controlledObject, obj, hitbox);
            }
        }
    }

    protected void FireProjectiles(ControlledObject controlledObject)
    {
        Transform projectileFirePoint = (controlledObject.GetEquipment(InputActions.Attack) as ProjectileEquipment)?.projectileFirePoint;
        if (projectileFirePoint == null) return;
        foreach (ProjectileAttack projectile in projectiles)
        {
            if (projectile.behaviour == null || controlledObject.stateMachine.actionFrame != projectile.fireOnFrame) continue;

            //controlledObject.projectileFirePoint.rotation
            //Projectile.FireProjectile(projectile.behaviour, controlledObject.projectileFirePoint.position + projectile.projectilePositionOffset, controlledObject.projectileFirePoint.rotation * Quaternion.Euler(projectile.projectileRotationOffset), controlledObject.allegiance);
            Projectile.FireProjectile(projectile.projectilePrefab,
                controlledObject.cameraEquipmentRoot.position + projectile.projectilePositionOffset,
                controlledObject.cameraEquipmentRoot.rotation * Quaternion.Euler(projectile.projectileRotationOffset),
                //Quaternion.FromToRotation(Vector3.forward, controlledObject.preferredRotation) * Quaternion.Euler(projectile.projectileRotationOffset), 
                controlledObject.allegiance);

            if (projectile.VFX)
            {
                GameObject obj = Instantiate(projectile.VFX, projectileFirePoint.position, projectileFirePoint.rotation);
                obj.transform.parent = null;
            }

            if (projectile.SFX)
            {
                AudioManager.Instance.PlaySFX(projectile.SFX, projectileFirePoint.position);
            }

            if(projectile.Screenshake > 0f)
			{
                controlledObject.GetEquipment(InputActions.Attack).ImpulseSource?.GenerateImpulse(projectile.Screenshake);
			}

            if (projectile.Recoil > 0f)
            {
                controlledObject.GetEquipment(InputActions.Attack).RecoilSource?.GenerateImpulse(projectile.Recoil * Camera.main.transform.forward);
            }
        }
    }

    public virtual void HitConfirm(ControlledObject controlledObject, TangibleObject obj, Hitbox hitbox)
    {
        switch (obj.tangibility)
        {
            case ObjectTangibility.Invincible:
                obj.TakeHit(hitbox.effects);
                break;

            case ObjectTangibility.Armor:
                obj.TakeHit(hitbox.effects);
                break;

            case ObjectTangibility.Normal:
                obj.TakeHit(hitbox.effects);
                break;
        }
    }

    public void CreateVFX(ControlledObject controlledObject)
    {
        foreach (AttackVFX vfx in VFX)
        {
            if (controlledObject.stateMachine.actionFrame == vfx.frame)
            {
                GameObject obj = Instantiate(vfx.VFX, controlledObject.transform);
                obj.transform.localPosition = vfx.position;
                obj.transform.localEulerAngles = vfx.rotation;
                obj.transform.parent = null;
            }
        }
    }

    public void CreateSFX(ControlledObject controlledObject)
    {
        foreach (AttackSFX sfx in SFX)
        {
            if (controlledObject.stateMachine.actionFrame == sfx.frame)
            {
                AudioManager.Instance.PlaySFX(sfx.SFX, controlledObject.transform.position + sfx.localPosition);
            }
        }
    }
}