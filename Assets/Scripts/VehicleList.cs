using System.Collections.Generic;
using UnityEngine;

public class VehicleList : MonoBehaviour
{
    public static List<VehicleList> Instances { get; private set; }
    public RectTransform Panel;

    void Awake()
    {
        if (Instances == null)
        {
            Instances = new List<VehicleList>();
        }

        Instances.Add(this);
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    public void ToggleDisplay(bool isOn)
    {
        if (Panel.transform.childCount > 2)
        {
            Panel.gameObject.SetActive(isOn);
        }
    }

    //void Update ()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        // only toggle visibility if there is at least one vehicle in scene
    //        if (Panel.transform.childCount > 2)
    //        {
    //            Panel.gameObject.SetActive(!Panel.gameObject.activeSelf);
    //        }
    //    }
    //}
}
