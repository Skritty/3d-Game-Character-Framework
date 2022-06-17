using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : Singleton<ProjectileManager>
{
    public GameObject defaultProjectile;
    public bool expandPoolAutomatically = false;

    private void Start()
    {
        Projectile.InitializeProjectiles();
    }
}
