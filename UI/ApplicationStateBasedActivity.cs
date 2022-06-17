using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this script on the UI panel object
/// </summary>
public class ApplicationStateBasedActivity : MonoBehaviour
{
    /// <summary>
    /// The states that enable this panel
    /// </summary>
    [Tooltip("The states that enable this panel")]
    public AppState activeStates;

    private void Awake()
    {
        CheckState(ApplicationState.CurrentState);
        ApplicationState.OnStateChanged += CheckState;
    }

    private void OnDestroy()
    {
        ApplicationState.OnStateChanged -= CheckState;
    }

    private void CheckState(AppState currentState)
    {
        if (((long)activeStates & (long)currentState) != 0)
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);
    }
}
