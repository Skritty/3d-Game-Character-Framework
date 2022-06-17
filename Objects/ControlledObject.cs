using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cinemachine;

[RequireComponent(typeof(KinematicCharacterMotor), typeof(StateMachine))]
public class ControlledObject : TangibleObject, ICharacterController
{
	public KinematicCharacterMotor Motor => GetComponent<KinematicCharacterMotor>();
	public BaseObjectController controller => GetComponent<BaseObjectController>();
	//  {
	//      get
	//{
	//	foreach (BaseObjectController c in GetComponents<BaseObjectController>())
	//		if (c.enabled)
	//              {
	//			return c;
	//		}

	//	return null;
	//}
	//  }
	public StateMachine stateMachine => GetComponent<StateMachine>();

    #region Animator
    [SerializeField]
	private Animator mainAnimator;
	[Sirenix.OdinInspector.ShowInInspector]
	private Dictionary<string, Animator> _animators = new Dictionary<string, Animator>();
	[Sirenix.OdinInspector.ReadOnly]
	public Dictionary<string, Animator> Animators
    {
        get
        {
			if (!_animators.ContainsKey("main"))
				_animators.Add("main", mainAnimator);

			_animators = _animators.Where(f => f.Value != null).ToDictionary(x => x.Key, x => x.Value);

			return _animators;
        }
    }

	public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
    {
		foreach (KeyValuePair<string, Animator> animator in Animators)
		{
			animator.Value.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset);
		}
	}

	public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset, float normalizedTransitionTime)
	{
		foreach (KeyValuePair<string, Animator> animator in Animators)
		{
			animator.Value.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
		}
	}
    #endregion

    public Vector3 preferredRotation;
	public Transform MeshRoot;
	public Vector3 MeshRootOffset;
	public Transform head;
	public Transform cameraEquipmentRoot;
	public Transform weaponHandle;
	public TrailRenderer weaponTrail;

	[Header("Data")]
	public SerializedDictionary<Locomotion, LocomotionState> locomotionStates = new SerializedDictionary<Locomotion, LocomotionState>();
	[SerializeField]
	private ActionState hurtState;
	[SerializeField]
	private ActionState deadState;
	public ControlledObjectAllegiance allegiance;

	#region Equipment
	[SerializeField]
	private SerializedDictionary<InputActions, Equipment> startingEquipment = new SerializedDictionary<InputActions, Equipment>();
	[Sirenix.OdinInspector.ShowInInspector, Sirenix.OdinInspector.ReadOnly]
	private SerializedDictionary<InputActions, Equipment> equipment = new SerializedDictionary<InputActions, Equipment>();

	public Equipment GetEquipment(InputActions action)
    {
		if (!equipment.ContainsKey(action))
			equipment.Add(action, null);

		return equipment[action];
	}

	public Equipment Equip(Equipment equip, Transform parent, InputActions action, bool createNew = false, bool destroyOld = false)
	{
		if (!equipment.ContainsKey(action))
			equipment.Add(action, null);

        if (destroyOld)
        {
			Equipment exisiting = GetEquipment(action);
			if (exisiting != null)
				Destroy(exisiting.gameObject);
		}

        if (createNew)
        {
			equip = Instantiate(equip, parent);
        }

		if (equip.animator)
        {
			if (!Animators.ContainsKey(action.ToString()))
			{
				Animators.Add(action.ToString(), equip.animator);
			}
			else
			{
				Animators[action.ToString()] = equip.animator;
			}
		}
        else if(Animators.ContainsKey(action.ToString()))
        {
			Animators.Remove(action.ToString());
		}

		equip.transform.parent = parent;
		equip.transform.localPosition = Vector3.zero;
		equip.transform.localRotation = Quaternion.identity;
		equipment[action] = equip;
		equip.OnEquip(this, action);
		return equip;
	}

	public void UseEquipment(InputActions action)
    {
		if (cameraEquipmentRoot && !cameraEquipmentRoot.gameObject.activeSelf) return;

		if (!equipment.ContainsKey(action))
			equipment.Add(action, null);

		if (equipment[action] != null)
			equipment[action].Activate(this, action);
	}
    #endregion

    public Transform respawnPoint;
	public TangibleObject target;
	public bool noKnockback;

	//should make motion class to clean up stuff
	[Header("Stable Movement")]
	public Vector3 velocity;
	public bool reorientToCamera;
	[Tooltip("X: normalized degrees from forward movement (0) to backwards movement (1)")]
	[SerializeField]
	private AnimationCurve MovespeedCurve;
	[SerializeField]
	public float MovementSpeedMultiplier;
	[SerializeField]
	private float AnimationSpeedMultiplier = 1;
	public float MoveSpeed {
		get 
		{
			float angle = Vector3.Angle(new Vector3(transform.forward.x, transform.forward.z), controller.GetInput<Vector2>(InputActions.Move));
			return MovespeedCurve.Evaluate(Mathf.Clamp01(angle / 180f)) * MovementSpeedMultiplier;
		} 
	}
	public float AnimationSpeed
    {
		get
		{
			float angle = Vector3.Angle(new Vector3(transform.forward.x, transform.forward.z), controller.GetInput<Vector2>(InputActions.Move));
			return MovespeedCurve.Evaluate(Mathf.Clamp01(angle / 180f)) * AnimationSpeedMultiplier;
		}
	}
	public float Friction = 15;
	public float OrientationSharpness = 10;

	[Header("Air Movement")]
	public float MaxAirMoveSpeed = 10f;
	public float AirAccelerationSpeed = 5f;
	public float Drag = 0.1f;

	[Header("Misc")]
	public Transform barkTransform;
	public bool RotationObstruction;
	public Vector3 Gravity = new Vector3(0, -30f, 0);
	private float lastX = 0;
	[HideInInspector]
	public bool jumpRequested;
	public SoundEffect hurtSFX;

	public void Awake()
	{
		//transform.parent = null; //STOPS LOSSYSCALE BUG yell at Christian if this trips you up for some reason

		foreach (KeyValuePair<InputActions, Equipment> e in startingEquipment)
			Equip(e.Value, cameraEquipmentRoot, e.Key, true);

		Motor.CharacterController = this;
	}

	public override void TakeHit(DamageInstance damage)
	{
		switch (tangibility)
		{
			case ObjectTangibility.Invincible:
				//if (currentHealth <= 0)
				//{
				//	Die(damage);
				//}
				break;
			case ObjectTangibility.Armor:
				if (damage.damage >= 0 && damage.allegiance == allegiance) break;
				currentHealth -= damage.damage;
				OnHit.Invoke(damage);
				if (currentHealth <= 0)
				{
					Die(damage);
				}
				else if (damage.armorPierce && damage.damage >= 0)
				{
					stateMachine.SetActionState(hurtState);
				}
				if (GetComponent<CinemachineImpulseSource>())
					GetComponent<CinemachineImpulseSource>().GenerateImpulse(damage.screenShake);
				//if (hurtSFX)
				//	AudioManager.Instance.PlaySFX(hurtSFX, transform.position);
				break;
			case ObjectTangibility.Normal:
				if (damage.damage >= 0 && damage.allegiance == allegiance) break;
				currentHealth -= damage.damage;
				OnHit.Invoke(damage);
				if (currentHealth <= 0)
				{
					Die(damage);
				}
				else if (damage.damage >= 0 && !noKnockback)
				{
					stateMachine.SetActionState(hurtState);
					//if (damage.origin == null)
					//	damage.origin = transform.position + transform.forward;
					DoKnockback(damage);
				}
				if (GetComponent<CinemachineImpulseSource>())
					GetComponent<CinemachineImpulseSource>().GenerateImpulse(damage.screenShake);
				if(hurtSFX)
					AudioManager.Instance.PlaySFX(hurtSFX, transform.position);
				break;
		}
	}

	protected virtual void DoKnockback(DamageInstance damage)
	{
		Vector3 kbVel = Vector3.zero;
		kbVel = (GetPlanarRotation(Quaternion.identity) * (transform.position - damage.impactOrigin)).normalized * damage.knockback;
		velocity = new Vector3(kbVel.x, 0f, kbVel.z);
	}

	public override void Die(DamageInstance damage)
    {
		base.Die(damage);
		GameManager.Instance.killCount++;
		stateMachine.SetActionState(deadState);
		if (GetComponent<UnityEngine.AI.NavMeshObstacle>())
			GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled = false;
	}

    public override void Reset()
    {
		base.Reset();
		foreach (Hurtbox h in GetComponentsInChildren<Hurtbox>())
        {
			h.ResetHurtbox();
		}
			
		stateMachine.SetActionState(null);
		stateMachine.SetLocomotionState(Locomotion.Idle);
    }

    #region Kinematic Callbacks
    public void AfterCharacterUpdate(float deltaTime)
	{

	}

	public void BeforeCharacterUpdate(float deltaTime)
	{

	}

	public bool IsColliderValidForCollisions(Collider coll)
	{
		return true;
	}

	public void OnDiscreteCollisionDetected(Collider hitCollider)
	{

	}

	public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{

	}

	public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{

	}

	public void PostGroundingUpdate(float deltaTime)
	{

	}

	public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
	{

	}

	public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		if (OrientationSharpness > 0f)
		{
			// Smoothly interpolate from current to target look direction
			Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, preferredRotation, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
			currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);

			lastX = transform.eulerAngles.y;
		}
	}

	public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		Vector3 targetMovementVelocity = Vector3.zero;
		if (Motor.GroundingStatus.IsStableOnGround)
		{
			// Reorient source velocity on current ground slope (this is because we don't want our smoothing to cause any velocity losses in slope changes)
			currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
			
			// Calculate target velocity
			Vector3 inputRight = Vector3.Cross(velocity, Motor.CharacterUp);
			Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * velocity.magnitude;
            targetMovementVelocity = reorientedInput;
			
			// Smooth movement Velocity
			currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-Friction * deltaTime));
			
		}
		else
		{
			// Add move input
			if (velocity.sqrMagnitude > 0f)
			{
				targetMovementVelocity = Vector3.ClampMagnitude(velocity, MaxAirMoveSpeed);

				// Prevent climbing on un-stable slopes with air movement
				if (Motor.GroundingStatus.FoundAnyGround)
				{
					Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
					targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
				}

				Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
				currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
			}

			// Gravity
			currentVelocity += Gravity * deltaTime;

			// Drag
			currentVelocity *= (1f / (1f + (Drag * deltaTime)));
		}
		
		Vector3 localVelocity = transform.TransformDirection(currentVelocity);
	}
    #endregion
}