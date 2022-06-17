// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Projectile : TangibleObject
{
    private static Queue<Projectile> projectilePool;
    private static List<Projectile> activeProjectiles = new List<Projectile>();
    private static Transform root;
    [SerializeField]
    private GameObject explosion;
    [SerializeField]
    private GameObject defaultHitFX;
    [SerializeField]
    private LayerMask ignoreLayers;
    [SerializeField]
    private bool CausesSound = false;
    [ShowIf("CausesSound")]
    [SerializeField]
    private TangibleObject soundTarget;
    [ShowIf("CausesSound")]
    [SerializeField]
    private GameObject soundVFX;

    public static void InitializeProjectiles()
    {
        root = (new GameObject("Projectile Pool")).transform;
        if (projectilePool == null)
        {
            projectilePool = new Queue<Projectile>();
            for (int i = 0; i < Defaults.projectilePoolSize; i++)
            {
                InitializeProjectile();
            }
        }
        DontDestroyOnLoad(root);
    }

    private static void InitializeProjectile()
    {
        GameObject obj = Instantiate(ProjectileManager.Instance.defaultProjectile);
        obj.transform.parent = root;
        obj.SetActive(false);
        projectilePool.Enqueue(obj.GetComponent<Projectile>());
    }

    public static void ExpandPool(int amount = 1)
    {
        for (int i = 0; i < Defaults.projectilePoolSize; i++)
        {
            InitializeProjectile();
        }
    }

    public static void KillAll()
    {
        foreach (Projectile p in activeProjectiles.ToArray())
            p.Kill();
    }

    /// <summary>
    /// Fire a projectile that uses the static pool. Only supports mesh visuals currently.
    /// </summary>
    public static void FireProjectile(ProjectileBehaviour behaviour, Vector3 initialPosition, Quaternion initialRotation, ControlledObjectAllegiance allegiance = ControlledObjectAllegiance.Neutral)
    {
        if (projectilePool.Count == 0)
        {
            if (ProjectileManager.Instance.expandPoolAutomatically)
            {
                InitializeProjectile();
            }
            else
            {
                Debug.LogWarning("No more projectiles left in the pool");
                return;
            }
        }

        Projectile p = projectilePool.Dequeue();
        activeProjectiles.Add(p);
        p.transform.position = p.initialPosition = initialPosition;
        p.transform.rotation = p.initialRotation = initialRotation;
        p.gameObject.SetActive(true);
        p.behaviour = behaviour;
        p.allegiance = allegiance;
        p.mf.sharedMesh = behaviour.BaseVisual.GetComponent<MeshFilter>().sharedMesh;
        p.mr.sharedMaterials = behaviour.BaseVisual.GetComponent<MeshRenderer>().sharedMaterials;
    }

    /// <summary>
    /// Fire a projectile that uses instantiation.
    /// </summary>
    public static Projectile FireProjectile(GameObject projectile, Vector3 initialPosition, Quaternion initialRotation, ControlledObjectAllegiance allegiance = ControlledObjectAllegiance.Neutral)
    {
        Projectile p = Instantiate(projectile).GetComponent<Projectile>();
        p.transform.position = p.initialPosition = initialPosition;
        p.transform.rotation = p.initialRotation = initialRotation;
        p.allegiance = allegiance;
        return p;
    }

    [SerializeField]
    private ProjectileBehaviour behaviour;
    private MeshFilter mf => GetComponent<MeshFilter>();
    private MeshRenderer mr => GetComponent<MeshRenderer>();
    private TrailRenderer tr => GetComponent<TrailRenderer>();

    public ControlledObjectAllegiance allegiance;
    public int currentFrame = 0;
    public bool impacted = false;
    public Vector3 initialPosition;
    public Quaternion initialRotation;

    public new void Start()
    {
        base.Start();
        initialPosition = transform.position;
        if (soundVFX != null)
            soundVFX.transform.localScale = Vector3.one * behaviour.Hitboxes[0].radius * 2;
    }

    private void OnEnable()
    {
        attackID = Random.Range(0, 10000);
        currentFrame = 0;
        impacted = false;
    }

    private new void FixedUpdate()
    {
        base.FixedUpdate();
        // Kill it if its past its max time
        if (currentFrame == behaviour.Lifetime) 
            Kill();

        // If not stuck in a surface, move
        if (!behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.HitScan)
            && !(impacted && behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.StickInSurface)))
            Move();

        CheckHitboxes(behaviour.Hitboxes);
        if(behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.HitScan))
            CheckHitScan();
        currentFrame++;
    }

    protected virtual void Move()
    {
        transform.rotation = initialRotation;
        transform.position = initialPosition + transform.forward * (currentFrame) * behaviour.ProjectileSpeed 
            + behaviour.Gravity * 1 / 2f * Mathf.Pow((currentFrame) * Time.fixedDeltaTime, 2);
        Vector3 prevPos = initialPosition + transform.forward * (currentFrame - 1) * behaviour.ProjectileSpeed 
            + behaviour.Gravity * 1 / 2f * Mathf.Pow((currentFrame - 1) * Time.fixedDeltaTime, 2);
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, transform.position - prevPos);
    }

    protected void CheckHitboxes(Hitbox[] hitboxes)
    {
        foreach (Hitbox hitbox in hitboxes)
        {
            if (currentFrame < hitbox.activeFrames.x || currentFrame > hitbox.activeFrames.y) continue;

            Vector3 center = transform.rotation * Quaternion.Euler(hitbox.rotation) * hitbox.position + transform.position;
            Collider[] result = Physics.OverlapSphere(center, hitbox.radius, ignoreLayers);
            foreach (Collider collider in result)
            {
                CheckCollider(collider, hitbox.effects);
            }
        }
    }

    protected void CheckHitScan()
    {
        RaycastHit hit;
        if (Physics.Raycast(initialPosition, transform.forward, out hit, behaviour.ProjectileSpeed, ignoreLayers, QueryTriggerInteraction.Collide))
        {
            transform.position = hit.point;
            CheckCollider(hit.collider, behaviour.HitscanDamage);
            //Debug.Log($"Hit: {hit.collider.gameObject.name}");
        }
        else
        {
            transform.position += transform.forward * behaviour.ProjectileSpeed;
        }
    }

    protected void CheckCollider(Collider collider, DamageInstance damage)
    {
        TangibleObject obj = collider.GetComponent<TangibleObject>();
        if(obj == null)
            obj = collider.GetComponentInParent<TangibleObject>();
        Hurtbox hObj = collider.GetComponent<Hurtbox>();
        damage.impactOrigin = transform.position;
        if (obj is ControlledObject && (obj as ControlledObject).allegiance == allegiance) return;
        if (!impacted)
        {
            if (behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.ExplodeOnImpact))
            {
                foreach (ProjectileBehaviour p in behaviour.ExplosionProjectiles)
                    Projectile.FireProjectile(explosion, transform.position, transform.rotation, allegiance);
            }

            if (obj == null && defaultHitFX != null)
            {
                Instantiate(defaultHitFX, transform.position, transform.rotation);
            }

            if (hObj == null && obj == null && behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.StickInSurface))
                impacted = true;

            // Do the hit if hitting a tangibleObject
            if (hObj != null && obj != null && obj.gameObject != gameObject && obj.hurtID != attackID)
            {
                obj.hurtID = attackID;
                HitConfirm(hObj, damage);
            }

            if (behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.DestroyOnImpact))
                Kill();
        }
        else
        {
            if (behaviour.Behaviours.HasFlag(ProjectileBehaviour.ProjectileBehaviours.HitWhileImpacted))
            {
                // Do the hit if hitting a tangibleObject
                if (obj == null || obj.gameObject == gameObject || obj.hurtID == attackID) return;
                obj.hurtID = attackID;

                HitConfirm(hObj, damage);
            }
        }
    }

    public virtual void HitConfirm(Hurtbox obj, DamageInstance damage)
    {
        //Debug.Log($"{name} Hit {obj.gameObject}");
        damage.allegiance = allegiance;
        obj.TakeHit(damage);

        if(CausesSound && obj.TryGetComponent(out AIController ai) && !ai.HasTarget())
        {
            ai.SetTarget(Instantiate(soundTarget.gameObject, transform.position, transform.rotation).GetComponent<TangibleObject>());
        }
    }

    protected void Kill()
    {
        gameObject.SetActive(false);
        if (activeProjectiles.Contains(this))
        {
            activeProjectiles.Remove(this);
            projectilePool.Enqueue(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        foreach (Hitbox hitbox in behaviour.Hitboxes)
        {
            if (currentFrame >= hitbox.activeFrames.x && currentFrame <= hitbox.activeFrames.y)
            {
                Vector3 center = transform.rotation * Quaternion.Euler(hitbox.rotation) * hitbox.position + transform.position;
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(center, hitbox.radius);
                Gizmos.color = new Color(1, 0, 0, .5f);
                Gizmos.DrawSphere(center, hitbox.radius);
            }
        }
    }
}