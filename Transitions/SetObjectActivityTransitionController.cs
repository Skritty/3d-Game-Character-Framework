using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetObjectActivityTransitionController : TransitionController
{
    [SerializeField]
    private bool activity = false;
    [SerializeField]
    private GameObject[] toChange;

    protected override void StartTransition()
    {
        foreach (GameObject o in toChange)
            o.SetActive(activity);
    }
}