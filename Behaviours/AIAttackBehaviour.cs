using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Behaviours/AI Attack Behaviour")]
[System.Serializable]
public class AIAttackBehaviour : ScriptableObject
{
    public AttackState attack;
    public int cooldown;
    public int weight;
    public Vector2 attackTriggerRange;
    public State[] targetStateBlacklist;
    public State[] targetStateWhitelist;
}
