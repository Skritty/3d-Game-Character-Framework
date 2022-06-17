using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Progress/Progress Trackers/Checkpoint Progress Tracker")]
public class CheckpointProgressTracker : GenericProgressTracker
{
    [ShowInInspector, DisableInEditorMode, PropertyOrder(8), Header("Checkpoint Data")]
    public static CheckpointProgressTracker currentCheckpoint;
    public override bool isReached 
    {
        get => _isReached;

        set
        {
            _isReached = value;
            if(value)
                currentCheckpoint = this;
            ProgressUpdated?.Invoke();
            TrackerUpdated?.Invoke(this);
        }
    }

    private Transform _checkpoint;
    private CheckpointHelper cp;
    [ShowInInspector, PropertyOrder(9)]
    private Transform SetCheckpoint
    {
        get => _checkpoint;
        set
        {
            if(value == null)
            {
                cp.OnTransformChanged -= UpdateCheckpoint;
                Destroy(cp);
                _checkpoint = null;
                scene = "";
                _buildIndex = -1;
            }
            else
            {
                _checkpoint = value;
                cp = _checkpoint.gameObject.AddComponent<CheckpointHelper>();
                cp.OnTransformChanged += UpdateCheckpoint;
                UpdateCheckpoint();
                Scene s = SceneManager.GetActiveScene();
                scene = s.name;
                _buildIndex = s.buildIndex;
            }
        }
    }

    private void UpdateCheckpoint()
    {
        _position = _checkpoint.transform.position;
        _rotation = _checkpoint.transform.eulerAngles;
        _qRotation = _checkpoint.transform.rotation;
    }

    [LabelText("Checkpoint Position"), SerializeField, DisableInEditorMode, PropertyOrder(10)]
    private Vector3 _position;
    public Vector3 Position => _position;

    [LabelText("Checkpoint Rotation"), SerializeField, DisableInEditorMode, PropertyOrder(11)]
    private Vector3 _rotation;
    private Quaternion _qRotation;
    public Quaternion Rotation => _qRotation;

    [LabelText("Scene Location"), SerializeField, DisableInEditorMode, PropertyOrder(12)]
    private string scene;

    // CHANGE THIS TO LEVEL INSTEAD
    private int _buildIndex;
    public int BuildIndex => _buildIndex;

    [Button(ButtonHeight = 50), PropertyOrder(-100)]
    private void GoToCheckpoint()
    {
        prerequisiteTracker?.ChainActivate();
        isReached = true;
        //SceneManager.LoadScene(_buildIndex);
        PlayerManager.Instance?.player?.controlledObject.Motor.SetPositionAndRotation(_position, _qRotation);
    }
}
