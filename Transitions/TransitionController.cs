// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TransitionController : MonoBehaviour
{
    [Tooltip("Will start the transition if transitions with this name are called")]
    [SerializeField]
    protected string transition = "";
    [SerializeField]
    protected bool sendCallbacks = false;

    private void OnEnable()
    {
        TransitionManager.OnTransitionBegin += ProcessTransition;
    }

    private void OnDisable()
    {
        TransitionManager.OnTransitionBegin -= ProcessTransition;
    }

    private void ProcessTransition(string _transition)
    {
        if (transition == _transition)
            StartTransition();
    }

    protected abstract void StartTransition();
}
