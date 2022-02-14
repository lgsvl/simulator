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

    void OnReceiveChargeEvent(EventArgsBase chargeEvent)
    {
        if (typeof(ChargeStationEvent.BatteryVolume).GetHashCode() != chargeEvent.TypeHash)
        {
            throw new System.ArgumentException(string.Format("different class hashcode {0}({1}) / {2}({3})",
                typeof(ChargeStationEvent.BatteryVolume).GetHashCode(),
                typeof(EventArgsBase).Name,
                chargeEvent.TypeHash,
                chargeEvent.GetType().Name
            ));
        }

        // receive chargeEvent values to appropriate proces
    }
}
