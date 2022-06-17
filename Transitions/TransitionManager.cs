// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Handles transitions such as UI fading, loading screens, and 3d transitions with dissolve, scale, etc
/// </summary>
public class TransitionManager : Singleton<TransitionManager>
{
    /// <summary>
    /// Used to trigger all transitions, which check if their transition name is the same as the one sent
    /// </summary>
    public static Action<string> OnTransitionBegin;

    /// <summary>
    /// Invoked by transitions. Sends the transition name
    /// </summary>
    public static Action<string> OnTransitionMidpoint;

    /// <summary>
    /// Invoked by transitions. Sends the transition name
    /// </summary>
    public static Action<string> OnTransitionEnd;

    public static void StartTransition(string transition)
    {
        //Debug.Log($"Starting transition: {transition}");
        OnTransitionBegin?.Invoke(transition);
    }
}
