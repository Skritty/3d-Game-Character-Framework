// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles UI transitions, such as UI fade and other effects
/// </summary>
public class UIFadeTransitionController : TransitionController
{
    [Tooltip("Uses % of the total frameDuration")]
    [SerializeField]
    private AnimationCurve fadeCurve;

    [SerializeField]
    private Image[] images;
    [SerializeField]
    private TMPro.TextMeshProUGUI[] text;

    private float progress = 0;

    private void Start()
    {
        FadeImages(fadeCurve.Evaluate(0));
        FadeText(fadeCurve.Evaluate(0));
    }
    protected override void StartTransition()
    {
        FadeImages(fadeCurve.Evaluate(0));
        FadeText(fadeCurve.Evaluate(0));
        progress = 0;
        StartCoroutine(DoFade());
    }

    private IEnumerator DoFade()
    {
        bool midpointDone = false;
        float duration = fadeCurve.keys[fadeCurve.length - 1].time;
        if (duration <= 0)
            progress = duration;
        while (progress < duration)
        {
            float a = Mathf.Clamp01(fadeCurve.Evaluate(progress/duration));
            FadeImages(a);
            FadeText(a);

            if (sendCallbacks && !midpointDone )//&& (int)(duration / 2) == (int)progress)
            {
                Debug.Log(transition);
                midpointDone = true;
                TransitionManager.OnTransitionMidpoint?.Invoke(transition);
                
            }
            yield return new WaitForEndOfFrame();
            progress += Time.deltaTime;
        }
        FadeImages(fadeCurve.Evaluate(duration));
        FadeText(fadeCurve.Evaluate(duration));
        if (sendCallbacks)
            TransitionManager.OnTransitionEnd?.Invoke(transition);
    }

    private void FadeImages(float a)
    {
        foreach (Image i in images)
        {
            Color c = i.color;
            c.a = a;
            i.color = c;
            if (a <= 0)
                i.enabled = false;
            else
                i.enabled = true;
        }
    }

    private void FadeText(float a)
    {
        foreach (TMPro.TextMeshProUGUI t in text)
        {
            Color c = t.color;
            c.a = a;
            t.color = c;
            if (a <= 0)
                t.enabled = false;
            else
                t.enabled = true;
        }
    }
}
