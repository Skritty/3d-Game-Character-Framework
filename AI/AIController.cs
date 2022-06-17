// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;

public class AIController : BaseObjectController
{
    [Header("AI Behaviour")]
    // Static information on how this specific entity thinks 
    [SerializeField]
    public AIBehaviour behaviour;

    // Currently Chosen
    private bool manualMode = false;
    private Coroutine manualModeUpdateLoop;
    public Vector3 targetLocation;
    [SerializeField]
    public AIBehaviour.AIAttack chosenAttack;
    [HideInInspector]
    public float chosenDistance;

    // Contains the times of when attacks end
    public Dictionary<AIBehaviour.AIAttack, int> attackCooldowns = new Dictionary<AIBehaviour.AIAttack, int>();

    // Movement
    [HideInInspector]
    public Transform staticTransform;
    [HideInInspector]
    public Transform moveableTransform;
    private NavMeshPath path;
    [Range(0f, 1f)]
    public float leanIntoMovement = 0f;
    
    [HideInInspector]
    public Vector3 targetChaseOffsetDir;
    private int targetChaseOffsetRepickDelay;
    
    //Old
    private AIMovementType movementType = AIMovementType.None;

    
    private int movementWeightPick = 0;
    

    public Vector3 home;
    public Transform wanderZoneCenter;
    public float wanderZoneRadius;

    private int waitDelay = 0;
    private int chaseTargetPickDelay = 0;
    private int moveTargetPickDelay = 0;
    
    // Tracking
    private TangibleObject currentTarget;
    private int trackingTime = 0;
    private float currentDetection = 0;
    private const float detectionThreshold = 100;
    public Bark targetSpotted;
    [SerializeField] bool willAlwaysTrackTarget = true;

    [Header("Visualization")]
    [SerializeField]
    [Range(0, 10)]
    private int attackDrawn;
    [SerializeField]
    [Range(0, 180)]
    private int animFrameDrawn;
    [SerializeField]
    [MinMaxSlider(0, 180)]
    public Vector2Int framesDrawn;
    Light sightLight;
    [SerializeField]
    Color undetectedColor = Color.yellow;
    [SerializeField]
    Color detectedColor = Color.red;

    [Header("Pathing Behaviour")]
    [SerializeField] AIPathWaypoint aiPathStartPoint;
     AIPathWaypoint currentWaypoint;
     AIPathWaypoint lastWaypoint;
    bool waitingAtPoint = false;
    float defaultMoveSpeed;
    float pathingWait = 1;

    [Header("Rotate Only Behaviour")]
    [SerializeField] Transform leftTransform;
    [SerializeField] Transform rightTransform;
    Transform currentRotateTransform;
    float currentRotateTime;
    bool waitingWhileRotating = false;

    [Header("ManualMode")]
    [SerializeField] bool ShouldEndIfDestReached;
    [ShowIf("ShouldEndIfDestReached")]
    public float manualDistanceThreshold = 1f;
    [ShowIf("ShouldEndIfDestReached")]
    public UnityEngine.Events.UnityEvent ManualDestinationReached;
    private void Start()
    {
        path = new NavMeshPath();
        home = transform.position;
        moveableTransform = (new GameObject($"{gameObject.name}'s Moveable Transform")).transform;
        moveableTransform.parent = transform;
        targetLocation = transform.position;
        if (behaviour)
        {
            foreach (AIBehaviour.AIAttack attack in behaviour.Attacks)
            {
                attackCooldowns.Add(attack, 0);
                controlledObject.Equip(Equipment.CreateEquipment<Equipment>(attack.attack), controlledObject.transform, InputActions.Attack);
            }
            currentRotateTime = behaviour.RotateTime;
        }

        currentWaypoint = aiPathStartPoint;
        defaultMoveSpeed = controlledObject.MovementSpeedMultiplier;
        currentRotateTransform = Random.Range(0,1) == 1 ? rightTransform : leftTransform;


        sightLight = GetComponentInChildren<Light>();
    }

    protected new void FixedUpdate()
    {
        base.FixedUpdate();
        DetermineAction();
    }

    private void DetermineAction()
    {
        // Timers
        DoAlways();

        // Logic
        if(!manualMode && behaviour)
            switch (controlledObject.stateMachine.CurrentLocomotion)
            {
                case Locomotion.Move:
                    DoWhenMoveState();
                    break;
                case Locomotion.Idle:
                    DoWhenIdleState();
                    break;
                case Locomotion.Fall:
                    break;
                case Locomotion.Jump:
                    break;
            }

        // Inputs
        CarryOutInputs();
    }

    private void CarryOutInputs()
    {
        // Can't do inputs while dead!
        if (controlledObject.currentHealth <= 0) return;

        // If the target destination is reached or aggressive, attack
        if ((behaviour && IsAttackValid(chosenAttack) && Random.Range(0f, 1f) < behaviour.Aggression) || !MoveToTarget())
            AttackTarget();
    }

    /// <summary>
    /// Enables manual mode, where it use the set targets for its logic. Will not attack while in manual mode.
    /// Set the leanIntoMovement variable to adjust how much the AI leans into the attack.
    /// </summary>
    /// <param name="target">The look target</param>
    /// <param name="location">The location to move to</param>
    public void EnableManualMode(TangibleObject target, Transform location)
    {
        if(manualModeUpdateLoop != null)
            StopCoroutine(manualModeUpdateLoop);

        manualMode = true;
        controlledObject.target = currentTarget = target;
        if (location)
            manualModeUpdateLoop = StartCoroutine(UpdateTargetLocation(location));
        else
            targetLocation = transform.position;

        IEnumerator UpdateTargetLocation(Transform l)
        {
            while (manualMode)
            {
                targetLocation = l.position;
                yield return new WaitForFixedUpdate();
                if (ShouldEndIfDestReached && Vector3.Distance(transform.position, l.position) < manualDistanceThreshold)
                {
                    manualMode = false;
                    ManualDestinationReached.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Enables manual mode, where it use the set targets for its logic. Will not attack while in manual mode.
    /// Will go to target Transform.
    /// </summary>
    /// <param name="location">The location to move to</param>
    public void EnableManualMoveTo(Transform location)
    {
        if (manualModeUpdateLoop != null)
            StopCoroutine(manualModeUpdateLoop);

        manualMode = true;
        if (location)
            manualModeUpdateLoop = StartCoroutine(UpdateTargetLocation(location));
        else
            targetLocation = transform.position;

        IEnumerator UpdateTargetLocation(Transform l)
        {
            while (manualMode)
            {
                targetLocation = l.position;
                yield return new WaitForFixedUpdate();
                if(ShouldEndIfDestReached && Vector3.Distance(transform.position, l.position) < manualDistanceThreshold)
                {
                    manualMode = false;
                    ManualDestinationReached.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Enables manual mode, where it use the set targets for its logic. Will not attack while in manual mode.
    /// Will just look at target tangible object
    /// </summary>
    /// <param name="target">The look target</param>
    public void ManualLookAt(TangibleObject target)
    {
        if(manualModeUpdateLoop != null)
            StopCoroutine(manualModeUpdateLoop);

        manualMode = true;
        controlledObject.target = currentTarget = target;
    }

    public void DisableManualMode()
    {
        manualMode = false;
        controlledObject.target = currentTarget = null;
        targetLocation = transform.position;
        if (manualModeUpdateLoop != null)
            StopCoroutine(manualModeUpdateLoop);
    }

    int pathIndex = 0;
    private bool MoveToTarget()
    {
        bool moved = false;

        // If we haven't reached the target rotation, rotate
        if (controlledObject.target)
        {
            Vector3 dir = (controlledObject.target.transform.position - controlledObject.transform.position);
            if (Vector3.Angle(controlledObject.transform.forward, dir) > .1f)
            {
                controlledObject.preferredRotation = (new Vector3(dir.x, 0f, dir.z)).normalized;
                moved = true;
            }
        }
        else controlledObject.preferredRotation = controlledObject.transform.forward;

        // When we finish the path, find a new path
        if (path.corners.Length == 0 || pathIndex + 2 >= path.corners.Length || (pathingWait > Mathf.Lerp(1, 15, (Vector3.Distance(controlledObject.transform.position, targetLocation) / 96) - (1f / 24))))
        {
            NavMeshHit hit;
            NavMesh.SamplePosition(controlledObject.transform.position, out hit, 2f, 1);

            if (hit.hit)
                NavMesh.CalculatePath(hit.position, targetLocation, new NavMeshQueryFilter() { agentTypeID = 0, areaMask = 1 }, path);

            pathIndex = 0;
            pathingWait = 0;
        }

        pathingWait += Time.fixedDeltaTime;

        // If it hasn't finished the path, move
        if (pathIndex + 1 == path.corners.Length || path.status != NavMeshPathStatus.PathComplete || path.corners.Length <= 1)
        {
            controlledObject.velocity = Vector3.zero;
            controlledObject.Motor.BaseVelocity = Vector3.zero;
            SetInput(InputActions.Move, Vector2.zero);
            moved = false;
        }
        else
        {
            Vector3 v = (path.corners[pathIndex + 1] - path.corners[pathIndex]).normalized;
            SetInput(InputActions.Move, new Vector2(v.x, v.z));

            for (int i = 0; i < path.corners.Length - 1; i++)
                if (i == pathIndex)
                    Debug.DrawLine(path.corners[pathIndex], path.corners[pathIndex + 1], Color.blue);
                else
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);

            if (Vector3.Distance(path.corners[pathIndex + 1], controlledObject.transform.position) < Vector3.Distance(path.corners[pathIndex], controlledObject.transform.position))
                pathIndex++;

            moved = true;
        }


        // Modify rotation based on movement
        Vector2 input = controlledObject.controller.GetInput<Vector2>(InputActions.Move);
        Vector3 lookDir = new Vector3(input.x, 0f, input.y).normalized;
        controlledObject.preferredRotation = Vector3.Lerp(controlledObject.preferredRotation, lookDir, leanIntoMovement);

        
        //if (distance > controlledObject.MoveSpeed * Time.fixedDeltaTime)
        //{

        //}
        //else
        //{
        //    arrived = true;
        //    controlledObject.velocity = Vector3.zero;
        //    controlledObject.Motor.BaseVelocity = Vector3.zero;
        //    input.BufferMovement(Vector2.zero);
        //}

        // Look Dir Visualization
        //Debug.DrawRay(controlledObject.transform.position, controlledObject.preferredRotation);

        return moved;
    }

    //private bool MoveToTarget()
    //{
    //    float distance = Vector3.Distance(controlledObject.transform.position, targetLocation);//Vector3.Distance(controlledObject.transform.position, new Vector3(targetLocation.x, controlledObject.transform.position.y, targetLocation.z));
    //    if (distance == 0) return false;
    //    //if(pathingWait < Mathf.Lerp(0, 500 * Time.deltaTime, (distance / 96) - (1f/24)))
    //    //{
    //    //    pathingWait += Time.fixedDeltaTime;
    //    //}
    //    //else
    //    //{
    //    //    NavMeshHit hit;
    //    //    NavMesh.SamplePosition(controlledObject.transform.position + (targetLocation - controlledObject.transform.position).normalized * .25f, out hit, 2f, 1);

    //    //    // Next, check if theres a path it can take to the target location
    //    //    if (hit.hit)
    //    //        NavMesh.CalculatePath(hit.position, targetLocation, NavMesh.AllAreas, path);

    //    //    pathingWait = 0;
    //    //}
    //    if (arrived)
    //    {
    //        NavMeshHit hit;
    //        NavMesh.SamplePosition(controlledObject.transform.position + (targetLocation - controlledObject.transform.position).normalized * .25f, out hit, 2f, 1);

    //        // Next, check if theres a path it can take to the target location
    //        if (hit.hit)
    //            NavMesh.CalculatePath(hit.position, targetLocation, NavMesh.AllAreas, path);
    //        pathIndex = 0;
    //        arrived = false;
    //    }

    //    bool moved = false;

    //    // If we haven't reached the target rotation, rotate
    //    if (controlledObject.target)
    //    {
    //        Vector3 dir = (controlledObject.target.transform.position - controlledObject.transform.position);//controlledObject.GetPlanarRotation(Quaternion.identity) * (controlledObject.target.transform.position - controlledObject.transform.position);
    //        if (Vector3.Angle(controlledObject.transform.forward, dir) > .1f)
    //        {
    //            controlledObject.preferredRotation = (new Vector3(dir.x, 0f, dir.z)).normalized;
    //            moved = true;
    //        }
    //    }
    //    else controlledObject.preferredRotation = controlledObject.transform.forward;

    //    // If it hasn't reached the target position, move
    //    if (distance > controlledObject.MoveSpeed * Time.fixedDeltaTime)
    //    {
    //        // Do movement input and animations
    //        Vector3 v;
    //        if (path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
    //        {
    //            Debug.Log($"Good path from {gameObject.name}");
    //            if (pathIndex + 1 < path.corners.Length && Vector3.Distance(path.corners[pathIndex], controlledObject.transform.position) < Vector3.Distance(path.corners[pathIndex + 1], controlledObject.transform.position))
    //                pathIndex++;
    //            v = (path.corners[pathIndex + 1] - path.corners[pathIndex]).normalized;//(path.corners[pathIndex] - controlledObject.transform.position).normalized;
    //            input.BufferMovement(new Vector2(v.x, v.z));
    //        }
    //        else
    //        {
    //            Debug.Log($"Bad path from {gameObject.name}");
    //            //v = (targetLocation - controlledObject.transform.position).normalized;
    //        }
            
    //        //controlledObject.anim.SetFloat("Move Speed", controlledObject.AnimationSpeed);
    //        //Vector3 moveDir = controlledObject.transform.InverseTransformDirection(v).normalized;
    //        //controlledObject.anim.SetFloat("Velocity X", moveDir.x);
    //        //controlledObject.anim.SetFloat("Velocity Z", moveDir.z);

    //        // Modify rotation based on movement
    //        Vector3 lookDir = new Vector3(controlledObject.controller.input.inputVector.x, 0f, controlledObject.controller.input.inputVector.y).normalized;//(controlledObject.GetPlanarRotation(Quaternion.identity) * new Vector3(controlledObject.controller.input.inputVector.x, 0f, controlledObject.controller.input.inputVector.y)).normalized;
    //        controlledObject.preferredRotation = Vector3.Lerp(controlledObject.preferredRotation, lookDir, leanIntoMovement);

    //        // Path Visualization
    //        if (path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
    //            for (int i = 0; i < path.corners.Length - 1; i++)
    //                Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);

    //        moved = true;
    //    }
    //    else
    //    {
    //        arrived = true;
    //        controlledObject.velocity = Vector3.zero;
    //        controlledObject.Motor.BaseVelocity = Vector3.zero;
    //        input.BufferMovement(Vector2.zero);
    //    }

    //    // Look Dir Visualization
    //    //Debug.DrawRay(controlledObject.transform.position, controlledObject.preferredRotation);

    //    return moved;
    //}

    private void AttackTarget()
    {
        if (IsAttackValid(chosenAttack))
        {
            //controlledObject.actionStates[(int)ActionStateEnums.Attack] = chosenAttack.attack;
            attackCooldowns[chosenAttack] = chosenAttack.cooldown;
            chaseTargetPickDelay = 0;
            BufferButton(InputActions.Attack);
        }

        controlledObject.velocity = Vector3.zero;

        if (waitDelay <= 0 && movementType != AIMovementType.None)
        {
            waitDelay = Random.Range(behaviour.ArrivedDelay.x, behaviour.ArrivedDelay.y);
        }
    }

    #region Do on state logic
    /// <summary>
    /// This is a good place to do cooldowns/timers and resets
    /// </summary>
    protected virtual void DoAlways()
    {
        SetInput(InputActions.Move, Vector2.zero);
        trackingTime--;

        if (waitDelay > 0)
            waitDelay--;

        // Pick preferred flank direction
        if (targetChaseOffsetRepickDelay > 0)
            targetChaseOffsetRepickDelay--;
        else
        {
            targetChaseOffsetRepickDelay = 600;
            targetChaseOffsetDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            if (chosenAttack != null)
                chosenDistance = Random.Range(chosenAttack.attackTriggerRange.x, chosenAttack.attackTriggerRange.y);
            else
                chosenDistance = 0;
        }

        // Lower attack cooldowns
        if(behaviour)
            foreach(AIBehaviour.AIAttack attack in behaviour.Attacks)
            {
                if (attackCooldowns[attack] > 0)
                    attackCooldowns[attack]--;
            }
    }
    protected virtual void DoWhenMoveState()
    {
        ChooseTarget();
        ChooseAttack();
        DetermineMoveTarget();
    }
    protected virtual void DoWhenIdleState()
    {
        ChooseTarget();
        ChooseAttack();
        DetermineMoveTarget();
    }
    protected virtual void DoWhenAttackState()
    {
        ChooseTarget();
        ChooseAttack();
        DetermineMoveTarget();
    }
    protected virtual void DoWhenDeadState()
    {
        controlledObject.target = null;
        targetLocation = transform.position;
    }
    protected virtual void DoWhenHurtState()
    {

    }
    #endregion

    #region AI Functionality: Targeting
    /// <summary>
    /// Find the best target
    /// </summary>
    /// <returns></returns>
    public override TangibleObject ChooseTarget()
    {
        if ((controlledObject.stateMachine.actionFrame + Random.Range(0, 60)) % 20 != 0) return controlledObject.target;

        TangibleObject primeTarget = null;
        float closest = Mathf.Infinity;

        // Check if the currentTarget is invalid
        if (currentTarget && !currentTarget.gameObject.activeSelf) currentTarget = null;
        if (currentTarget is ControlledObject &&
                ((behaviour.TargetStateWhitelist.Length > 0 && !behaviour.TargetStateWhitelist.Contains((currentTarget as ControlledObject).stateMachine.CurrentActionState))
                || behaviour.TargetStateBlacklist.Contains((currentTarget as ControlledObject).stateMachine.CurrentActionState)))
            currentTarget = null;

        foreach (TangibleObject obj in TangibleObject.tangibleObjects)
        {
            if (obj.gameObject == gameObject /*|| !(obj is ControlledObject)*/) continue;

            //// Check team if the object is controlled
            if (behaviour.IgnoreAllies && obj is ControlledObject && (obj as ControlledObject).allegiance == controlledObject.allegiance)
                continue;

            // Check if target's current state is on a blacklist/whitelist
            if (obj is ControlledObject &&
                ((behaviour.TargetStateWhitelist.Length > 0 && !behaviour.TargetStateWhitelist.Contains((obj as ControlledObject).stateMachine.CurrentActionState))
                || behaviour.TargetStateBlacklist.Contains((obj as ControlledObject).stateMachine.CurrentActionState)))
                continue;
            
            // Check distance
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < behaviour.TargetingDistanceMinMax.x || dist > behaviour.TargetingDistanceMinMax.y)
                continue;

            // Check sight angle
            if (Vector3.Angle(transform.forward, obj.transform.position - transform.position) > behaviour.SightAngle / 2f)
                continue;

            // Check line of sight
            RaycastHit hit;
            RaycastHit hit2;
            if (!willAlwaysTrackTarget)
            {
                //Check if can see top of player.
                if (Physics.Linecast(transform.TransformPoint(behaviour.EyeLocalOffset),
                obj.transform.position + new Vector3(0, behaviour.EyeLocalOffset.y, 0),
                out hit, behaviour.SightBlockerMask))
                {
                    //print("Line of sight found: " + hit.transform);
                    //If can't, then check if player is crouching and not blocked by an object.
                    Debug.DrawLine(transform.position,
                        obj.transform.position, Color.magenta);
                    if (hit.transform != obj.transform && 
                        Physics.Linecast(transform.position,
                        obj.transform.position,
                        out hit2, behaviour.SightBlockerMask))
                    {
                        if (hit2.transform != obj.transform)
                            continue;
                    }
                }
                else
                {
                    //If can't, then check if player is crouching and not blocked by an object.
                    Debug.DrawLine(transform.position,
                        obj.transform.position, Color.magenta);
                    if (Physics.Linecast(transform.position,
                        obj.transform.position,
                        out hit2, behaviour.SightBlockerMask))
                    {
                        if (hit2.transform != obj.transform)
                            continue;
                    }
                }
            }



            // Important targeting logic is past here
            // Prioritize ControlledObjects, preferred alligence, then whichever is closer
            if (primeTarget == null)
            {
                primeTarget = obj;
                closest = dist;
            }
            else if(obj is ControlledObject)
            {
                if((obj as ControlledObject).allegiance == behaviour.TargetingPriority)
                {
                    if (dist < closest || !(primeTarget is ControlledObject) || (primeTarget as ControlledObject).allegiance != behaviour.TargetingPriority)
                    {
                        primeTarget = obj;
                        closest = dist;
                    }
                }
                else if(!(primeTarget is ControlledObject) || (primeTarget as ControlledObject).allegiance != behaviour.TargetingPriority)
                {
                    if (dist < closest || !(primeTarget is ControlledObject))
                    {
                        primeTarget = obj;
                        closest = dist;
                    }
                }
                    
            }
            else if(!(primeTarget is ControlledObject))
            {
                if(dist < closest)
                {
                    primeTarget = obj;
                    closest = dist;
                }
            }
        }

        //no target seen decrease seen rate
        if (primeTarget == null)
        {
            currentDetection = Mathf.Max(currentDetection - behaviour.DetectionRate, 0);
        }
        // target seen increase seen amount
        if (primeTarget != null && currentTarget == null && currentDetection < detectionThreshold)
        {
            currentDetection += behaviour.DetectionRate * primeTarget.detectedMultiplier;
            primeTarget = null;
        }
        //Change Light color based on seen amount
        if(sightLight != null)
        {
            if (currentTarget != null)
                sightLight.color = detectedColor;
            else
                sightLight.color = Color.Lerp(undetectedColor, detectedColor, currentDetection / detectionThreshold);
        }



        // If the target is lost, continue tracking it for a time
        if (primeTarget == null && currentTarget != null && trackingTime < 0)
        {
            trackingTime = Random.Range(behaviour.ContinueTrackingFrameDurationRange.x, behaviour.ContinueTrackingFrameDurationRange.y);
        }

        if (trackingTime > 0)
        {
            controlledObject.target = currentTarget;
        }
        else
        {
            SetTarget(primeTarget);
        }

        return primeTarget;
    }

    public void SetTarget(TangibleObject t)
    {
        if (targetSpotted && currentTarget == null && t != controlledObject.target)
        {
            Debug.Log($"{controlledObject}, {t}");
            BarkManager.Instance!.PlayBark(targetSpotted, controlledObject.barkTransform, Vector3.zero);
        }

        controlledObject.target = currentTarget = t;

        //if(behaviour && t != null)
        //    foreach(Collider c in Physics.OverlapSphere(controlledObject.transform.position, behaviour.TargetShareRange))
        //    {
        //        AIController AI = c.GetComponent<AIController>();
        //        if (AI && AI.controlledObject.allegiance == controlledObject.allegiance && AI.controlledObject.target == null)
        //        {
        //            StartCoroutine(Delay());

        //            IEnumerator Delay()
        //            {
        //                int range = Random.Range(0, 60);
        //                int count = 0;
        //                while (count < range)
        //                {
        //                    yield return new WaitForFixedUpdate();
        //                    count++;
        //                }
        //                Debug.Log($"{gameObject} shared target {t} to {AI.gameObject}");
        //                AI.SetTarget(t);
        //            }
        //        }
        //    }

        // When the target changes, pick a new way to move
        DetermineMoveType();
    }

    /// <summary>
    /// Choose which attack the AI will attempt to get into position and do next
    /// </summary>
    protected virtual void ChooseAttack()
    {
        if (chaseTargetPickDelay > 0)
        {
            chaseTargetPickDelay--;
        }
        else
        {
            chaseTargetPickDelay = Random.Range(behaviour.MovementPositionPickDelay.x, behaviour.MovementPositionPickDelay.y);

            // Default to no attack
            AIBehaviour.AIAttack newlyChosenAttack = null;

            if (controlledObject.target && controlledObject.target.canBeAttacked && (!(controlledObject.target is ControlledObject) || (controlledObject.target as ControlledObject).allegiance != controlledObject.allegiance))
            {
                Vector3 dir = transform.position - controlledObject.target.transform.position;
                // Find closest attack from random attack range
                float closest = Mathf.Infinity;
                foreach (AIBehaviour.AIAttack attack in behaviour.Attacks)
                {
                    // Check if the attack is on cooldown
                    if (attackCooldowns[attack] > 0)
                        continue;

                    // Check if target's current state is on a blacklist/whitelist
                    if (controlledObject.target is ControlledObject &&
                        ((attack.targetStateWhitelist.Length > 0 && !attack.targetStateWhitelist.Contains((controlledObject.target as ControlledObject).stateMachine.CurrentActionState))
                        || attack.targetStateBlacklist.Contains((controlledObject.target as ControlledObject).stateMachine.CurrentActionState)))
                        continue;

                    float dist = Random.Range(attack.attackTriggerRange.x, attack.attackTriggerRange.y);
                    if (Mathf.Abs(dir.magnitude - dist) < closest)
                    {
                        closest = dist;
                        newlyChosenAttack = attack;
                    }
                }
            }

            // On picking a new attack
            if(newlyChosenAttack != chosenAttack)
            {
                chosenAttack = newlyChosenAttack;
                targetChaseOffsetRepickDelay = 0;
            }

            //DetermineMoveType();
        }
    }

    protected bool IsAttackValid(AIBehaviour.AIAttack attack)
    {
        if (attack == null || controlledObject.target == null)
            return false;

        //Debug.Log(controlledObject.target);
        // Check if target's current state is on a blacklist/whitelist
        if (controlledObject.target is ControlledObject &&
            ((attack.targetStateWhitelist != null && attack.targetStateWhitelist.Length > 0 && !attack.targetStateWhitelist.Contains((controlledObject.target as ControlledObject).stateMachine.CurrentActionState))
            || (attack.targetStateBlacklist != null && attack.targetStateBlacklist.Contains((controlledObject.target as ControlledObject).stateMachine.CurrentActionState))))
            return false;

        // Check if target is in range
        float dist = Vector3.Distance(controlledObject.transform.position, controlledObject.target.transform.position);
        if (dist < attack.attackTriggerRange.x || dist > attack.attackTriggerRange.y)
            return false;

        return true;
    }
    #endregion
    #region AI Functionality: Movement
    protected virtual void DetermineMoveType()
    {
        movementType = AIMovementType.None;
        if (waitDelay > 0) return;

        ObjectType type = ObjectType.None;
        if (controlledObject.target)
        {
            if (controlledObject.target is ControlledObject)
                type |= ObjectType.Controlled;
            else if (controlledObject.target is PhysicsObject)
                type |= ObjectType.Physical;
            else 
                type |= ObjectType.Tangible;
        }
        if (type == ObjectType.None)
            type = ObjectType.NoTarget;

        List<AIBehaviour.AIMovementOption> options = new List<AIBehaviour.AIMovementOption>();
        int weight = 0;
        foreach(AIBehaviour.AIMovementOption movementOption in behaviour.MovementBehaviour)
        {
            if (movementOption.target.HasFlag(type) 
                && chosenAttack != null == movementOption.canAttack 
                && (!(controlledObject.target is ControlledObject) || ((controlledObject.target as ControlledObject).allegiance == movementOption.allegiance)))
            {
                options.Add(movementOption);
                weight += movementOption.frequency;
            }
        }
        if (waitDelay <= 0)
            movementWeightPick = Random.Range(0, weight);
        weight = 0;
        foreach(AIBehaviour.AIMovementOption option in options)
        {
            weight += option.frequency;
            if (movementWeightPick <= weight)
            {
                movementType = option.movementType;
                leanIntoMovement = option.leanIntoMovement;
                break;
            }
        }
    }

    private void DetermineMoveTarget()
    {
        if (moveTargetPickDelay > 0)
        {
            moveTargetPickDelay--;
        }
        else
        {
            moveTargetPickDelay = Random.Range(behaviour.MovementPositionPickDelay.x, behaviour.MovementPositionPickDelay.y);
            // Pick target location
            switch (movementType)
            {
                case AIMovementType.Chase:
                    targetLocation = ChaseTarget();
                    break;
                case AIMovementType.Wander:
                    targetLocation = WanderTarget();
                    break;
                case AIMovementType.Path:
                    targetLocation = PathTarget();
                    break;
                case AIMovementType.Return:
                    targetLocation = ReturnTarget();
                    break;
                case AIMovementType.Follow:
                    targetLocation = FollowTarget();
                    break;
                case AIMovementType.Approach:
                    targetLocation = AppraochTarget();
                    break;
                case AIMovementType.Retreat:
                    targetLocation = RetreatTarget();
                    break;
                case AIMovementType.Rotate:
                    targetLocation = RotateTarget();
                    break;
                case AIMovementType.None:
                default:
                    if (!manualMode)
                        targetLocation = transform.position;
                    break;
            }
            moveableTransform.position = targetLocation;
        }
    }

    /**
     * Needed context references:
     * Target chase offset direction
     * Chosen distance
     * Home transform
    **/ 
    protected virtual Vector3 ChaseTarget()
    {
        controlledObject.MovementSpeedMultiplier = defaultMoveSpeed;
        if (!controlledObject.target) return transform.position;
        return controlledObject.target.transform.position + controlledObject.target.transform.rotation * targetChaseOffsetDir * chosenDistance;
    }

    protected virtual Vector3 WanderTarget()
    {
        if (moveableTransform)
        {
            if (!moveableTransform.gameObject.activeSelf)
            {
                //InitializeWanderingTarget();
            }

            return moveableTransform.position;
        }
        else
        {
            return targetLocation;
        }
    }

    protected virtual Vector3 PathTarget()
    {
        controlledObject.MovementSpeedMultiplier = behaviour.PathMoveSpeed;
        if (!waitingAtPoint && Vector3.Distance(transform.position, currentWaypoint.transform.position) < behaviour.PathPointDistanceThreshold)
        {
            waitingAtPoint = true;
            StartCoroutine(WaitAtPoint());
        }
        return currentWaypoint.transform.position;

        IEnumerator WaitAtPoint()
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
            yield return new WaitForSeconds(Random.Range(behaviour.PointReachedWaitTime.x, behaviour.PointReachedWaitTime.y));
            int nextPoint = Random.Range(0, currentWaypoint.accessableWaypoints.Length);
            AIPathWaypoint tempWaypoint = currentWaypoint.accessableWaypoints[nextPoint];
            if (tempWaypoint == lastWaypoint && (currentWaypoint.accessableWaypoints.Length == 1 || Random.Range(0, 1.0f) > behaviour.ChanceToTurnAround))
            {
                nextPoint = Random.Range(0, currentWaypoint.accessableWaypoints.Length);
                tempWaypoint = currentWaypoint.accessableWaypoints[nextPoint];
            }
            lastWaypoint = currentWaypoint;
            currentWaypoint = tempWaypoint;
            waitingAtPoint = false;
        }
    }

    protected virtual Vector3 ReturnTarget()
    {
        controlledObject.MovementSpeedMultiplier = defaultMoveSpeed;
        return home == null ? transform.position : home;
    }

    protected virtual Vector3 RotateTarget()
    {
        controlledObject.MovementSpeedMultiplier = 0;
        currentRotateTime -= Time.deltaTime;
        if (currentRotateTime <= 0 && !waitingWhileRotating)
        {
            waitingWhileRotating = true;
            StartCoroutine(WaitWhileTurning());
        }
        return currentRotateTransform == null ? transform.position : currentRotateTransform.position;

        IEnumerator WaitWhileTurning()
        {
            controlledObject.stateMachine.SetLocomotionState(Locomotion.Idle);
            Transform nextTransform = currentRotateTransform == rightTransform ? leftTransform : rightTransform;
            currentRotateTransform = null;
            yield return new WaitForSeconds(1);
            currentRotateTransform = nextTransform;
            currentRotateTime = behaviour.RotateTime;
            waitingWhileRotating = false;
        }
    }

    protected virtual Vector3 FollowTarget()
    {
        if (!currentTarget) 
            return transform.position;
        return currentTarget.transform.position + (controlledObject.transform.position - currentTarget.transform.position).normalized * behaviour.MinMaxFollowRange.x;
    }

    protected virtual Vector3 AppraochTarget()
    {
        if (!currentTarget) 
            return transform.position;
        return currentTarget.transform.position + (controlledObject.transform.position - currentTarget.transform.position).normalized * behaviour.ApproachDistance;
    }

    protected virtual Vector3 RetreatTarget()
    {
        if(!currentTarget) 
            return transform.position;
        return currentTarget.transform.position + (controlledObject.transform.position - currentTarget.transform.position).normalized * behaviour.RetreatDistance;
    }

    protected virtual void InitializeWanderingTarget()
    {
        moveableTransform.DOKill();
        moveableTransform.gameObject.SetActive(true);
        List<Vector3> walkPath = new List<Vector3>();
        Vector3 currentRandomPoint = transform.position;
        float totalDistance = 0;
        Vector3 prevDir = Vector3.zero;
        for (int i = 0; i < (int)(behaviour.WanderPathLength); i++)
        {
            Vector3 potential = new Vector3(Random.Range(-behaviour.WanderMaximumStraightPathLength, behaviour.WanderMaximumStraightPathLength),
                0, Random.Range(-behaviour.WanderMaximumStraightPathLength, behaviour.WanderMaximumStraightPathLength)) + currentRandomPoint;

            //float angle = Vector3.Angle(potential - currentRandomPoint, prevDir);
            NavMeshHit hit;
            if (Vector3.Distance(potential, wanderZoneCenter.position) <= wanderZoneRadius && NavMesh.SamplePosition(potential, out hit, 2f, 1))// && angle < 20)
            {
                //Debug.Log(angle);
                totalDistance += Vector3.Distance(currentRandomPoint, potential);
                currentRandomPoint = potential;
                walkPath.Add(potential);
            }

            //prevDir = potential - currentRandomPoint;
        }
        if(walkPath.Count > 0)
            moveableTransform.DOPath(walkPath.ToArray(), totalDistance / controlledObject.MoveSpeed / behaviour.WanderSpeedMultiplier).SetEase(Ease.Linear).OnComplete(
                () => {
                    movementType = AIMovementType.Return;
                    moveableTransform.gameObject.SetActive(false);
                });
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }
    #endregion

    #region Visualization
    private void OnDrawGizmosSelected()
    {
        if (behaviour == null)  return;
        // Draw vision
        Gizmos.color = Color.gray;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + behaviour.EyeLocalOffset, (behaviour.SightAngle >= 180 ? Quaternion.AngleAxis(180, Vector3.up) * transform.rotation : transform.rotation), Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, behaviour.SightAngle, behaviour.TargetingDistanceMinMax.y * ((180f - (behaviour.SightAngle >= 180 ? (360 - behaviour.SightAngle) : behaviour.SightAngle)) / 180f), behaviour.TargetingDistanceMinMax.x, 1);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + behaviour.EyeLocalOffset, behaviour.TargetingDistanceMinMax.y);
        Gizmos.DrawRay(transform.position + behaviour.EyeLocalOffset, transform.forward * behaviour.TargetingDistanceMinMax.y);

        // Draw home and wander zone
        Gizmos.color = Color.green;
        if (wanderZoneCenter != null)
            Gizmos.DrawWireSphere(wanderZoneCenter.position, wanderZoneRadius);
        if (Application.isPlaying)
        {
            Gizmos.DrawWireSphere(home, behaviour.MinMaxDistFromHome.x);
            Gizmos.DrawWireSphere(home, behaviour.MinMaxDistFromHome.y);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, behaviour.MinMaxDistFromHome.x);
            Gizmos.DrawWireSphere(transform.position, behaviour.MinMaxDistFromHome.y);
        }
    }


    //private void OnDrawGizmos()
    //{
    //    if (behaviour == null) return;

    //    // Draw attack
    //    if (behaviour.Attacks == null || behaviour.Attacks.Length <= attackDrawn || behaviour.Attacks[attackDrawn] == null) return;
    //    AttackState attack = behaviour.Attacks[attackDrawn].attack;
    //    if (attack == null) return;
    //    foreach (Hitbox hitbox in attack.hitboxes)
    //    {
    //        if (framesDrawn.x <= hitbox.activeFrames.y && framesDrawn.y >= hitbox.activeFrames.x)
    //        {
    //            Vector3 center = transform.rotation * Quaternion.Euler(hitbox.rotation) * hitbox.position + transform.position;
    //            Gizmos.color = Color.white;
    //            Gizmos.DrawWireSphere(center, hitbox.radius);
    //            Gizmos.color = new Color(1, 0, 0, .5f);
    //            Gizmos.DrawSphere(center, hitbox.radius);
    //        }
    //    }

    //    // Draw animation
    //    if (Application.isPlaying) return;
    //    controlledObject.anim.speed = 0f;
    //    animFrameDrawn = (int)((framesDrawn.x + framesDrawn.y) / 2f);
    //    controlledObject.anim.Play(attack.animationStateName, 0, animFrameDrawn * Time.fixedDeltaTime);
    //    controlledObject.anim.Update(Time.fixedDeltaTime);
    //}
    #endregion
}
