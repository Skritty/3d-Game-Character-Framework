using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsObject : TangibleObject
{
    private Rigidbody rb => GetComponent<Rigidbody>();

    enum PhysicsType { Normal, KinematicUntilHit}
    [SerializeField]
    private PhysicsType physicsType = PhysicsType.Normal;
    [SerializeField]
    private ForceMode forceMode = ForceMode.VelocityChange;

    public override void Start()
    {
        base.Start();
        if (physicsType == PhysicsType.KinematicUntilHit)
            rb.isKinematic = true;
    }

    public override void TakeHit(DamageInstance damage)
    {
        switch (tangibility)
        {
            case ObjectTangibility.Invincible:
                break;

            case ObjectTangibility.Armor:
                currentHealth -= damage.damage;
                OnHit.Invoke(damage);
                if (currentHealth <= 0)
                {
                    Die(damage);
                }
                break;

            case ObjectTangibility.Normal:
                currentHealth -= damage.damage;
                OnHit.Invoke(damage);
                if(damage.damage >= 0)
                {
                    DoKnockback(damage);
                }
                if (currentHealth <= 0)
                {
                    Die(damage);
                }
                break;
        }
    }

    protected virtual void DoKnockback(DamageInstance damage)
    {
        if (physicsType == PhysicsType.KinematicUntilHit)
        {
            rb.isKinematic = false;
            this.RunFunctionOnDelay(() => KnockBack(), new WaitForFixedUpdate());
        }
        else
            KnockBack();

        void KnockBack()
        {
            Vector3 kbVel = Vector3.zero;
            kbVel = (GetPlanarRotation(Quaternion.identity) * (transform.position - damage.impactOrigin)).normalized * damage.knockback;
            rb.AddForceAtPosition((damage.impactOrigin - damage.damageOrigin).normalized * damage.knockback, damage.impactOrigin, forceMode);
        }
    }
}
