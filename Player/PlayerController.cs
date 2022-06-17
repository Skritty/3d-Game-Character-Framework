using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

public class PlayerController : BaseObjectController
{
	[SerializeField]
	private SoundEffect takeHitSound;
	[SerializeField]
	private float bloodSplatterFadeSpeed = 1;
	private float bloodSplatterInitialDelay = .5f;
	[SerializeField] 
	private Image bloodSplatter;

	[Sirenix.OdinInspector.ReadOnly]
	public List<Equipment> weapons = new List<Equipment>();

	[Header("Player-Specific Equipment")]
	[SerializeField]
	private ActionState weaponSwapState;

	private void Awake()
    {
		// Fix for camera issues (also in OnEnable/Disable
		controlledObject.cameraEquipmentRoot = new GameObject("Player Camera Equipment").transform;
		controlledObject.cameraEquipmentRoot.parent = Camera.main.transform;
		controlledObject.cameraEquipmentRoot.position = Camera.main.transform.position;
		controlledObject.cameraEquipmentRoot.rotation = Camera.main.transform.rotation;
	}

    private void Start()
	{
		CreateEquipment();

		Equipment startingWeapon = controlledObject.GetEquipment(InputActions.Attack);
		if (startingWeapon)
			AddWeaponToInventory(startingWeapon);

		controlledObject.OnHit.AddListener(OnHit);

		// Tell camera to follow transform
		//orbitCamera.SetFollowTransform(cameraFollowPoint);

		// Ignore the character's collider(s) for camera obstruction checks
		//orbitCamera.IgnoredColliders.Clear();
		//orbitCamera.IgnoredColliders.AddRange(GetComponentsInChildren<Collider>());
	}

	private void CreateEquipment()
    {
		Equipment swap = Equipment.CreateEquipment<Equipment>(weaponSwapState,
			ActivationCondition: () => GetInput<float>(InputActions.Scroll) != 0 && weapons.Count >= 2
		);
		controlledObject.Equip(swap, transform, InputActions.Scroll);
	}

    private void OnEnable()
    {
		controlledObject?.cameraEquipmentRoot?.gameObject.SetActive(true);
	}

    private void OnDisable()
    {
		controlledObject?.cameraEquipmentRoot?.gameObject.SetActive(false);
	}

    private void LateUpdate()
	{
		//HandleCameraInput();
	}

	private void HandleCameraInput()
	{
		// Create the look input vector for the camera
		Vector2 mouseMovement = GetInput<Vector2>(InputActions.Mouse);
		Vector3 lookInputVector = new Vector3(mouseMovement.x, mouseMovement.y, 0f);

		// Prevent moving the camera while the cursor isn't locked
		if (Cursor.lockState != CursorLockMode.Locked)
		{
			lookInputVector = Vector3.zero;
		}

		//// Input for zooming the camera (disabled in WebGL because it can cause problems)
		//float scrollInput = -Input.GetAxis(MouseScrollInput);
		////#if UNITY_WEBGL
		//scrollInput = 0f;
		////#endif

		// Apply inputs to the camera
		//orbitCamera.UpdateWithInput(Time.deltaTime, 0f, lookInputVector);

		// Handle toggling zoom level
		//if (Input.GetMouseButtonDown(1))
		//{
		//    OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
		//}
	}

	private void OnHit(DamageInstance damage)
    {
		if (GetComponent<TangibleObject>().bigHurt)
		{
			GetComponent<TangibleObject>().bigHurt = false;
			//orbitCamera.ImpulseSource.GenerateImpulse();
		}
		CreateBloodSplatter();
		AudioManager.Instance.PlaySFX(takeHitSound, transform.position);
	}

	private void CreateBloodSplatter()
	{
		bool isShowingBloodSplatter = false;

		if (bloodSplatter && !isShowingBloodSplatter)
		{
			isShowingBloodSplatter = true;
			bloodSplatter.gameObject.SetActive(isShowingBloodSplatter);
			bloodSplatter.DOFade(1, 0).OnComplete(() =>
			{
				if (GetComponent<TangibleObject>().currentHealth > 0)
					StartCoroutine(OnDelayExecution(() =>
					{
						bloodSplatter.DOFade(0, bloodSplatterFadeSpeed).OnComplete(() => {
							isShowingBloodSplatter = false;
							bloodSplatter.gameObject.SetActive(isShowingBloodSplatter);
						});
					}, bloodSplatterInitialDelay));
			});
		}
	}
	IEnumerator OnDelayExecution(UnityAction callback, float timeElapsed)
	{
		yield return new WaitForSeconds(timeElapsed);
		callback?.Invoke();
	}

	public override TangibleObject ChooseTarget()
	{
		//TODO: Implement player targeting
		return null;
	}

	#region InputBuffering
	private bool fireHeld;
    private void Update()
    {
		if (fireHeld && GetInput<bool>(InputActions.Attack))
			BufferButton(InputActions.Attack);
    }
    public void Mouse(InputAction.CallbackContext ctx) => SetInput(InputActions.Mouse, ctx.ReadValue<Vector2>());
	public void Move(InputAction.CallbackContext ctx) => SetInput(InputActions.Move, ctx.ReadValue<Vector2>());
	public void WeaponSwap(InputAction.CallbackContext ctx)
    {
		if (ctx.performed)
        {
			SetInput(InputActions.Scroll, ctx.ReadValue<float>());
			if(ctx.ReadValue<float>() != 0)
				BufferButton(InputActions.Scroll);
		}
	}
	public void FirePress(InputAction.CallbackContext ctx)
    {
		if (ctx.performed)
        {
			fireHeld = true;
			BufferButton(InputActions.Attack);
		}
	}
	public void FireRelease(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
        {
			fireHeld = false;
		}
	}
	public void Melee(InputAction.CallbackContext ctx)
    {
		if (ctx.performed) BufferButton(InputActions.Melee);
	}
	public void Jump(InputAction.CallbackContext ctx)
    {
		if (ctx.performed) BufferButton(InputActions.Jump);
	}
	public void Sprint(InputAction.CallbackContext ctx)
    {
		if (ctx.performed) BufferButton(InputActions.Sprint);
	}
	public void Crouch(InputAction.CallbackContext ctx)
	{
		if (ctx.performed) BufferButton(InputActions.Crouch);
	}
	public void Reload(InputAction.CallbackContext ctx)
    {
		if (ctx.performed) BufferButton(InputActions.Reload);
	}
	public void Menu(InputAction.CallbackContext ctx)
    {
		if (ctx.performed) BufferButton(InputActions.Menu);
	}
	public void PlayerPause(InputAction.CallbackContext ctx)
	{
		MenuManager.Instance.TogglePauseMenu();
	}
	#endregion

	#region Inventory
	public void AddWeaponToInventory(Equipment weapon)
	{
		foreach (Equipment e in weapons)
			e.gameObject.SetActive(false);
		weapons.Add(weapon);
	}
	#endregion
}

