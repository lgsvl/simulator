using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleWeatherListener : DayNightEventListener
{
    public VehicleController vehicleCtrl;
    void Start()
    {
        if (vehicleCtrl == null)
            vehicleCtrl = GetComponentInParent<VehicleController>();

        CheckTimeOfDayEvents();
    }

    protected override void OnDay()
    {
        vehicleCtrl.OnDay();
    }

    protected override void OnNight()
    {
        vehicleCtrl.OnNight();
    }

    protected override void OnSunRise()
    {
        vehicleCtrl.OnSunRise();
    }

    protected override void OnSunSet()
    {
        vehicleCtrl.OnSunSet();
    }

    void CheckTimeOfDayEvents()
    {
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunrise)
            OnSunRise();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Day)
            OnDay();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Sunset)
            OnSunSet();
        if (DayNightEventsController.Instance.currentPhase == DayNightEventsController.Phase.Night)
            OnNight();
    }
}
