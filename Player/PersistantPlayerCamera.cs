using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PersistantPlayerCamera : Singleton<PersistantPlayerCamera>
{
    public static CinemachineBrain Brain;
    
    public CinemachineBlenderSettings[] cinemachineBlends;
    private int currentSceneIndex;

    private void Awake()
    {
        Brain = GetComponent<CinemachineBrain>();

        if (cinemachineBlends[currentSceneIndex] != null)
            Brain.m_CustomBlends = cinemachineBlends[currentSceneIndex];
        else
            Brain.m_CustomBlends = null;

        StartCoroutine(SubscribeToManagers());
    }

    IEnumerator SubscribeToManagers() //USE THIS TO COMMUNICATE WITH OTHER MANAGERS IN START FUNCTIONS TO AVOID RACE CONDITIONS WITH REQUESTED MANAGER ASSIGNING ITS STATIC CURRENT REFERENCE
	{
        yield return new WaitForEndOfFrame();
        //MANAGER CODE GO HERE
        if(LevelManager.Instance.currentLevelData)
            currentSceneIndex = LevelManager.Instance.currentLevelData.PrimarySceneBuildIndex;
    }
}
