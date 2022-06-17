// Written by: Trevor Thacker
using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerManager : Singleton<PlayerManager>
{
    public PlayerController player => playerObject.GetComponent<PlayerController>();
    public GameObject playerObject;
    public Camera mainCamera;
    [SerializeField]
    private Cinemachine.CinemachineInputProvider cameraInput;

    [SerializeField]
    private ActionStateProgressDictionary stateProgressDictionary;
    [SerializeField]
    private CheckpointProgressDictionary respawnProgressDictionary;

    private void Respawn(DamageInstance damage)
    {
        TransitionManager.StartTransition("respawnFade");
        TransitionManager.OnTransitionMidpoint += DoRespawn;

        void DoRespawn(string transition)
        {
            if (transition != "respawnFade") return;
            TransitionManager.OnTransitionMidpoint -= DoRespawn;

            // Teleport the player halfway through fading
            player.controlledObject.Motor.SetPositionAndRotation(
                (respawnProgressDictionary.currentProgressTracker as CheckpointProgressTracker).Position, 
                (respawnProgressDictionary.currentProgressTracker as CheckpointProgressTracker).Rotation);
            //player.controlledObject.stateMachine.PlayStateAnim(player.controlledObject, nextState, 0);
            player.controlledObject.stateMachine.SetActionState(stateProgressDictionary.CurrentState);

            //Reset game TODO (probably put in a game manager)
            player.controlledObject.ResetHealth();
        }
    }

    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        player.controlledObject.Motor.SetPositionAndRotation(position, rotation);
    }

    public void TeleportPlayer(Transform t)
    {
        player.controlledObject.Motor.SetPositionAndRotation(t.position, t.rotation);
    }

    public void SetPlayerInput(bool enabled)
    {
        PlayerManager.Instance.playerObject.GetComponent<PlayerInput>().enabled = enabled;
        cameraInput.enabled = enabled;
    }
}
