// Written by: Trevor Thacker
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBarTransitionController : TransitionController
{
    [SerializeField]
    private Slider loadingBar;
    [SerializeField]
    private GameObject containerPanel;

    private void Start()
    {
        loadingBar.value = 0;
    }
    protected override void StartTransition()
    {
        loadingBar.value = 0;
        StartCoroutine(UpdateLoadingBar());
    }

    private IEnumerator UpdateLoadingBar()
    {
        while(loadingBar.value < 1)
        {
            loadingBar.value = LevelManager.Instance.loadingProgress;
            yield return new WaitForEndOfFrame();
        }
    }
}
