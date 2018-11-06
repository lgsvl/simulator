/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

// CameraView: If you add something to this enum, make sure you add it to the SwitchView function.
//public enum CameraView { DRIVER, DRIVERNOMIRROR, DRIVERDASH, HOOD, THIRDPERSON, REVERSE };
public enum CameraView { DRIVER, THIRDPERSON, REVERSE };

public class DriverCamera : MonoBehaviour
{
    private CamSmoothFollow smoothFollow;
    private CamFixTo fixTo;

    public GameObject carObject;

    public bool showControl = false;

    public LayerMask playerCarMask;

    private float angle;

    public Transform driverCameraPosition;
    public Transform hoodCameraPosition;
    public Transform thirdPersonCameraPosition;
    public Transform reverseViewCameraPosition;

    private CameraView currentCameraView;

    const float selectFov = 65f;    

    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        fixTo = GetComponent<CamFixTo>();
        smoothFollow = GetComponent<CamSmoothFollow>();

        Init();
        ViewInside();
        SwitchView(CameraView.THIRDPERSON);

        SetCameraType(selectFov);

        carObject.GetComponent<CarInputController>()[InputEvent.CHANGE_CAM_VIEW].Press += SwitchView;
    }

    void OnDestroy()
    {
        carObject.GetComponent<CarInputController>()[InputEvent.CHANGE_CAM_VIEW].Press -= SwitchView;
    }

    public void SetCameraType(float fov)
    {
        angle = fov;
        RecalculateCam();
    }

    public void SetFarClip(float farClip)
    {
        cam.farClipPlane = farClip;
    }

    public void SetNearClip(float nearClip)
    {
        cam.nearClipPlane = nearClip;
    }

    public void SetFoV(float fov)
    {
        angle = fov;
        RecalculateCam();
    }

    public void ToggleCamSettings()
    {
        showControl = !showControl;
    }

    void RecalculateCam()
    {
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.fieldOfView = Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(angle * Mathf.Deg2Rad / 2) / cam.aspect);
    }

    public void Init()
    {
        fixTo.fixTo = driverCameraPosition;
        currentCameraView = CameraView.DRIVER;
    }

    public void ViewInside()
    {
        fixTo.fixTo = driverCameraPosition;
        fixTo.enabled = true;
        smoothFollow.enabled = false;
        LayerMask mask = cam.cullingMask & ~playerCarMask;
        cam.cullingMask = mask;
        currentCameraView = CameraView.DRIVER;
    }

    public void SetCullingMask(int mask)
    {
        cam.cullingMask = mask;
    }

    private void DisplayCarInCamera(bool disp)
    {
        LayerMask mask;
        if (disp)
        {
            mask = cam.cullingMask | playerCarMask;
        }
        else
        {
            mask = cam.cullingMask & ~playerCarMask;
        }
        SetCullingMask(mask);
    }


    public void SwitchView(CameraView view)
    {
        currentCameraView = view;

        Transform pos;
        // Debug.LogWarning("**** New Camera View " + currentCameraView + " " + (int)currentCameraView + " / " + (System.Enum.GetNames(typeof(CameraView)).Length - 1));
        switch (view)
        {
            default:
            case CameraView.DRIVER: // fallthrough intentional from default
                DisplayCarInCamera(false);
                pos = driverCameraPosition;
                smoothFollow.enabled = false;
                fixTo.enabled = true;
                break;
            //case CameraView.DRIVERNOMIRROR:
            //    DisplayCarInCamera(false);
            //    pos = driverCameraPosition;
            //    smoothFollow.enabled = false;
            //    fixTo.enabled = true;
            //    break;
            //case CameraView.DRIVERDASH:
            //    DisplayCarInCamera(true);
            //    pos = driverCameraPosition;
            //    smoothFollow.enabled = false;
            //    fixTo.enabled = true;
            //    break;
            //case CameraView.HOOD:
            //    if (!hoodCameraPosition)
            //    {
            //        ViewInside();
            //        return;
            //    }
            //    DisplayCarInCamera(true);
            //    pos = hoodCameraPosition;
            //    smoothFollow.enabled = false;
            //    fixTo.enabled = true;
            //    break;
            case CameraView.THIRDPERSON:
                if (!thirdPersonCameraPosition)
                {
                    ViewInside();
                    return;
                }
                DisplayCarInCamera(true);
                pos = thirdPersonCameraPosition;
                smoothFollow.enabled = true;
                fixTo.enabled = false;
                break;
            case CameraView.REVERSE:
                if (!reverseViewCameraPosition)
                {
                    ViewInside();
                    return;
                }
                DisplayCarInCamera(true);
                pos = reverseViewCameraPosition;
                smoothFollow.enabled = true;
                fixTo.enabled = false;
                break;
        }
        smoothFollow.targetPositionTransform = pos;
        fixTo.fixTo = pos;
    }

    public void SwitchView()
    {
        currentCameraView++;
        if ((int)currentCameraView >= System.Enum.GetNames(typeof(CameraView)).Length)
        {
            currentCameraView = CameraView.DRIVER;
        }
        SwitchView(currentCameraView);
    }

    public static float ScaleFov(float fovAtThreeScreens)
    {
        var fov = fovAtThreeScreens;
        return fov * 4 / 5 + (fov / 5) / 2 * (((float)Screen.width / Screen.height) / (16f / 9f) - 1);
    }
}
