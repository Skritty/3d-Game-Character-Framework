using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TangibleObject : MonoBehaviour
{
    public static List<TangibleObject> tangibleObjects = new List<TangibleObject>();

    public UnityEvent<DamageInstance> OnHit;
    public UnityEvent OnDie;

    public ObjectTangibility baseTangibility;
    public ObjectTangibility tangibility;
    public int iFrames = 0;
    public int armorFrames = 0;
    public bool canBeAttacked = true;
    [SerializeField]
    private int maximumHealth;
    public int currentHealth;
    public void ResetHealth() => currentHealth = maximumHealth;
    [Tooltip("Multipler for when this tangible object is being detected.")]
    public float detectedMultiplier = 1;
    [SerializeField]
    private GameObject recieveHitFX;

    [HideInInspector]
    public int hurtID;
    //[HideInInspector]
    public int attackID;
    public bool bigHurt;

    /// <summary>
    /// Transform movement by this rotation.
    /// </summary>
    /// <param name="viewRotation">The camera rotation. Use Quaternion.identity if no camera.</param>
    /// <returns>The rotation to transform by</returns>
    public Quaternion GetPlanarRotation(Quaternion viewRotation)
    {
        Vector3 planarDirection = Vector3.ProjectOnPlane(viewRotation * Vector3.forward, transform.up).normalized;
        if (planarDirection.sqrMagnitude == 0f)
            planarDirection = Vector3.ProjectOnPlane(viewRotation * Vector3.up, transform.up).normalized;
        return Quaternion.LookRotation(planarDirection, transform.up);
    }

    public virtual void Start()
    {
        tangibility = baseTangibility;
        currentHealth = maximumHealth;
        OnHit.AddListener(OnHitFX);
    }

    protected void FixedUpdate()
    {
        FindState();
    }

    private void OnEnable()
    {
        tangibleObjects.Add(this);
    }

    private void OnDisable()
    {
        tangibleObjects.Remove(this);
    }

    protected virtual void FindState()
    {
        if (iFrames != 0)
            tangibility = ObjectTangibility.Invincible;
        else if (armorFrames != 0)
            tangibility = ObjectTangibility.Armor;
        else
            tangibility = baseTangibility;

        if(iFrames > 0) 
            iFrames--;
        if(armorFrames > 0)
            armorFrames--;
    }

    public virtual void TakeHit(DamageInstance damage)
    {
        switch (tangibility)
        {
            case ObjectTangibility.Invincible:
                if (currentHealth <= 0)
                {
                    Die(damage);
                }
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
                if (damage.armorPierce)
                    bigHurt = true;
                OnHit?.Invoke(damage);
                if (currentHealth <= 0)
                {
                    Die(damage);
                }
                break;
        }
    }

    public virtual void Die(DamageInstance damage)
    {
        OnDie.Invoke();
    }

    protected virtual void OnHitFX(DamageInstance damage)
    {
        if (recieveHitFX)
            Instantiate(recieveHitFX, damage.impactOrigin, Camera.main.transform.rotation);
    }

    public virtual void Reset()
    {
        currentHealth = maximumHealth;
        tangibility = baseTangibility;
        armorFrames = 0;
        iFrames = 0;
    }
}