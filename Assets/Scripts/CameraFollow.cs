/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


﻿using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour {
    public Transform target;
    private Vector3 offset;
    public bool rigidFollow = false;
    public float lerpAmount = 0.005f;

    Vector3 oldPosition;
    Quaternion oldRotation;
    bool TopDownView = false;

    Rect Limits = Rect.MinMaxRect(-7, -7, +7, +7);
    float MinY = 1.0f;
    float MaxY = 10.0f;
    Vector2 LastPosition;

    private void Start()
    {
        offset = transform.position - target.transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            rigidFollow = !rigidFollow;
        }

        lerpAmount = rigidFollow ? 1.0f : 0.005f;

        if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current.currentSelectedGameObject == null)
        {
            TopDownView = !TopDownView;

            if (TopDownView)
            {
                oldPosition = transform.position;
                oldRotation = transform.rotation;
                transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.LookRotation(new Vector3(0, -1, 0)));
            }
            else
            {
                transform.SetPositionAndRotation(oldPosition, oldRotation);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (TopDownView)
        {
            var pos = transform.position;

            if (Input.GetMouseButtonDown(1))
            {
                LastPosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(1))
            {
                Vector2 newPos = Input.mousePosition;
                pos.x -= 0.01f * (newPos.x - LastPosition.x);
                pos.z -= 0.01f * (newPos.y - LastPosition.y);
                LastPosition = newPos;
            }

            pos.y -= Input.mouseScrollDelta.y;

            pos.x = Mathf.Clamp(pos.x, Limits.xMin, Limits.xMax);
            pos.y = Mathf.Clamp(pos.y, MinY, MaxY);
            pos.z = Mathf.Clamp(pos.z, Limits.yMin, Limits.yMax);

            transform.position = pos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, offset + target.position, lerpAmount);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lerpAmount);
        }
    }
}
