using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.IO;

public class ProgressManager : Singleton<ProgressManager>
{
    const string fileName = "ParacosmSave.json";
    [SerializeField, InfoBox("DO_NOT_SAVE will prevent any progress from saving. Ensure that this is disabled when building or testing progress mechanics. It will also prevent the resetting of all progress to save locally.", infoMessageType: InfoMessageType.Error)]
    private bool DO_NOT_SAVE = false;
    [SerializeField]
    private List<GenericProgressTracker> progressTrackers;
    [SerializeField]
    private List<ProgressDictionary> stateProgressDictionaries;

    [System.Serializable]
    public class ProgressSaveWrapper
    {
        public List<TrackerContainer> trackerList = new List<TrackerContainer>();
        public List<DictionaryContainer> dictionaryList = new List<DictionaryContainer>();

        [System.Serializable]
        public struct TrackerContainer
        {
            public string name;
            public bool isReached;
            public TrackerContainer(string name, bool isReached)
            {
                this.name = name;
                this.isReached = isReached;
            }
        }

        [System.Serializable]
        public struct DictionaryContainer
        {
            public string name;
            public int current;

            public DictionaryContainer(string name, int current)
            {
                this.name = name;
                this.current = current;
            }
        }

        public ProgressSaveWrapper() { }
        public ProgressSaveWrapper(List<GenericProgressTracker> progressTrackers, List<ProgressDictionary> stateProgressDictionaries)
        {
            trackerList.Clear();
            foreach (GenericProgressTracker t in progressTrackers)
                trackerList.Add(new TrackerContainer(t.name, t.isReached));

            dictionaryList.Clear();
            foreach (ProgressDictionary d in stateProgressDictionaries)
            {
                int i = progressTrackers.IndexOf(d.currentProgressTracker);
                if (i == -1)
                    Debug.LogWarning($"{d.currentProgressTracker} not in ProgressManager.progressTrackers! This will prevent it from being saved.");
                else
                    dictionaryList.Add(new DictionaryContainer(d.name, i));
            }
        }

        public void Read(List<GenericProgressTracker> progressTrackers, List<ProgressDictionary> stateProgressDictionaries)
        {
            // Update the bools of all trackers
            foreach (TrackerContainer container in trackerList)
            {
                GenericProgressTracker t = progressTrackers.Find(x => x.name == container.name);
                if (t)
                {
                    t.isReached = container.isReached;
                }
            }

            // Update the currents of all state dictionaries
            foreach(DictionaryContainer container in dictionaryList)
            {
                ProgressDictionary d = stateProgressDictionaries.Find(x => x.name == container.name);
                if (d)
                {
                    d.currentProgressTracker = progressTrackers[container.current];
                }
            }
        }
    }
    
    private void Start()
    {
        // Clear all checkpoint data and update it from the save file
        foreach(GenericProgressTracker tracker in progressTrackers)
        {
            tracker.SetReachedNoUpdates(false);
        }
        LoadProgressFromJSON();

        // Set up automatic tracker updating of state dictionaries
        foreach (ProgressDictionary d in stateProgressDictionaries)
            d.EnableAutoUpdate();
    }

    private void OnEnable()
    {
        GenericProgressTracker.ProgressUpdated += SaveProgressToJSON;
    }

    private void OnDisable()
    {
        GenericProgressTracker.ProgressUpdated -= SaveProgressToJSON;
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        SaveProgressToJSON();
    }

    private void SaveProgressToJSON()
    {
        if (DO_NOT_SAVE) return;
        string path = $"{Application.persistentDataPath}/{fileName}";
        ProgressSaveWrapper wrapper = new ProgressSaveWrapper(progressTrackers, stateProgressDictionaries);
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);
        Debug.Log($"Progress saved to {path}");
    }

    private void LoadProgressFromJSON()
    {
        string path = $"{Application.persistentDataPath}/{fileName}";
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        JsonUtility.FromJson<ProgressSaveWrapper>(json).Read(progressTrackers, stateProgressDictionaries);
        Debug.Log($"Progress loaded");
    }

    [Button, PropertyOrder(-1)]
    private void ResetAllProgress()
    {
        foreach (GenericProgressTracker tracker in progressTrackers)
        {
            tracker.isReached = false;
        }
        foreach (ProgressDictionary d in stateProgressDictionaries)
        {
            d.currentProgressTracker = null;
        }
        SaveProgressToJSON();
    }
}