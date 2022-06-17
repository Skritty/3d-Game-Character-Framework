// Written by: Trevor Thacker
// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Controls the loading of scenes and perpetuation of persistant objects.
/// </summary>
public class LevelManager : Singleton<LevelManager>
{
    // For debug/testing scripts
    public const int totalLevels = 10;

    public static Action OnStartLoading;
    public static Action OnDoneLoading;
    public static bool initialLoadDone = false;
    public float loadingProgress 
    {
        get
        {
            float progress = 0;
            foreach(AsyncOperation operation in operations)
            {
                progress += operation.progress;
            }
            return operations.Count == 0 ? 0f : progress / operations.Count;
        }
        private set
        {
            loadingProgress = value;
        }
    }
    private List<AsyncOperation> operations = new List<AsyncOperation>();

    [SerializeField]
    public List<LevelLoadData> levelLoadData = new List<LevelLoadData>();

    [SerializeField]
    private int startLevel = 0;
    public LevelLoadData currentLevelData;
    public LevelLoadData previousLevelData;
    public LevelController currentLevelController;

    // Prevent a level from being loaded while one is still loading in
    private bool loadInProgress = false;

    private void Start()
    {
        Debug.Log($"First level loaded? {initialLoadDone}");
        if (!initialLoadDone)
        {
            initialLoadDone = true;
            LoadLevel(startLevel);
        }
    }

    private void OnDisable()
    {
        initialLoadDone = false;
    }

    public void LoadMainMenu(bool finishedGame)
    {
        PlayerManager.Instance.SetPlayerInput(false);

        string t = "loadingScreen";
        if (finishedGame)
            t = "endScreen";
        loadInProgress = false;
        TransitionManager.OnTransitionMidpoint += LoadStart;
        TransitionManager.StartTransition(t);
        
        void LoadStart(string transition)
        {
            Debug.Log(transition);
            if (loadInProgress || transition != t) return;
            loadInProgress = true;
            TransitionManager.OnTransitionMidpoint -= LoadStart;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            PlayerManager.Instance.player.controlledObject.ResetHealth();

            int unloading = 0;
            for (int i = 1; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).buildIndex != 0)
                {
                    unloading++;
                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i)).completed += (x) => unloading--;
                }
            }

            yield return new WaitWhile(() => unloading > 0);

            LoadLevelInternal(levelLoadData[startLevel]);
            PlayerManager.Instance.SetPlayerInput(true);

            previousLevelData = null;
            MenuManager.Instance.ToggleMainMenu(true);
        }
    }

    /// <summary>
    /// Loads a level (main scene and any connected scenes)
    /// </summary>
    /// <param name="index">The level index to load</param>
    /// <param name="loadingScreen">The loading screen transition to use, put "" to not use one</param>
    public void LoadLevel(int index, string loadingScreen = "loadingScreen")
    {
        if (levelLoadData.IndexOf(currentLevelData) == index) return;

        LevelLoadData level = levelLoadData[index];

        if (loadingScreen != "")
        {
            PlayerManager.Instance.SetPlayerInput(false);
            TransitionManager.OnTransitionMidpoint += Load;
            TransitionManager.StartTransition(loadingScreen);
            loadInProgress = false;
            void Load(string transition)
            {
                Debug.Log($"Loading {loadInProgress}");
                if (loadInProgress || transition != loadingScreen) return;
                loadInProgress = true;
                Debug.Log($"{transition} loading level {level.name}");
                TransitionManager.OnTransitionMidpoint -= Load;
                Debug.Log("sfgasdfas");
                LoadLevelInternal(level);
                PlayerManager.Instance.SetPlayerInput(true);
                Debug.Log("sfgasdfas");
            }
         }
        else
        {
            LoadLevelInternal(level);
        }
    }

    /// <summary>
    /// Loads a level (main scene and any connected scenes)
    /// </summary>
    /// <param name="levelData">The level data to load</param>
    /// <param name="loadingScreen">The loading screen transition to use, leave blank to not use one</param>
    public void LoadLevel(LevelLoadData levelData, string loadingScreen = "loadingScreen")
    {
        LevelLoadData level = levelData;

        if (loadingScreen != "")
        {
            loadInProgress = false;
            PlayerManager.Instance.SetPlayerInput(false);
            TransitionManager.OnTransitionMidpoint += Load;
            TransitionManager.StartTransition(loadingScreen);
            void Load(string transition)
            {
                if (loadInProgress || transition != loadingScreen) return;
                loadInProgress = true;
                Debug.Log($"{transition} loading level {level.name}");
                TransitionManager.OnTransitionMidpoint -= Load;
                LoadLevelInternal(level);
                PlayerManager.Instance.SetPlayerInput(true);
            }
        }
        else
        {
            LoadLevelInternal(level);
        }
    }

    private void LoadLevelInternal(LevelLoadData level)
    {
        BarkManager.Instance?.ClearAllBarks();

        previousLevelData = currentLevelData;
        currentLevelData = level;

        //Confirm that the persistant stuff exists
        if (!SceneManager.GetSceneByBuildIndex(0).isLoaded)
            LoadScene(0);

        // Unload all non-persistant scenes
        LevelUnloading();
        for (int i = 1; i < SceneManager.sceneCount; i++)
        {
            if (!level.AdditiveSceneBuildIndecies.Contains(SceneManager.GetSceneAt(i).buildIndex) && !(level.PrimarySceneBuildIndex == SceneManager.GetSceneAt(i).buildIndex))
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        // Load the primary scene
        if (!SceneManager.GetSceneByBuildIndex(level.PrimarySceneBuildIndex).isLoaded)
        {
            LoadScene(level.PrimarySceneBuildIndex).completed += (x) =>
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(level.PrimarySceneBuildIndex));
                LevelLoading(GetLevelControllerByPrimaryIndex(level.PrimarySceneBuildIndex));
            };
        }
        else
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(level.PrimarySceneBuildIndex));
            LevelLoading(GetLevelControllerByPrimaryIndex(level.PrimarySceneBuildIndex));
        }

        // Load the additional scenes
        foreach (int buildIndex in level.AdditiveSceneBuildIndecies)
            if (!SceneManager.GetSceneByBuildIndex(buildIndex).isLoaded)
                LoadScene(buildIndex);
            else
                GetLevelControllerByPrimaryIndex(buildIndex).Initialize();
        loadInProgress = false;
    }

    public void NextLevel(string loadingScreen = "loadingScreen")
    {
        if (!currentLevelData) return;
        int next = levelLoadData.IndexOf(currentLevelData) + 1;
        if (next < levelLoadData.Count)
        {
            LoadLevel(next, loadingScreen);
        }
    }

    public void PreviousLevel(string loadingScreen = "loadingScreen")
    {
        if (!currentLevelData) return;
        int previous = levelLoadData.IndexOf(currentLevelData) - 1;
        if (previous >= 0)
        {
            LoadLevel(previous, loadingScreen);
        }
    }

    public AsyncOperation LoadScene(int buildIndex)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);
        operations.Add(load);
        /*load.allowSceneActivation = false;
        StartCoroutine(Delay());
        IEnumerator Delay() 
        {
            while (load.progress != 0.9f)
            {
                Debug.Log("test");
                yield return new WaitForEndOfFrame();
            }
            //yield return new WaitUntil(() => load.isDone);
            yield return new WaitForSeconds(5); 
            load.allowSceneActivation = true;
            //Debug.Log("test");
        }*/
        return load;
    }

    /*private static bool SceneAlreadyLoaded(int buildIndex)
    {
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        bool exists = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i) == scene)
                exists = true;
        }

        return exists;
    }*/

    private LevelController GetLevelControllerByPrimaryIndex(int primaryBuildIndex)
    {
        foreach(LevelController c in GameObject.FindObjectsOfType<LevelController>())
        {
            if (c.levelData.PrimarySceneBuildIndex == primaryBuildIndex)
                return c;
        }
        return null;
    }

    private void LevelLoading(LevelController currentLevelController)
    {
        currentLevelController?.LoadLevel();
    }

    private void LevelUnloading()
    {
        currentLevelController?.UnloadLevel();
    }
}
