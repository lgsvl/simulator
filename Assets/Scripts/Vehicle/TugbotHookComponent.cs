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
    public GameObject hookTrack;
    private bool isTrackLeft = false;
    private bool isTrackRight = false;

    //hook
    public GameObject hook;
    private bool isHooked = false;
    public bool IsHooked { get { return isHooked; } }

    private void Update()
    {
        CheckTrackLeftRight();
        ApplyTrackInput();
        ApplyHookInput();
    }

    #region track
    private void CheckTrackLeftRight()
    {
        if (hookTrack.transform.localEulerAngles.y >= 0 && hookTrack.transform.localEulerAngles.y < 45)
        {
            isTrackLeft = true;
            isTrackRight = false;
        }
        else if (hookTrack.transform.localEulerAngles.y <= 359 && hookTrack.transform.localEulerAngles.y > 320)
        {
            isTrackRight = true;
            isTrackLeft = false;
        }
    }

    private void ApplyTrackInput()
    {
        if (Input.GetKey(KeyCode.H))
        {
            if (hookTrack.transform.localEulerAngles.y < 45 || isTrackRight)
                hookTrack.transform.Rotate(Vector3.up, Space.Self);
        }

        if (Input.GetKey(KeyCode.J))
        {
            if (hookTrack.transform.localEulerAngles.y > 320 || isTrackLeft)
                hookTrack.transform.Rotate(Vector3.down, Space.Self);
        }
    }

    public void CenterHook()
    {
        if (hookTrack.transform.localEulerAngles.y < 45 || isTrackRight)
            hookTrack.transform.Rotate(new Vector3(0f, - hookTrack.transform.localEulerAngles.y, 0f), Space.Self);
        else if (hookTrack.transform.localEulerAngles.y > 320 || isTrackLeft)
            hookTrack.transform.Rotate(new Vector3(0f, (360f - hookTrack.transform.localEulerAngles.y), 0f), Space.Self);
 
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

    private void ToggleHooked()
    {
        isHooked = !isHooked;
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
