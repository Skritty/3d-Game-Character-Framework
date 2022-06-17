using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFlyToTransitionController : TransitionController
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float duration;

    protected override void StartTransition()
    {
        StartCoroutine(CameraFlyToTarget());
    }

    private IEnumerator CameraFlyToTarget()
    {
        Camera cam = Camera.main;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        float currentTime = 0;
        if (duration <= 0)
            cam.transform.position = target.position;
        while (cam.transform.position != target.position && cam.transform.rotation != target.rotation)
        {
            cam.transform.position = Vector3.Lerp(startPos, target.position, (currentTime += Time.deltaTime) / duration);
            cam.transform.rotation = Quaternion.Slerp(startRot, target.rotation, (currentTime += Time.deltaTime) / duration);
            yield return new WaitForEndOfFrame();
        }
        cam.transform.position = target.position;
        cam.transform.rotation = target.rotation;
        TransitionManager.OnTransitionEnd.Invoke(transition);
    }
}