using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// TODO remove
public class CarAITimeListener : DayNightEventListener
{
    public CarAIController NPCar;

    private void Start()
    {
        if (NPCar == null) { NPCar = GetComponentInParent<CarAIController>(); }
    }

    protected override void OnSunRise()
    {
        if (NPCar != null) { NPCar.OnSunRise(); }
    }
    protected override void OnDay()
    {
        if (NPCar != null) { NPCar.OnDay(); }
    }
    protected override void OnSunSet()
    {
        if (NPCar != null) { NPCar.OnSunSet(); }
    }
    protected override void OnNight()
    {
        if (NPCar != null) { NPCar.OnNight(); }
    }
}
