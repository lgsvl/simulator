using UnityEngine;
using System.Collections;

public class MiscHelper
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public int GetLayer(string name)
    {
        // default is 0
        int layer = 0;
        switch (name)
        {
            case "Lane":
                layer = 13;
                break;
        }

        return layer;
    }

    public string GetTag(string name)
    {
        string tag = "Untagged";
        switch (name)
        {
            case "Lane":
                tag = "Road";
                break;
        }

        return tag;
    }
}
