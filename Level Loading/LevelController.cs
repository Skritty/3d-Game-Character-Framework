// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.Linq;
using Cinemachine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// A container class used to ensure proper additive loading and unloading of scenes.
/// </summary>
public class LevelController : MonoBehaviour
{
    [InfoBox("This script must be filled out for playtesting to work!", InfoMessageType.Warning, VisibleIf = "@levelData == null || initialSceneStates.Count == 0")]
    public LevelLoadData levelData;

    [System.Serializable]
    public class InitialSceneState
    {
        public LevelLoadData fromScene;

        public bool setTime;
        [ShowIf("@setTime"), Range(0, 24)] 
        public float time;

        public bool teleportPlayer;
        [ShowIf("@teleportPlayer")]
        public Transform playerTeleportTransform;

        public bool useInitialPlayerState;
        [ShowIf("@useInitialPlayerState")]
        public ActionState initialPlayerState;

        public bool useInitialTimeline;
        [ShowIf("@useInitialTimeline")]
        public PlayableDirector director;

        public bool useSetPlayerEquip;
        [ShowIf("@useSetPlayerEquip")]
        public AttackState attackState;

        public bool useInitialCamera;
        [ShowIf("@useInitialCamera")]
        [Tooltip("This VCam will be set to priority 10 if it is the primary scene")]
        public CinemachineVirtualCamera initialCamera;

        //public bool useTerrain;
        //[ShowIf("@useTerrain")]
        //public Terrain map;

        public UnityEvent OnLoad;
    }

    [Header("Scene initial state")]
    [SerializeField]
    private CinemachineBlenderSettings cameraBlends;
    [SerializeField]
    private bool usePlayerCharacter = true;
    [SerializeField]
    private bool useBackgroundColor = false;
    [SerializeField, ShowIf("@useBackgroundColor")]
    private Color backgroundColor = Color.black;
    [InfoBox("Make one Initial Scene State for each scene you expect to be entering from! Add one with no level load data to playtest the scene.")]
    [SerializeField]
    private List<InitialSceneState> initialSceneStates = new List<InitialSceneState>();

    [Space]
    public UnityEvent SceneLoading;
    public UnityEvent SceneUnloading;
    [Space]

    [Header("Scene respawn")]
    public ActionState respawnState;
    public int timesRespawned;

    [Header("Debug")]
    public Transform[] debugRespawnPoints;

    private void Awake()
    {
        if (!LevelManager.initialLoadDone)
        {
            LevelManager.initialLoadDone = true;
            SceneManager.LoadSceneAsync(0).completed += (x) =>
            {
                LevelManager.Instance.LoadLevel(levelData);
                Initialize();
            };
        }
        else
        {
            Initialize(); 
        }
    }

    /// <summary>
    /// Initialize things once the scene is loaded. This happens for all scenes with a level controller.
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"Initializing {levelData.name}");

        // Set all VCams to priority 0
        foreach (CinemachineVirtualCamera vcam in GameObject.FindObjectsOfType<CinemachineVirtualCamera>())
            if(vcam.Priority != 1234)
                vcam.Priority = 0;

        // Populate every timeline with the required references
        foreach (CinemachineBrain brain in GameObject.FindObjectsOfType<CinemachineBrain>())
            if (PersistantPlayerCamera.Brain != brain)
                brain.gameObject.SetActive(false);

        foreach (PlayableDirector director in GameObject.FindObjectsOfType<PlayableDirector>(true))
        {
            TimelineAsset timeline = director.playableAsset as TimelineAsset;
            if (!timeline) return;

            for (int i = 0; i < timeline.outputTrackCount; i++)
            {
                TrackAsset track = timeline.GetOutputTrack(i);
                CinemachineTrack t = track as CinemachineTrack;
                
                if (t)
                    director.SetGenericBinding(track, PersistantPlayerCamera.Brain);
            }
        }
    }

    private void OnDisable()
    {
        if (!LevelManager.Instance) return;

        if(LevelManager.Instance.currentLevelController)
            LevelManager.Instance.currentLevelController = null;
        if (PlayerManager.Instance && PlayerManager.Instance.playerObject)
            PlayerManager.Instance.playerObject.SetActive(false);
        //if (LightingManager.Instance.grass != null)
        //    LightingManager.Instance.grass.Clear();
    }

    /// <summary>
    /// Initialize things related to the current level. This happens for the primary scene only.
    /// </summary>
    public void LoadLevel()
    {
        Debug.Log($"Loading level {levelData.name}");

        LevelManager.Instance.currentLevelController = this;
        LevelManager.Instance.currentLevelData = levelData;

        SceneLoading.Invoke();

        if (cameraBlends)
            PersistantPlayerCamera.Brain.m_CustomBlends = cameraBlends;

        InitialSceneState initial = initialSceneStates.Find(x => x.fromScene == LevelManager.Instance.previousLevelData);
        if (initial == null)
        {
            initial = initialSceneStates.Find(x => x.fromScene == null);
            
        }

        PlayerManager.Instance.player.gameObject.SetActive(usePlayerCharacter);
        PlayerManager.Instance.player.controlledObject.Motor.enabled = true;
        if (useBackgroundColor)
        {
            Camera.main.backgroundColor = backgroundColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
        else
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }

        if (initial != null)
        {
            if (initial.setTime)
            {
                //LightingManager.Instance.TimeOfDay = initial.time;
            }

            if (usePlayerCharacter && initial.teleportPlayer && initial.playerTeleportTransform)
            {
                PlayerManager.Instance.TeleportPlayer(initial.playerTeleportTransform);
            }

            if (initial.useInitialPlayerState && initial.initialPlayerState)
            {
                PlayerManager.Instance.player.controlledObject.stateMachine.SetActionState(initial.initialPlayerState);
            }

            if (initial.useInitialTimeline)
            {
                initial.director.Play();
            }

            if(initial.useSetPlayerEquip && initial.attackState)
            {
                //PlayerManager.Instance.player.controlledObject.actionStates[(int)ActionStateEnums.Attack] = initial.attackState;
            }

            if (initial.useInitialCamera && initial.initialCamera)
            {
                initial.initialCamera.Priority = 1234;
                Camera.main.transform.position = initial.initialCamera.transform.position;
                Camera.main.transform.rotation = initial.initialCamera.transform.rotation;
            }

            //if (initial.useTerrain)
            //{
            //    LightingManager.Instance.grass.Add(initial.map);
            //}

            initial.OnLoad?.Invoke();
        }

        LevelManager.OnDoneLoading?.Invoke();
    }

    public void UnloadLevel()
    {
        SceneUnloading.Invoke();

        PlayerManager.Instance.player.controlledObject.Motor.enabled = false;
    }

    public void GoToNextLevel()
    {
        LevelManager.Instance.NextLevel();
    }
    public void GoToPreviousLevel()
    {
        LevelManager.Instance.PreviousLevel();
    }
    public void GoToLevel(int index)
    {
        LevelManager.Instance.LoadLevel(index);
    }

    public void GoToNextLevel(string transition = "loadingScreen")
    {
        LevelManager.Instance.NextLevel(transition);
    }
    public void LoadNextLevelAsync(int buildIndex)
    {
        LevelManager.Instance.LoadScene(buildIndex);
    }
    public void GoToPreviousLevel(string transition = "loadingScreen")
    {
        LevelManager.Instance.PreviousLevel(transition);
    }
    public void GoToLevel(int index, string transition = "loadingScreen")
    {
        LevelManager.Instance.LoadLevel(index, transition);
    }
    public void ReturnToTitle(string transition = "endScreen")
    {
        LevelManager.Instance.LoadMainMenu(true);
        MenuManager.Instance.EnableMenuDelay(5);
    }
}
