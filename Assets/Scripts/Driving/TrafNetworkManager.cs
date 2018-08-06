using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using UnityEngine;

public enum DataType
{
    CarInfo,
    CarEvent,
}

public struct DataTuple
{
    public string userid;
    public string json;
    public DataType type;
}

public class TrafNetworkManager : UnitySingleton<TrafNetworkManager>
{
    public TrafPerformanceManager trafPerfManager;

    System.Random random;

    [HideInInspector]
    public Queue<string> freeIdPool; //Free user id

    void Start()
    {
        if (trafPerfManager == null)
        {
            trafPerfManager = TrafPerformanceManager.GetInstance();
        }

        //Debug.Log("Machine MAC Address in Hex String Format: " + GetMACAddress());
        var MACAddress_Int = System.Convert.ToInt64(GetMACAddress(), 16);
        //Debug.Log("Machine MAC Address in Int 32 Format: " + MACAddress_Int);
        random = new System.Random((int)MACAddress_Int);

        freeIdPool = new Queue<string>();
    }

    public static long GetUTCTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalMilliseconds;
    }

    //Test to see if the car amount and UIDs are kept same after traffic running for a while
    void UIDTestFunction()
    {
        string txtFilePath = Application.persistentDataPath + "/Test.txt";
        int i = 0;
        while (System.IO.File.Exists(txtFilePath))
        {
            txtFilePath = Application.persistentDataPath + "/Test(" + (i++) + ").txt";
        }

        System.IO.StreamWriter writer = new System.IO.StreamWriter(txtFilePath, false);
        writer.WriteLine("Total Driving Traffic Car Amount: " + TrafPerformanceManager.GetInstance().GetCarSet().Count);
        writer.WriteLine();
        foreach (var carAI in TrafPerformanceManager.GetInstance().GetCarSet())
        { writer.WriteLine(carAI.carID); }
        writer.Close();

        Debug.Log("Car Analytical Data Saved to: " + txtFilePath);
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

    //Generate the random event id
    public string GenerateRandomEventId()
    {
        return System.Guid.NewGuid().ToString("D");
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
