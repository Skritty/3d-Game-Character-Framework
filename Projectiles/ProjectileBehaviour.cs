using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/Projectile Behaviour")]
public class ProjectileBehaviour : ScriptableObject
{
    [Header("Visuals")]
    [SerializeField]
    private GameObject _baseVisual;
    public GameObject BaseVisual => _baseVisual;

    [Header("Behaviour")]
    [SerializeField]
    private DamageInstance hitscanDamage;
    public DamageInstance HitscanDamage => hitscanDamage;

    [SerializeField]
    private Hitbox[] hitboxes;
    public Hitbox[] Hitboxes => hitboxes;

    [SerializeField]
    private ProjectileBehaviour[] _explosionProjectiles;
    public ProjectileBehaviour[] ExplosionProjectiles => _explosionProjectiles;

    [System.Flags]
    public enum ProjectileBehaviours 
    {
        None = 0,
        ExplodeOnImpact = 1,
        StickInSurface = 2,
        DestroyOnImpact = 4,
        HitWhileImpacted = 8,
        HitScan = 16
    }

    [SerializeField]
    private ProjectileBehaviours _behaviours;
    public ProjectileBehaviours Behaviours => _behaviours;

    [SerializeField]
    private float _projectileSpeed;
    public float ProjectileSpeed => _projectileSpeed;

    [SerializeField]
    private Vector3 _gravity;
    public Vector3 Gravity => _gravity;

    [SerializeField]
    private int _lifetime;
    public int Lifetime => _lifetime;
    
}
