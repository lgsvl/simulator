/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TugbotHookComponent : MonoBehaviour
{
    // track
    public HingeJoint hookJoint;
    public Rigidbody hookRigidbody;

    //hook
    public GameObject hook;
    private bool isHooked = false;
    public bool IsHooked { get { return isHooked; } }

    private void Update()
    {
        ApplyTrackInput();
        ApplyHookInput();
    }

    #region track
    private void ApplyTrackInput()
    {
        if (Input.GetKey(KeyCode.H))
        {
            var spring = hookJoint.spring;
            spring.targetPosition += 0.25f;
            hookJoint.spring = spring;
        }

        if (Input.GetKey(KeyCode.J))
        {
            var spring = hookJoint.spring;
            spring.targetPosition -= 0.25f;
            hookJoint.spring = spring;
        }
    }

    public void CenterHook()
    {
        var spring = hookJoint.spring;
        spring.targetPosition = 0f;
        hookJoint.spring = spring;
    }

    #endregion

    #region hooked
    private void ApplyHookInput()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            ToggleHooked();
        }
    }

    public void ToggleHooked()
    {
        isHooked = !isHooked;
        hookJoint.useSpring = !isHooked;
        hook.transform.localEulerAngles = isHooked ? Vector3.zero : new Vector3(-30f, 0f, 0f);
        if (!isHooked)
        {
            List<PaletteComponent> palettes = new List<PaletteComponent>(FindObjectsOfType<PaletteComponent>());
            for (int i = 0; i < palettes.Count; i++)
            {
                palettes[i].ReleaseTugBot();
            }
        }
    }

    public bool EngageHook(bool engage)
    {
        if (engage && !isHooked)
        {
            ToggleHooked();
        }
        else if (!engage && IsHooked)
        {
            ToggleHooked();
        }
        return true;
    }
    #endregion
}
