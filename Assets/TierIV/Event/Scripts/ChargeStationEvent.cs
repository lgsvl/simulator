using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TierIV.Event;

public class ChargeStationEvent : MonoBehaviour
{
    struct BatteryVolume
    {
        public float parcentage;
    }

    BatteryVolume volume = new BatteryVolume { parcentage = 10f };

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerStay(Collider target)
    {
        // targetに通知
        EventNotifier.Instance.BroadcastEvent("Charge", JsonUtility.ToJson(volume));
    }

}