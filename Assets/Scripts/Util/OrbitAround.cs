/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class OrbitAround : MonoBehaviour {

    public GameObject target;
    public float rotationSpeed;
    public float distance;
    public float height;
    public float pitch;
    public float xOffset;
    public float yaw;

    public const float moveTime = 1f;

    private const float farDist = 8.5f;
    private const float farHeight = 1.1f;
    private const float farPitch = 7.2f;
    private const float farOffset = 0f;
    private const float farYaw = -88.9f;
    private const float farMovetime = 1.5f;

    private const float closeDist = 13.4f;
    private const float closeHeight = 0.9f;
    private const float closePitch = 7f;
    private const float closeOffset = 0f;
    private const float closeYaw = -88f;



	// Use this for initialization
	void Start () {
       // yaw = 0f;
	}
	
	// Update is called once per frame
	void Update () {
        if(target != null)
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target.transform.position - distance * transform.forward + height * Vector3.up + xOffset * Vector3.right;
            yaw += rotationSpeed * Time.deltaTime;
            if(yaw > 360)
                yaw -= 360;
        }
	}

    public void LerpYaw(float newYaw, System.Action onFinish)
    {
        StartCoroutine(_LerpYaw(newYaw, onFinish));
    }

    IEnumerator _LerpYaw(float newYaw, System.Action onFinish)
    {
        float startYaw = yaw;
        float startTime = Time.time;
        while(Time.time - startTime < moveTime)
        {
            float t = (Time.time - startTime) / moveTime;
            yaw = Mathf.Lerp(startYaw, newYaw, t);
            yield return null;
        }
        yaw = newYaw;
        if(onFinish != null)
            onFinish();
    }

    public void Move(GameObject newTarget)
    {
        if(target == null)
            return;

        target = null;
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPosition = newTarget.transform.position - distance * (targetRotation * Vector3.forward) + height * Vector3.up + xOffset * Vector3.right; ;
        //yaw = 0f;
        StartCoroutine(_DoMove(targetPosition, targetRotation, newTarget));
    }

    public void MoveToFar(System.Action onComplete)
    {
        StartCoroutine(_MoveToFar(onComplete));
    }

    public IEnumerator _MoveToFar(System.Action onComplete)
    {
        float startPitch = pitch;
        float startYaw = yaw;
        float startDist = distance;
        float startHeight = height;
        float startOffset = xOffset;
        float startTime = Time.time;

        while(Time.time - startTime < farMovetime)
        {
            float t = (Time.time - startTime) / farMovetime;
            pitch = Mathf.Lerp(startPitch, farPitch, t);
            yaw = Mathf.Lerp(startYaw, farYaw, t);
            distance = Mathf.Lerp(startDist, farDist, t);
            height = Mathf.Lerp(startHeight, farHeight, t);
            xOffset = Mathf.Lerp(startOffset, farOffset, t);
            yield return null;
        }
        pitch = farPitch;
        yaw = farYaw;
        distance = farDist;
        height = farHeight;
        xOffset = farOffset;

        if(onComplete != null)
            onComplete();
    }

    public void MoveToClose(System.Action onComplete)
    {
        StartCoroutine(_MoveToClose(onComplete));
    }

    public IEnumerator _MoveToClose(System.Action onComplete)
    {
        float startPitch = pitch;
        float startYaw = yaw;
        float startDist = distance;
        float startHeight = height;
        float startOffset = xOffset;
        float startTime = Time.time;

        while(Time.time - startTime < farMovetime)
        {
            float t = (Time.time - startTime) / farMovetime;
            pitch = Mathf.Lerp(startPitch, closePitch, t);
            yaw = Mathf.Lerp(startYaw, closeYaw, t);
            distance = Mathf.Lerp(startDist, closeDist, t);
            height = Mathf.Lerp(startHeight, closeHeight, t);
            xOffset = Mathf.Lerp(startOffset, closeOffset, t);
            yield return null;
        }
        pitch = closePitch;
        yaw = closeYaw;
        distance = closeDist;
        height = closeHeight;
        xOffset = closeOffset;

        if(onComplete != null)
            onComplete();
    }

    private IEnumerator _DoMove(Vector3 pos, Quaternion rot, GameObject newTarget)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startTime = Time.time;

        while(Time.time - startTime < moveTime)
        {
            float t = (Time.time - startTime)/moveTime;
            transform.position = Vector3.Lerp(startPos, pos, t);
            transform.rotation = Quaternion.Slerp(startRot, rot, t);
            yield return null;
        }

        target = newTarget;
    }
}
