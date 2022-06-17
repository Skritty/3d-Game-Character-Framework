using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Flags]
public enum AppState 
{
    None = 0,
    MainMenu = 1,
    Start = 2,
    CharacterSelect = 4,
    Results = 8,
    TrainingMode = 16,
    VersusMode = 32,
    Pause = 64,
    Battle = 128
};

[CreateAssetMenu(menuName = "Application State", fileName = "Application State")]
public class ApplicationState : ScriptableObject
{
    #region Static
    // Change this to the initial state!
    private static AppState _currentState = AppState.Start;
    public static AppState CurrentState
    {
        get
        {
            return _currentState;
        }
        private set
        {
            _currentState = value;
            OnStateChanged?.Invoke(value);
        }
    }

    public static Action<AppState> OnStateChanged;

    static ApplicationState()
    {
        OnStateChanged = null;
    }

    /// <summary>
    /// Will enable passed in UI panel and any others assigned to that state
    /// </summary>
    private static void SetState(ApplicationState state)
    {
        // If this app state can coexist with what states are existing
        if ((CurrentState & state.BlockedBy) == AppState.None)
        {
            SetStateInternal(state);
        }
        Debug.Log($"Current Application States: {CurrentState}");
    }

    private static void SetStateInternal(ApplicationState state)
    {
        // Toggle states off if they are all already on
        if (state.Toggle && CurrentState.HasFlag(state.States))
        {
            CurrentState &= ~state.States;
            StateDisabled(state);

        }
        // Otherwise, add the states
        else
        {
            CurrentState &= state.IgnoreOverride;
            CurrentState |= state.States;
            StateEnabled(state);
        }
    }

    private static void StateEnabled(ApplicationState state)
    {
        if (state.LoadScene >= 0 && state.LoadScene < SceneManager.sceneCountInBuildSettings && !IsSceneLoaded(state.LoadScene))
        {
            if (state.concurrentLoad)
                SceneManager.LoadSceneAsync(state.LoadScene, LoadSceneMode.Additive);
            else
                SceneManager.LoadSceneAsync(state.LoadScene, LoadSceneMode.Single);
        }

        if (state.LockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if(state.Pause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;

        state.OnActivate?.Invoke();
    }

    private static void StateDisabled(ApplicationState state)
    {
        if (state.concurrentLoad && state.LoadScene >= 0 && state.LoadScene < SceneManager.sceneCountInBuildSettings && IsSceneLoaded(state.LoadScene))
        {
            SceneManager.UnloadSceneAsync(state.LoadScene);
        }

        if (state.Pause)
            Time.timeScale = 1;
    }

    private static bool IsSceneLoaded(int buildIndex)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).buildIndex == buildIndex)
                return true;
        return false;
    }
    #endregion

    #region Scriptable Object
    [field: SerializeField, Tooltip("States to be set"), Header("State Info")]
    public AppState States { get; private set; }
    [field: SerializeField, Tooltip("Cannot be set if any of these states are active")]
    public AppState BlockedBy { get; private set; }
    [field: SerializeField, Tooltip("Does not override the selected states")]
    public AppState IgnoreOverride { get; private set; }
    [field: SerializeField, Tooltip("Will toggle states (and scenes) if set multiple times")]
    public bool Toggle { get; private set; } = false;

    [field: SerializeField, Tooltip("Build index. -1 is do not load"), Header("Scene Loading")]
    public int LoadScene { get; private set; } = -1;
    [field: SerializeField, Tooltip("When this state is toggled, it will unload the scene")]
    public bool concurrentLoad = false;

    public UnityEngine.Events.UnityAction OnActivate;
    [field: SerializeField, Header("Misc")]
    public bool LockCursor { get; private set; } = false;
    [field: SerializeField]
    public bool Pause { get; private set; } = false;

    public void SetStateActive()
    {
        SetState(this);
    }
    #endregion
}
