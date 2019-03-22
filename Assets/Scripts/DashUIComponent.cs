using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashUIComponent : MonoBehaviour
{
    public Image ignitionImage;
    public Image wiperImage;
    public Image lightsImage;
    public Image parkingBrakeImage;
    public Image shiftImage;

    #region toggles
    public void SetDashIgnitionUI(Color color)
    {
        ignitionImage.color = color;
    }

    public void SetDashWiperUI(Color color, Sprite sprite)
    {
        wiperImage.color = color;
        wiperImage.sprite = sprite;
    }

    public void SetDashLightsUI(Color color, Sprite sprite)
    {
        lightsImage.color = color;
        lightsImage.sprite = sprite;
    }

    public void SetDashParkingBrakeUI(Color color)
    {
        parkingBrakeImage.color = color;
    }

    public void SetDashShiftUI(Color color, Sprite sprite)
    {
        shiftImage.color = color;
        shiftImage.sprite = sprite;
    }
    #endregion
}
