/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using UnityEngine;

public class TrafInfoManager : UnitySingleton<TrafInfoManager>
{
    public TrafPerformanceManager trafPerfManager;

    System.Random random;

    [HideInInspector]
    public Queue<string> freeIdPool; //Free user id

    void Start()
    {
        if (trafPerfManager == null)
        {
            trafPerfManager = TrafPerformanceManager.Instance;
        }

        //Debug.Log("Machine MAC Address in Hex String Format: " + GetMACAddress());
        var MACAddress_Int = System.Convert.ToInt64(GetMACAddress(), 16);
        //Debug.Log("Machine MAC Address in Int 32 Format: " + MACAddress_Int);
        random = new System.Random((int)MACAddress_Int);

        freeIdPool = new Queue<string>();
    }

    //Generate a string of 64 digits Hexadecimal
    public string GeneratePseudoRandomUID()
    {
        StringBuilder sb = new StringBuilder(64);
        for (int i = 0; i < 8; i++)
        {
            sb.Append(random.Next().ToString("x8"));
        }
        return sb.ToString();
    }

    static string GetMACAddress()
    {
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        string sPhysicalAddress = string.Empty;
        foreach (NetworkInterface adapter in nics)
        {
            if (sPhysicalAddress == string.Empty) // only return MAC Address from first card
            {
                sPhysicalAddress = adapter.GetPhysicalAddress().ToString();
            }
        }
        return sPhysicalAddress;
    }
}
