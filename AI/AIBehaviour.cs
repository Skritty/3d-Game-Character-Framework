// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Behaviours/AI Behaviour")]
public class AIBehaviour : ScriptableObject
{
    [Tooltip("Keep this value very low, it is how often per frame it decides to attack instead of move")]
    [SerializeField]
    [Range(0f, 1f)]
    float _aggression;
    public float Aggression => _aggression;

    [Header("Targeting")]

    [SerializeField]
    Vector3 _eyeLocalOffset;
    public Vector3 EyeLocalOffset => _eyeLocalOffset;

    [SerializeField]
    [Range(0, 360)]
    int _sightAngle = 45;
    public int SightAngle => _sightAngle;

    [SerializeField]
    Vector2 _targetingDistanceMinMax;
    public Vector2 TargetingDistanceMinMax => _targetingDistanceMinMax;

    [SerializeField]
    Vector2Int _continueTrackingFrameDurationRange;
    public Vector2Int ContinueTrackingFrameDurationRange => _continueTrackingFrameDurationRange;

    [SerializeField]
    LayerMask _sightBlockerMask = ~0;
    public LayerMask SightBlockerMask => _sightBlockerMask;

    [SerializeField]
    private ControlledObjectAllegiance _targetingPriority;
    public ControlledObjectAllegiance TargetingPriority => _targetingPriority;

    [SerializeField]
    private bool _ignoreAllies;
    public bool IgnoreAllies => _ignoreAllies;

    [SerializeField]
    private State[] _targetStateBlacklist;
    public State[] TargetStateBlacklist => _targetStateBlacklist;

    [SerializeField]
    private State[] _targetStateWhitelist;
    public State[] TargetStateWhitelist => _targetStateWhitelist;

    [SerializeField, Tooltip("Spreads the same target to other AI on the same team in this range if they have no target")]
    private float _targetShareRange;
    public float TargetShareRange => _targetShareRange;

    [SerializeField, Tooltip("Rate that detection is filled every check.")]
    private float _detectionRate = 5f;
    public float DetectionRate => _detectionRate;


    [Header("Movement: General")]
    [SerializeField]
    private AIMovementOption[] _movementBehaviour;
    public AIMovementOption[] MovementBehaviour => _movementBehaviour;

    [SerializeField]
    [MinMaxSlider(0, 180)]
    Vector2Int _movementPositionPickDelay;
    public Vector2Int MovementPositionPickDelay => _movementPositionPickDelay;

    [SerializeField]
    [MinMaxSlider(0, 180)]
    Vector2Int _arrivedDelay;
    public Vector2Int ArrivedDelay => _arrivedDelay;


    [Header("Movement: Wander")]

    [SerializeField]
    float _wanderMaximumStraightPathLength;
    public float WanderMaximumStraightPathLength => _wanderMaximumStraightPathLength;

    [SerializeField]
    int wanderPathLength;
    public int WanderPathLength => wanderPathLength;

    [SerializeField]
    [Range(0, 2f)]
    float _wanderSpeedMultiplier = 1;
    public float WanderSpeedMultiplier => _wanderSpeedMultiplier;


    [Header("Movement: Return")]

    [SerializeField]
    Vector2Int _minMaxDistFromHome;
    public Vector2Int MinMaxDistFromHome => _minMaxDistFromHome;


    [Header("Movement: Follow")]
    [SerializeField]
    Vector2 _minMaxFollowRange;
    public Vector2 MinMaxFollowRange => _minMaxFollowRange;


    [Header("Movement: Approach")]
    [SerializeField]
    float _approachDistance;
    public float ApproachDistance => _approachDistance;

    [Header("Movement: Retreat")]
    [SerializeField]
    float _retreatDistance;
    public float RetreatDistance => _retreatDistance;

    [Header("Movement: Path")]
    [SerializeField]
    float _pathPointDistanceThreshold = .5f;
    public float PathPointDistanceThreshold => _pathPointDistanceThreshold;
    [SerializeField]
    float _chanceToTurnAround = .25f;
    public float ChanceToTurnAround => _chanceToTurnAround;
    [SerializeField]
    float _pathMoveSpeed = 1f;
    public float PathMoveSpeed => _pathMoveSpeed;
    [MinMaxSlider(0, 20)]
    [SerializeField]
    Vector2 _pointReachedWaitTime = new Vector2(0, 3);
    public Vector2 PointReachedWaitTime => _pointReachedWaitTime;

    [Header("Movement: Rotate")]
    [SerializeField]
    float _rotateTime = 1f;
    public float RotateTime => _rotateTime;


    [Header("Actions")]

    [SerializeField]
    AIAttack[] _attacks;
    public AIAttack[] Attacks => _attacks;

    #region Containers
    [System.Serializable]
    public class AIAttack
    {
        public AttackState attack;
        public int cooldown;
        public int weight;
        public Vector2 attackTriggerRange;
        public State[] targetStateBlacklist;
        public State[] targetStateWhitelist;
    }

    [System.Serializable]
    public class AIMovementOption
    {
        [Header("Activation")]
        public int frequency;
        public ObjectType target;
        public ControlledObjectAllegiance allegiance = ControlledObjectAllegiance.Neutral;
        public bool canAttack;
        public bool isWaiting;

        [Header("Info")]
        public AIMovementType movementType;
        [Range(0f, 1f)]
        public float leanIntoMovement = 1f;
    }
    #endregion
}
