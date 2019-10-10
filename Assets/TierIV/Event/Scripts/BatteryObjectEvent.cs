using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TierIV.Event;


public class BatteryObjectEvent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    void OnEnable()
    {
        EventNotifier.Instance.SubscribeEvent(OnReceiveChargeEvent, "Charge");
    }

    void OnDisable()
    {
        EventNotifier.Instance.UnSubscribeEvent(OnReceiveChargeEvent);
    }

    void OnReceiveChargeEvent(string chargeEventJson)
    {
        // parse chargeEventJson to get an appropriate value ...
    }
}
